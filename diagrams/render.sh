#!/bin/bash
# Render all PlantUML diagrams to SVG
cd "$(dirname "$0")"
PUML_USER="$(id -u):$(id -g)" docker compose run --rm render
