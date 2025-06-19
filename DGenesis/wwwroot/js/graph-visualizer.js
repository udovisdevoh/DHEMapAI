class GraphVisualizer {
    constructor(containerSelector) {
        this.container = document.querySelector(containerSelector);
        if (!this.container) {
            throw new Error(`Container not found for selector: ${containerSelector}`);
        }
        this.svgNS = "http://www.w3.org/2000/svg";
        this.nodeColors = {
            standard: "#2980b9",
            start: "#2ecc71",
            exit: "#e74c3c",
            locked: "#7f8c8d"
        };
        this.nodeRadius = 15;
    }

    _createSVG() {
        this.container.innerHTML = '';
        const svg = document.createElementNS(this.svgNS, "svg");
        svg.setAttribute("width", "100%");
        svg.setAttribute("height", "100%");
        svg.setAttribute("id", "graph-svg");

        const g = document.createElementNS(this.svgNS, "g");
        g.setAttribute("id", "zoom-group");
        svg.appendChild(g);

        this.container.appendChild(svg);
        this.svg = svg;
        this.zoomGroup = g;
    }

    _createTooltip() {
        this.tooltip = document.createElement("div");
        this.tooltip.className = "graph-tooltip";
        document.body.appendChild(this.tooltip);
    }

    _calculateViewBox(nodes) {
        if (nodes.length === 0) return "0 0 100 100";
        const padding = 50;
        const xCoords = nodes.map(n => n.position.x);
        const yCoords = nodes.map(n => n.position.y);
        const minX = Math.min(...xCoords) - padding;
        const minY = Math.min(...yCoords) - padding;
        const maxX = Math.max(...xCoords) + padding;
        const maxY = Math.max(...yCoords) + padding;
        const width = maxX - minX;
        const height = maxY - minY;
        return `${minX} ${minY} ${width} ${height}`;
    }

    render(graphData) {
        if (!graphData || !graphData.nodes) return;
        this._createSVG();
        this._createTooltip();

        const { nodes, edges } = graphData;
        const nodeMap = new Map(nodes.map(n => [n.id, n]));

        // Calculer la viewBox pour centrer le graphe
        this.svg.setAttribute("viewBox", this._calculateViewBox(nodes));

        // 1. Dessiner les arêtes (d'abord, pour qu'elles soient en dessous des nœuds)
        edges.forEach(edge => {
            const sourceNode = nodeMap.get(edge.source);
            const targetNode = nodeMap.get(edge.target);
            if (sourceNode && targetNode) {
                const line = document.createElementNS(this.svgNS, "line");
                line.setAttribute("x1", sourceNode.position.x);
                line.setAttribute("y1", sourceNode.position.y);
                line.setAttribute("x2", targetNode.position.x);
                line.setAttribute("y2", targetNode.position.y);
                line.setAttribute("class", "edge");
                this.zoomGroup.appendChild(line);
            }
        });

        // 2. Dessiner les nœuds
        nodes.forEach(node => {
            const circle = document.createElementNS(this.svgNS, "circle");
            circle.setAttribute("cx", node.position.x);
            circle.setAttribute("cy", node.position.y);
            circle.setAttribute("r", this.nodeRadius);
            circle.setAttribute("fill", this.nodeColors[node.type] || this.nodeColors.standard);
            circle.setAttribute("class", "node");
            circle.dataset.id = node.id;

            // Événements pour l'infobulle
            circle.addEventListener('mouseover', (e) => {
                let tooltipContent = `<b>ID:</b> ${node.id}<br><b>Type:</b> ${node.type}`;
                if (node.type === 'locked') {
                    // Trouver qui déverrouille ce nœud
                    const unlocker = nodes.find(n => n.unlocks?.includes(node.id));
                    if (unlocker) {
                        tooltipContent += `<br><b>Déverrouillé par:</b> Nœud ${unlocker.id}`;
                    }
                }
                if (node.unlocks) {
                    tooltipContent += `<br><b>Déverrouille:</b> Nœud ${node.unlocks.join(', ')}`;
                }

                this.tooltip.innerHTML = tooltipContent;
                this.tooltip.style.display = 'block';
                this.tooltip.style.left = `${e.pageX + 15}px`;
                this.tooltip.style.top = `${e.pageY + 15}px`;
            });

            circle.addEventListener('mouseout', () => {
                this.tooltip.style.display = 'none';
            });

            this.zoomGroup.appendChild(circle);
        });

        // Activer le pan & zoom
        svgPanZoom(this.svg, {
            panEnabled: true,
            controlIconsEnabled: true,
            zoomScaleSensitivity: 0.2,
            minZoom: 0.5,
            maxZoom: 10,
            fit: true,
            center: true,
        });
    }
}