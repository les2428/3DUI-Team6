"""
Tiny static file server for local testing of the WebGL build.

Unlike `python -m http.server` it sends no-store headers, so the browser always picks up the latest
build (during development Unity's own data caching should be off as well - the template does that).

    py -3 Tools/serve.py            (serves Builds/WebGL at http://localhost:8080)
"""

import argparse
import functools
from pathlib import Path
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer


class NoCacheHandler(SimpleHTTPRequestHandler):
    extensions_map = {
        **SimpleHTTPRequestHandler.extensions_map,
        ".unityweb": "application/octet-stream",
        ".wasm": "application/wasm",
        ".data": "application/octet-stream",
        ".js": "application/javascript",
    }

    def end_headers(self):
        self.send_header("Cache-Control", "no-store, must-revalidate")
        self.send_header("Pragma", "no-cache")
        self.send_header("Expires", "0")
        # WebXR needs a secure context; on localhost http is fine.
        super().end_headers()

    def log_message(self, fmt, *args):
        if "GET" in fmt % args and any(x in (fmt % args) for x in (".data", ".wasm", "index.html")):
            super().log_message(fmt, *args)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dir", default=str(Path(__file__).resolve().parent.parent / "Builds" / "WebGL"))
    ap.add_argument("--port", type=int, default=8080)
    ap.add_argument("--host", default="127.0.0.1")
    args = ap.parse_args()
    handler = functools.partial(NoCacheHandler, directory=args.dir)
    print(f"Serving {args.dir} at http://{args.host}:{args.port} (no cache)")
    ThreadingHTTPServer((args.host, args.port), handler).serve_forever()


if __name__ == "__main__":
    main()
