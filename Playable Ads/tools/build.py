"""
Inlines src/game.js, src/levels.json, assets/img/*.webp, assets/audio/*.mp3 and the Fredoka font into ONE
self-contained HTML file: Playable Ads/AvoidTheCold_Playable.html  (no external requests - Meta requirement).

Run:  python "Playable Ads/tools/build.py"
Fails loudly if the output exceeds Meta's 2 MB single-HTML limit.
"""
import base64, json, os

HERE = os.path.dirname(os.path.abspath(__file__))
PA = os.path.dirname(HERE)
ROOT = os.path.join(os.path.dirname(PA), "Assets")
OUT = os.path.join(PA, "AvoidTheCold_Playable.html")
META_LIMIT = 2 * 1024 * 1024

AUDIO = ["bgm", "wind", "connect", "winsfx", "whoosh", "pop"]
manifest = json.load(open(os.path.join(PA, "assets", "manifest.json")))
levels = json.load(open(os.path.join(PA, "src", "levels.json")))


def b64(path):
    return base64.b64encode(open(path, "rb").read()).decode("ascii")


assets = {}
for key, m in manifest.items():
    assets[key] = "data:image/webp;base64," + b64(os.path.join(PA, "assets", m["file"]))
for key in AUDIO:
    assets[key] = "data:audio/mpeg;base64," + b64(os.path.join(PA, "assets", "audio", key + ".mp3"))
assets["font"] = "data:font/ttf;base64," + b64(os.path.join(ROOT, "Fonts", "Fredoka", "static", "Fredoka-Regular.ttf"))

html = open(os.path.join(PA, "src", "template.html"), encoding="utf-8").read()
game = open(os.path.join(PA, "src", "game.js"), encoding="utf-8").read()
html = html.replace("/*ASSETS*/", json.dumps(assets, separators=(",", ":")))
html = html.replace("/*LEVELS*/", json.dumps(levels, separators=(",", ":")))
html = html.replace("/*GAME*/", game)
open(OUT, "w", encoding="utf-8", newline="\n").write(html)

size = os.path.getsize(OUT)
groups = {"images": sum(len(v) for k, v in assets.items() if v.startswith("data:image")),
          "audio": sum(len(v) for k, v in assets.items() if v.startswith("data:audio")),
          "font": len(assets["font"]), "code": len(game)}
print("wrote %s  %.1f KB (%.2f MB)" % (OUT, size / 1024, size / 1048576))
for k, v in groups.items():
    print("  %-7s %7.1f KB" % (k, v / 1024))
if size > META_LIMIT:
    raise SystemExit("ERROR: over Meta's 2 MB single-HTML limit")
print("OK: under Meta 2 MB single-HTML limit and under the 5 MB project limit")
