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
        if (this.tooltip) {
            this.tooltip.remove();
        }
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

        this.svg.setAttribute("viewBox", this._calculateViewBox(nodes));

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

        nodes.forEach(node => {
            const circle = document.createElementNS(this.svgNS, "circle");
            circle.setAttribute("cx", node.position.x);
            circle.setAttribute("cy", node.position.y);
            circle.setAttribute("r", this.nodeRadius);
            circle.setAttribute("fill", this.nodeColors[node.type] || this.nodeColors.standard);
            circle.setAttribute("class", "node");
            circle.dataset.id = node.id;

            // --- LOGIQUE DE SURBRILLANCE MISE À JOUR ---
            circle.addEventListener('mouseover', (e) => {
                const partnerIds = new Set();

                // Si on survole un nœud qui déverrouille...
                if (node.unlocks && node.unlocks.length > 0) {
                    node.unlocks.forEach(id => partnerIds.add(id));
                }

                // Si on survole un nœud verrouillé...
                if (node.type === 'locked') {
                    const unlockerNode = nodes.find(n => n.unlocks?.includes(node.id));
                    if (unlockerNode) {
                        partnerIds.add(unlockerNode.id);
                    }
                }

                // Appliquer le style de surbrillance
                if (partnerIds.size > 0) {
                    // Mettre le nœud survolé en surbrillance
                    e.currentTarget.classList.add('highlight-partner');
                    // Mettre tous les partenaires en surbrillance
                    partnerIds.forEach(id => {
                        const partnerElement = this.zoomGroup.querySelector(`circle[data-id='${id}']`);
                        if (partnerElement) {
                            partnerElement.classList.add('highlight-partner');
                        }
                    });
                }

                // Logique de l'infobulle (inchangée)
                let tooltipContent = `<b>ID:</b> ${node.id}<br><b>Type:</b> ${node.type}`;
                if (node.type === 'locked') {
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
            });

            circle.addEventListener('mouseout', () => {
                // Retirer la surbrillance de tous les nœuds
                this.zoomGroup.querySelectorAll('.highlight-partner').forEach(el => {
                    el.classList.remove('highlight-partner');
                });

                this.tooltip.style.display = 'none';
            });
            // --- FIN DE LA LOGIQUE MISE À JOUR ---

            circle.addEventListener('mousemove', (e) => {
                this.tooltip.style.left = `${e.pageX + 15}px`;
                this.tooltip.style.top = `${e.pageY + 15}px`;
            });

            this.zoomGroup.appendChild(circle);
        });

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