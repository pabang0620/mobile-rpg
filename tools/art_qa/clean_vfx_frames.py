"""Clean AI-generated skill VFX sheets so every frame sits fully inside its own grid cell.

Problems this fixes (measured 2026-09-28, see docs/QUALITY_PLAN.md):
  * Unity slices these sheets on a uniform cols x rows grid (SkillVfxImporter /
    WarriorSkillVfxImporter, PPU = width / 8). The generated art did not respect
    that grid, so many frames crossed cell borders: the slice showed a hard
    straight cut on one side and a sliver of the neighbouring frame on the other.
  * ~40% of every sheet was alpha 1..8 haze, which renders as a faint box.

Method (canvas size and grid stay identical, so PPU / world size do not change):
  1. Soft haze cut: alpha *= smoothstep(HAZE_LO, HAZE_HI, alpha).
  2. Row and frame borders are moved off the uniform grid lines to the nearest
     real gap in the art (projection of alpha > STRONG, within WINDOW cells).
  3. Each frame is moved (and shrunk only if it cannot fit - area effects
     uniformly, directional rows per axis) so its bbox lies inside its import
     cell with MARGIN px of clear border.
Frame pivots are recomputed from content bboxes by the importers, so moving
content inside a cell keeps the anchoring convention (centre / left edge).

Usage: python3 tools/art_qa/clean_vfx_frames.py [--dry-run]
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[2] / "client/Assets/Sapphire/Art/VFX"
# (file, cols, rows, directional rows) - must match SkillVfxImporter / WarriorSkillVfxImporter.
# MageSkillVfxAtlas.png is intentionally absent: every row of it is overridden at import
# time by the *Padded strips below, so it never reaches the game.
SHEETS = [
    ("Warrior/WarriorSkillVfxAtlas.png", 8, 5, {0, 4}),
    ("Warrior/WarriorGroundSlamPadded.png", 8, 1, set()),
    ("MageDirectionalPadded.png", 8, 3, {2}),
    ("ManaShieldPadded.png", 8, 1, set()),
    ("ThunderFieldPadded.png", 8, 1, set()),
]
HAZE_LO, HAZE_HI = 4.0, 20.0
STRONG = 16
MARGIN = 4
WINDOW = 0.35


def smoothstep(lo, hi, v):
    t = np.clip((v - lo) / (hi - lo), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def segment(proj, k):
    """Return k+1 cut positions (0 .. len) splitting one axis into k frame bands.

    The generated frames sit roughly on the uniform import grid but overflow it,
    so each inner cut is searched within +-WINDOW of its grid line: prefer empty
    columns deep inside a gap, then the thinnest content, then closeness to the
    grid line.
    """
    n = len(proj)
    cell = n / k
    ps = np.convolve(proj.astype(np.float64), np.ones(5) / 5, mode="same")
    occ = ps > 0
    gap = ndimage.distance_transform_edt(~occ) if occ.any() else np.full(n, 1e6)
    cuts = [0]
    for j in range(1, k):
        e = j * cell
        lo = max(cuts[-1] + int(cell * 0.3), int(e - WINDOW * cell))
        hi = min(n - 1, int(e + WINDOW * cell))
        xs = np.arange(lo, hi + 1)
        score = -ps[xs] * 1000 + np.minimum(gap[xs], 40) - 0.25 * np.abs(xs - e)
        cuts.append(int(xs[int(np.argmax(score))]))
    cuts.append(n)
    return cuts


def clean(path, cols, rows, directional_rows):
    img = np.array(Image.open(path).convert("RGBA")).astype(np.float32)
    H, W = img.shape[:2]
    alpha = img[..., 3] * smoothstep(HAZE_LO, HAZE_HI, img[..., 3])
    alpha[alpha < 1] = 0
    img[..., 3] = alpha
    strong = alpha > STRONG
    # Hollow rings/domes would otherwise show their thinnest column in the middle
    # of the ring; fill enclosed holes so cuts land between frames.
    solid = ndimage.binary_fill_holes(ndimage.binary_closing(strong, np.ones((9, 9))))

    xs = [round(c * W / cols) for c in range(cols + 1)]
    ys = [round(r * H / rows) for r in range(rows + 1)]
    ycuts = segment(solid.sum(1), rows)

    out = np.zeros_like(img)
    report = []
    for r in range(rows):
        band = slice(ycuts[r], ycuts[r + 1])
        xcuts = segment(solid[band].sum(0), cols)
        for c in range(cols):
            region = np.zeros((H, W), bool)
            region[band, xcuts[c]:xcuts[c + 1]] = True
            mask = region & (alpha > 0)
            if not mask.any():
                continue
            yx = np.argwhere(mask)
            (y0, x0), (y1, x1) = yx.min(0), yx.max(0) + 1
            layer = np.where(mask[..., None], img, 0)[y0:y1, x0:x1]
            cl, cr, ct, cb = xs[c] + MARGIN, xs[c + 1] - MARGIN, ys[r] + MARGIN, ys[r + 1] - MARGIN
            bw, bh = x1 - x0, y1 - y0
            sx = min(1.0, (cr - cl) / bw)
            sy = min(1.0, (cb - ct) / bh)
            if r not in directional_rows:  # area effects keep their proportions
                sx = sy = min(sx, sy)
            if sx < 1.0 or sy < 1.0:
                pm = layer.copy()
                pm[..., :3] *= pm[..., 3:4] / 255.0  # resample premultiplied: no fringes
                nw, nh = max(1, int(bw * sx)), max(1, int(bh * sy))
                chans = [np.array(Image.fromarray(pm[..., i]).resize((nw, nh), Image.LANCZOS)) for i in range(4)]
                pm = np.clip(np.stack(chans, -1), 0, 255)
                a = pm[..., 3:4]
                pm[..., :3] = np.where(a > 0, pm[..., :3] * 255.0 / np.maximum(a, 1e-3), 0)
                layer = np.clip(pm, 0, 255)
                x0 = int(round((x0 + x1) / 2 - nw / 2))
                y0 = int(round((y0 + y1) / 2 - nh / 2))
                bw, bh = nw, nh
            # Keep each frame where the art put it relative to its own cell as far as
            # possible, but never across the cell border.
            nx0 = int(np.clip(x0, cl, cr - bw))
            ny0 = int(np.clip(y0, ct, cb - bh))
            out[ny0:ny0 + bh, nx0:nx0 + bw] = layer
            if sx < 1.0 or sy < 1.0 or (nx0, ny0) != (x0, y0):
                report.append(f"r{r}c{c}: moved ({nx0 - x0},{ny0 - y0}) scale ({sx:.2f},{sy:.2f})")
    return np.clip(out + 0.5, 0, 255).astype(np.uint8), report


def main():
    dry = "--dry-run" in sys.argv
    for rel, cols, rows, directional in SHEETS:
        path = ROOT / rel
        result, report = clean(path, cols, rows, directional)
        print(f"## {rel}: {len(report)} frames adjusted")
        for line in report:
            print("   ", line)
        if not dry:
            Image.fromarray(result, "RGBA").save(path, optimize=True)


if __name__ == "__main__":
    main()
