#!/bin/bash
# Render all PlantUML diagrams to SVG using Docker
docker run --rm -v "$(cd "$(dirname "$0")" && pwd)":/data plantuml/plantuml -tsvg /data/*.puml
