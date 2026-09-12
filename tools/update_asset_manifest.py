import hashlib, json
from datetime import date
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
files = sorted((root / "client/Assets/Game/Art").glob("*.png"))
files += sorted((root / "generated-images").rglob("*.png"))
assets = []
for path in files:
    with Image.open(path) as image:
        assets.append({
            "path": path.relative_to(root).as_posix(),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            "width": image.width,
            "height": image.height,
            "pixel_format": image.mode,
        })
(root / "docs/ASSET_MANIFEST.json").write_text(
    json.dumps({"schema": 2, "generated_at": date.today().isoformat(), "assets": assets}, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)
print(f"wrote {len(assets)} assets")
