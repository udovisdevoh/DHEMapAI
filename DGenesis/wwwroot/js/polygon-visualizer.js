class PolygonVisualizer {
    constructor(containerSelector) {
        this.container = document.querySelector(containerSelector);
        if (!this.container) {
            throw new Error(`Container not found for selector: ${containerSelector}`);
        }
        this.svgNS = "http://www.w3.org/2000/svg";
    }

    _createSVG() {
        this.container.innerHTML = '';
        const svg = document.createElementNS(this.svgNS, "svg");
        svg.setAttribute("width", "100%");
        svg.setAttribute("height", "100%");
        svg.setAttribute("id", "shape-svg");

        const g = document.createElementNS(this.svgNS, "g");
        g.setAttribute("id", "zoom-group");
        svg.appendChild(g);

        this.container.appendChild(svg);
        this.svg = svg;
        this.zoomGroup = g;
    }

    _calculateViewBox(vertices) {
        if (vertices.length === 0) return "0 0 100 100";
        const padding = 50;
        const xCoords = vertices.map(v => v.x);
        const yCoords = vertices.map(v => v.y);
        const minX = Math.min(...xCoords) - padding;
        const minY = Math.min(...yCoords) - padding;
        const maxX = Math.max(...xCoords) + padding;
        const maxY = Math.max(...yCoords) + padding;
        const width = maxX - minX;
        const height = maxY - minY;
        return `${minX} ${minY} ${width} ${height}`;
    }

    render(shapeData, symmetryAxes = 0) {
        if (!shapeData || !shapeData.vertices || shapeData.vertices.length < 3) return;

        this._createSVG();
        const { vertices } = shapeData;

        const viewBox = this._calculateViewBox(vertices);
        this.svg.setAttribute("viewBox", viewBox);

        // Dessiner les axes de symétrie EN PREMIER pour qu'ils soient en arrière-plan
        if (symmetryAxes > 0) {
            const viewBoxParts = viewBox.split(' ').map(parseFloat);
            const lineLength = Math.max(viewBoxParts[2], viewBoxParts[3]) * 0.6;

            for (let i = 0; i < symmetryAxes; i++) {
                const angle = (i * Math.PI) / symmetryAxes;
                const axisLine = document.createElementNS(this.svgNS, "line");

                const x1 = lineLength * Math.cos(angle);
                const y1 = lineLength * Math.sin(angle);

                axisLine.setAttribute("x1", x1);
                axisLine.setAttribute("y1", y1);
                axisLine.setAttribute("x2", -x1);
                axisLine.setAttribute("y2", -y1);
                axisLine.setAttribute("class", "symmetry-axis");
                this.zoomGroup.appendChild(axisLine);
            }
        }

        const polygon = document.createElementNS(this.svgNS, "polygon");

        const pointsString = vertices.map(v => `${v.x},${v.y}`).join(' ');
        polygon.setAttribute("points", pointsString);
        polygon.setAttribute("class", "sector-polygon");

        this.zoomGroup.appendChild(polygon);

        // Ajouter des points sur les sommets pour mieux visualiser
        vertices.forEach(v => {
            const circle = document.createElementNS(this.svgNS, "circle");
            circle.setAttribute("cx", v.x);
            circle.setAttribute("cy", v.y);
            circle.setAttribute("r", 4);
            circle.setAttribute("class", "polygon-vertex");
            this.zoomGroup.appendChild(circle);
        });

        svgPanZoom(this.svg, {
            panEnabled: true,
            controlIconsEnabled: true,
            zoomScaleSensitivity: 0.2,
            minZoom: 0.1,
            maxZoom: 20,
            fit: true,
            center: true,
        });
    }
}