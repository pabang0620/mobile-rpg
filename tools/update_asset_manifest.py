import hashlib, json
from datetime import date
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
files = sorted((root / "client/Assets/Game/Art").rglob("*.png"))
files += sorted((root / "generated-images").rglob("*.png"))

# Third-party asset provenance for files added under client/Assets/Game/Art/{Backgrounds,VFX,Enemies,UI}.
# Existing first-party/AI-generated assets (top-level Art/*.png, generated-images/**) are left without
# source/license/used_by metadata here; their provenance is documented in ASSET_STATUS.md instead.
PROVENANCE = {
    "client/Assets/Game/Art/Backgrounds/WinterTreesFar.png": {
        "source_url": "https://joe777.itch.io/free-parallax-backgrounds",
        "license": "Free for commercial use, resale of the asset pack itself prohibited (itch.io page terms)",
        "used_by": ["BattleScreen.cs far parallax layer (technical depth-layer demo, not production-selected)"],
    },
    "client/Assets/Game/Art/Backgrounds/WinterTreesMid.png": {
        "source_url": "https://joe777.itch.io/free-parallax-backgrounds",
        "license": "Free for commercial use, resale of the asset pack itself prohibited (itch.io page terms)",
        "used_by": ["not wired - kept for future depth-layer use"],
    },
    "client/Assets/Game/Art/Backgrounds/WinterTreesNear.png": {
        "source_url": "https://joe777.itch.io/free-parallax-backgrounds",
        "license": "Free for commercial use, resale of the asset pack itself prohibited (itch.io page terms)",
        "used_by": ["not wired - kept for future depth-layer use"],
    },
    "client/Assets/Game/Art/Backgrounds/CaveCrystalRidgeA.png": {
        "source_url": "https://joe777.itch.io/free-parallax-backgrounds",
        "license": "Free for commercial use, resale of the asset pack itself prohibited (itch.io page terms)",
        "used_by": ["not wired - alternate crop of the wired ridge layer"],
    },
    "client/Assets/Game/Art/Backgrounds/CaveCrystalRidgeB.png": {
        "source_url": "https://joe777.itch.io/free-parallax-backgrounds",
        "license": "Free for commercial use, resale of the asset pack itself prohibited (itch.io page terms)",
        "used_by": ["BattleScreen.cs near parallax layer (sapphire-themed, production-recommended)"],
    },
    "client/Assets/Game/Art/VFX/MagicMissile.png": {
        "source_url": "https://icemaan.itch.io/free-500-pixel-art-effects",
        "license": "Free for commercial use, resale of the asset pack itself prohibited (itch.io page terms)",
        "used_by": ["BattleScreen.cs skill impact flipbook (ImpactFlash)"],
    },
    "client/Assets/Game/Art/Enemies/GoblinPixelArtIdle.png": {
        "source_url": "https://opengameart.org/content/goblin-free-pixelart",
        "license": "CC0",
        "used_by": ["BattleScreen.cs enemy slot 1 sprite (contrast goblin, production-recommended)"],
    },
    "client/Assets/Game/Art/Enemies/GoblinPixelArtRun.png": {
        "source_url": "https://opengameart.org/content/goblin-free-pixelart",
        "license": "CC0",
        "used_by": ["not wired - kept for future animation states"],
    },
    "client/Assets/Game/Art/Enemies/GoblinPixelArtAttack.png": {
        "source_url": "https://opengameart.org/content/goblin-free-pixelart",
        "license": "CC0",
        "used_by": ["not wired - kept for future animation states"],
    },
    "client/Assets/Game/Art/Enemies/GoblinPixelArtDeath.png": {
        "source_url": "https://opengameart.org/content/goblin-free-pixelart",
        "license": "CC0",
        "used_by": ["not wired - kept for future animation states"],
    },
    "client/Assets/Game/Art/Enemies/GoblinMonsterSpritesheet32.png": {
        "source_url": "https://opengameart.org/content/goblin-monster",
        "license": "CC0",
        "used_by": ["not wired directly - source sheet for GoblinMonsterFrame.png"],
    },
    "client/Assets/Game/Art/Enemies/GoblinMonsterFrame.png": {
        "source_url": "https://opengameart.org/content/goblin-monster",
        "license": "CC0",
        "used_by": ["BattleScreen.cs enemy slot 2 sprite (contrast goblin, secondary variant)"],
    },
    "client/Assets/Game/Art/UI/InventoryShopIcons.png": {
        "source_url": "https://kenney.nl/assets/ui-pack-rpg-expansion",
        "license": "CC0",
        "used_by": ["BattleScreen.cs bag/shop menu slot icons (placeholder)"],
    },
    "client/Assets/Game/Art/UI/FantasyPanelBorder.png": {
        "source_url": "https://kenney-assets.itch.io/fantasy-ui-borders",
        "license": "CC0",
        "used_by": ["BattleScreen.cs right menu panel 9-slice border overlay"],
    },
}


def has_usable_alpha(image):
    if image.mode != "RGBA":
        return False
    alphas = image.getdata(3)
    transparent = False
    visible = False
    for a in alphas:
        if a == 0:
            transparent = True
        else:
            visible = True
        if transparent and visible:
            return True
    return False


assets = []
for path in files:
    with Image.open(path) as image:
        image = image.convert("RGBA") if image.mode in ("RGB", "RGBA", "P", "LA") else image
        entry = {
            "path": path.relative_to(root).as_posix(),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            "width": image.width,
            "height": image.height,
            "pixel_format": image.mode,
            "alpha": has_usable_alpha(image.convert("RGBA")),
        }
        provenance = PROVENANCE.get(entry["path"])
        if provenance:
            entry.update(provenance)
        assets.append(entry)

(root / "docs/ASSET_MANIFEST.json").write_text(
    json.dumps({"schema": 3, "generated_at": date.today().isoformat(), "assets": assets}, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)
print(f"wrote {len(assets)} assets")
