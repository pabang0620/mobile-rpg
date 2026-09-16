#!/usr/bin/env python3
"""Measure per-cell alpha bbox and foot (bottom-of-content) position for
character topdown grid sheets (3 cols idle/walkA/walkB x 4 rows down/left/
right/up, cellSize=362). Prints per-frame (not averaged) foot_norm and
cx_norm so frame-to-frame jitter within a direction is visible."""
import sys
from PIL import Image

ALPHA_THRESHOLD = 10

def measure(path, cell_size=362, row_names=("Down", "Left", "Right", "Up"),
            col_names=("Idle", "WalkA", "WalkB")):
    img = Image.open(path).convert("RGBA")
    w, h = img.size
    expected_w = cell_size * len(col_names)
    expected_h = cell_size * len(row_names)
    print(f"\n=== {path} ({w}x{h}, expected {expected_w}x{expected_h}) ===")
    px = img.load()
    results = {}
    for r, rname in enumerate(row_names):
        row_results = []
        for c, cname in enumerate(col_names):
            left = c * cell_size
            top = r * cell_size
            min_x, max_x, min_y, max_y = None, None, None, None
            for y in range(cell_size):
                for x in range(cell_size):
                    a = px[left + x, top + y][3]
                    if a > ALPHA_THRESHOLD:
                        if min_x is None or x < min_x: min_x = x
                        if max_x is None or x > max_x: max_x = x
                        if min_y is None or y < min_y: min_y = y
                        if max_y is None or y > max_y: max_y = y
            if min_x is None:
                print(f"  {rname}_{cname}: EMPTY (no alpha content)")
                continue
            cx = (min_x + max_x) / 2.0
            cx_norm = cx / cell_size
            # foot = bottom-most content row, in cell-local top-down coords.
            # foot_norm = distance from cell's bottom edge, normalized.
            foot_norm = (cell_size - 1 - max_y) / cell_size
            top_norm = min_y / cell_size
            bbox_h = max_y - min_y + 1
            bbox_w = max_x - min_x + 1
            print(f"  {rname}_{cname}: bbox_x=[{min_x},{max_x}] bbox_y=[{min_y},{max_y}] "
                  f"(w={bbox_w},h={bbox_h}) cx_norm={cx_norm:.4f} foot_norm={foot_norm:.4f} top_norm={top_norm:.4f}")
            row_results.append((cname, cx_norm, foot_norm, max_y, min_y))
        if row_results:
            avg_cx = sum(r_[1] for r_ in row_results) / len(row_results)
            avg_foot = sum(r_[2] for r_ in row_results) / len(row_results)
            foot_spread = max(r_[2] for r_ in row_results) - min(r_[2] for r_ in row_results)
            cx_spread = max(r_[1] for r_ in row_results) - min(r_[1] for r_ in row_results)
            print(f"  -> {rname} AVG cx_norm={avg_cx:.4f} foot_norm={avg_foot:.4f} "
                  f"| SPREAD cx={cx_spread*100:.2f}pp foot={foot_spread*100:.2f}pp")
            results[rname] = (avg_cx, avg_foot, foot_spread, cx_spread)
    return results

if __name__ == "__main__":
    for p in sys.argv[1:]:
        measure(p)
