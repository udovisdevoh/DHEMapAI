class DPolyGraphVisualizer {
    constructor(containerSelector) {
        this.container = document.querySelector(containerSelector);
        if (!this.container) {
            throw new Error(`Container not found for selector: ${containerSelector}`);
        }
        this.svgNS = "http://www.w3.org/2000/svg";
        this.sectorColors = {
            standard: "#3498db",
            start: "#2ecc71",
            exit: "#e74c3c",
            locked: "#95a5a6",
            corridor: "#f39c12" // Couleur pour les corridors
        };
        this.highlightColor = "#e67e22"; // Une couleur de surbrillance plus vive
    }

    _createSVG() {
        this.container.innerHTML = ''; // Nettoyer le conteneur
        const svg = document.createElementNS(this.svgNS, "svg");
        svg.setAttribute("width", "100%");
        svg.setAttribute("height", "100%");
        svg.setAttribute("id", "dpolygraph-svg");

        const g = document.createElementNS(this.svgNS, "g");
        g.setAttribute("id", "zoom-group");
        svg.appendChild(g);

        this.container.appendChild(svg);
        this.svg = svg;
        this.zoomGroup = g;
    }

    _calculateViewBox(sectors) {
        if (sectors.length === 0) return "0 0 100 100";
        const padding = 50;
        let allVertices = sectors.flatMap(s => s.polygon);

        if (allVertices.length === 0) return "0 0 100 100";

        const xCoords = allVertices.map(v => v.x);
        const yCoords = allVertices.map(v => v.y);

        const minX = Math.min(...xCoords) - padding;
        const minY = Math.min(...yCoords) - padding;
        const maxX = Math.max(...xCoords) + padding;
        const maxY = Math.max(...yCoords) + padding;

        const width = maxX - minX;
        const height = maxY - minY;

        return `${minX} ${minY} ${width} ${height}`;
    }

    render(polyGraphData) {
        if (!polyGraphData || !polyGraphData.sectors || polyGraphData.sectors.length === 0) {
            this.container.innerHTML = '<div class="placeholder-text">Aucune donnée à visualiser.</div>';
            return;
        }

        this._createSVG();
        const { sectors } = polyGraphData;

        this.svg.setAttribute("viewBox", this._calculateViewBox(sectors));

        sectors.forEach(sector => {
            const polygon = document.createElementNS(this.svgNS, "polygon");

            const points = sector.polygon.map(p => `${p.x},${p.y}`).join(' ');
            polygon.setAttribute("points", points);
            polygon.setAttribute("class", "sector-polygon");
            polygon.setAttribute("fill", this.sectorColors[sector.type] || this.sectorColors.standard);
            polygon.dataset.id = sector.id;

            // Logique de surbrillance
            polygon.addEventListener('mouseover', () => {
                let partnerId = null;
                if (sector.unlocksSector != null) {
                    partnerId = sector.unlocksSector;
                } else if (sector.unlockedBySector != null) {
                    partnerId = sector.unlockedBySector;
                }

                if (partnerId !== null) {
                    const partnerElement = this.zoomGroup.querySelector(`polygon[data-id='${partnerId}']`);
                    if (partnerElement) {
                        polygon.style.stroke = this.highlightColor;
                        partnerElement.style.stroke = this.highlightColor;
                    }
                }
            });

            polygon.addEventListener('mouseout', () => {
                this.zoomGroup.querySelectorAll('.sector-polygon').forEach(p => {
                    p.style.stroke = '#2c3e50';
                });
            });

            this.zoomGroup.appendChild(polygon);
        });

        // Activer le pan/zoom
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