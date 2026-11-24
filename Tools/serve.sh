#!/bin/sh
# Serves Builds/WebGL at http://localhost:8080 (macOS / Linux). Needs Python 3.
cd "$(dirname "$0")" && python3 serve.py "$@"
