#!/usr/bin/env python3
"""Remove cross-cell bleed artifacts from the character topdown grid sheets.

Root cause (measured via PIL/scipy connected-component analysis, 2026-09-16):
each 362x362 grid cell occasionally contains a small SECOND alpha blob,
disconnected from the main character silhouette by a real transparent gap,
where the neighboring cell's artwork (a hood tip, hair strand, etc.) was
drawn slightly oversized and spilled past its own cell boundary into the
adjacent cell. For the "Right" facing row in particular this bleed sits at
the very bottom edge of the cell, which corrupted the naive full-alpha-bbox
foot-position measurement used to hand-pick sprite pivots (bbox bottom
appeared to be at the cell edge, i.e. foot_norm=0, when the character's own
feet actually end ~11-17% of a cell higher) - this is the direct cause of
the "floating, especially when facing right" bug report.

This script keeps only the LARGEST connected alpha component (8-connectivity)
per cell and zeroes every other pixel to fully transparent, for every one of
the 12 cells (4 directions x 3 poses) in both MageTopdownGridSheet.png and
WarriorTopdownGridSheet.png. It does not touch pixels within the main
character silhouette itself.
"""
import sys
from PIL import Image
import numpy as np
from scipy import ndimage

ALPHA_THRESHOLD = 10
CELL = 362
ROW_NAMES = ("Down", "Left", "Right", "Up")
COL_NAMES = ("Idle", "WalkA", "WalkB")


def clean(path):
    img = Image.open(path).convert("RGBA")
    arr = np.array(img)
    alpha = arr[:, :, 3]
    total_removed = 0
    report = []
    for r, rname in enumerate(ROW_NAMES):
        for c, cname in enumerate(COL_NAMES):
            top = r * CELL
            left = c * CELL
            cell_alpha = alpha[top:top + CELL, left:left + CELL]
            mask = cell_alpha > ALPHA_THRESHOLD
            labeled, n = ndimage.label(mask, structure=np.ones((3, 3)))
            if n <= 1:
                continue
            sizes = ndimage.sum(mask, labeled, range(1, n + 1))
            biggest = int(np.argmax(sizes)) + 1
            removed_mask = mask & (labeled != biggest)
            removed_px = int(removed_mask.sum())
            if removed_px == 0:
                continue
            # Zero out RGBA (not just alpha) for removed pixels so no
            # premultiplied-color ghosting can show up under any blend mode.
            sub = arr[top:top + CELL, left:left + CELL]
            sub[removed_mask] = [0, 0, 0, 0]
            arr[top:top + CELL, left:left + CELL] = sub
            total_removed += removed_px
            report.append(f"  {rname}_{cname}: removed {removed_px}px bleed ({n - 1} extra component(s))")
    out = Image.fromarray(arr, "RGBA")
    out.save(path)
    print(f"{path}: removed {total_removed}px total")
    for line in report:
        print(line)


if __name__ == "__main__":
    for p in sys.argv[1:]:
        clean(p)
