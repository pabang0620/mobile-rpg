#!/usr/bin/env python3
"""Sapphire title-flow UI kit v2 - dark navy/charcoal + sapphire-gold GLOW
grammar (no gems, per orchestrator spec "게임시작~캐릭터선택 UI 갈아엎기",
2026-09-16).

Reuses the same procedural (non-AI-image) technique as build_ui_kit.py /
shapes.py - SuperCanvas supersamples at SS_FACTOR then LANCZOS-downsamples,
which is this project's own established fix for the "gpt-image panels looked
AI-made (uneven line weight + asymmetry)" problem (see shapes.py's module
docstring). This is a NEW, self-contained script - it imports shapes.py/
tokens.py read-only and does not modify either file or anything under
client/. Every color/number this file needs that isn't already in tokens.py
is defined locally below (V2_* constants) instead of editing tokens.py, to
keep the existing v3 beige kit's SSOT untouched.

Run: python3 build_title_kit_v2.py
Outputs every PNG into generated-images/title-kit-v2/ only.

Unit convention: identical to build_ui_kit.py - every draw call below is in
TARGET (on-screen/Unity RectTransform) px; SCALE_V2=3 means native saved px
= target px * 3, so pixelsPerUnit = 100 * native/target = 300 for every
9-sliced asset here (matches the existing CharacterSlotFrame/InputFieldFrame
convention in CharacterFlowArtImportConfigurator.cs).
"""
import os
import sys
import math

sys.path.insert(0, os.path.dirname(__file__))

from PIL import Image, ImageDraw, ImageFilter
import tokens as T
from shapes import (
    SuperCanvas, draw_beige_panel, draw_dark_capsule, draw_hex_cut_button,
    draw_gradient_disc, draw_ring_disc, draw_fading_line_h, apply_soft_shadow,
    font_kr, vgrad_rounded_rect,
)

OUT_DIR = os.path.join(
    os.path.dirname(os.path.dirname(os.path.dirname(__file__))),
    "generated-images", "title-kit-v2")
os.makedirs(OUT_DIR, exist_ok=True)

SCALE_V2 = 3
PPU_V2 = 100 * SCALE_V2  # 300

# ---------------------------------------------------------------------------
# V2 palette (local to this script - orchestrator spec: navy/charcoal base,
# sapphire-blue glow accent, champagne-gold secondary accent, no gems).
# ---------------------------------------------------------------------------
V2_BG_TOP = (26, 36, 54, 255)        # #1a2436
V2_BG_BOTTOM = (13, 20, 32, 255)     # #0d1420
V2_SAPPHIRE = (127, 184, 255, 255)   # #7fb8ff bright rim
V2_SAPPHIRE_MID = (79, 140, 255, 255)  # #4f8cff
V2_SAPPHIRE_DIM = (79, 140, 255, 140)
V2_GOLD = (232, 211, 160, 255)       # #e8d3a0 champagne gold
V2_GOLD_DIM = (232, 211, 160, 170)
V2_DANGER = (200, 90, 90, 140)       # desaturated, restrained red accent only
V2_OUTER_LINE = (8, 11, 18, 255)     # near-black outer stroke
V2_TEXT_DARK = (18, 24, 36, 255)     # for text on light (gold) buttons

PRESSED_DARKEN = 0.82  # matches tokens.PRESSED_DARKEN convention


def save(img, name):
    path = os.path.join(OUT_DIR, name)
    img.save(path)
    print(f"saved {name}: {img.size} {img.mode}")
    return path


def darken(rgba, mult):
    r, g, b, a = rgba
    return (int(r * mult), int(g * mult), int(b * mult), a)


# ---------------------------------------------------------------------------
# Shared glow helpers (not in shapes.py - additive, local to this file)
# ---------------------------------------------------------------------------

def glow_ellipse(size, center, rx, ry, color, blur):
    """Return an RGBA image of `size` with a soft blurred glow ellipse."""
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    core = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(core).ellipse(
        [center[0] - rx, center[1] - ry, center[0] + rx, center[1] + ry], fill=color)
    core = core.filter(ImageFilter.GaussianBlur(blur))
    img = Image.alpha_composite(img, core)
    return img


def radial_gradient_ellipse(size, center, rx, ry, inner_color, outer_color):
    """Radial gradient disc/ellipse: inner_color at center fading to
    outer_color (usually alpha 0) at the rim. Built via a 1D distance ramp
    to stay fast at large canvas sizes (same spirit as shapes.vertical_gradient)."""
    w, h = size
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    px = out.load()
    cx, cy = center
    steps = 64
    ramp = []
    for i in range(steps + 1):
        t = i / steps
        ramp.append(tuple(int(inner_color[c] + (outer_color[c] - inner_color[c]) * t) for c in range(4)))
    y0, y1 = max(0, int(cy - ry) - 2), min(h, int(cy + ry) + 2)
    x0, x1 = max(0, int(cx - rx) - 2), min(w, int(cx + rx) + 2)
    for y in range(y0, y1):
        ny = (y - cy) / ry
        for x in range(x0, x1):
            nx = (x - cx) / rx
            d = math.sqrt(nx * nx + ny * ny)
            if d > 1.0:
                continue
            idx = min(steps, int(d * steps))
            px[x, y] = ramp[idx]
    return out


def draw_dashed_rounded_rect(canvas: SuperCanvas, box, radius_t, color, width_t, dash_t, gap_t):
    """Dashed outline for the 'empty slot' frame. Draws a faint continuous
    base line first (so corners still read as a frame), then bright dashed
    segments along the 4 straight edges only (corner curves stay solid-faint
    - simplest way to get a convincing dashed look without arc-dash math)."""
    x0, y0, x1, y1 = canvas.sbox(box)
    radius = canvas.s(radius_t)
    w = max(1, round(canvas.s(width_t)))
    faint = (color[0], color[1], color[2], int(color[3] * 0.35))
    canvas.draw.rounded_rectangle([x0, y0, x1 - 1, y1 - 1], radius=radius, outline=faint, width=w)

    dash = canvas.s(dash_t)
    gap = canvas.s(gap_t)
    r = radius

    def dashed_h(y, xa, xb):
        x = xa
        while x < xb:
            xe = min(x + dash, xb)
            canvas.draw.line([(x, y), (xe, y)], fill=color, width=w)
            x += dash + gap

    def dashed_v(x, ya, yb):
        y = ya
        while y < yb:
            ye = min(y + dash, yb)
            canvas.draw.line([(x, y), (x, ye)], fill=color, width=w)
            y += dash + gap

    dashed_h(y0 + w / 2, x0 + r, x1 - r)
    dashed_h(y1 - w / 2, x0 + r, x1 - r)
    dashed_v(x0 + w / 2, y0 + r, y1 - r)
    dashed_v(x1 - w / 2, y0 + r, y1 - r)


def draw_glow_line_h(canvas: SuperCanvas, y_t, x0_t, x1_t, thickness_t, color, blur_t):
    """Solid horizontal line with a soft glow halo behind it (used for
    NameplateBar's top/bottom accent lines and panel dividers)."""
    x0, y0 = canvas.spt((x0_t, y_t))
    x1, _ = canvas.spt((x1_t, y_t))
    th = max(1, round(canvas.s(thickness_t)))
    blur = canvas.s(blur_t)
    w = int(x1 - x0)
    h = int(blur * 6 + th * 4)
    strip = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    cy = h // 2
    ImageDraw.Draw(strip).line([(0, cy), (w, cy)], fill=color, width=th)
    glow = strip.filter(ImageFilter.GaussianBlur(blur))
    glow = Image.alpha_composite(glow, strip)
    canvas.img.alpha_composite(glow, (int(x0), int(y0 - h / 2)))


# ---------------------------------------------------------------------------
# 1. CharacterPedestal.png - glowing spotlight disc under the character's
#    feet (MapleStory-select-screen reference). Square canvas, character art
#    (full-body) is composited ON TOP of this by the scene, so the pedestal
#    itself only needs the floor glow, not a full ring around the body.
# ---------------------------------------------------------------------------

def build_pedestal():
    target = 240
    c = SuperCanvas(target, target, scale=SCALE_V2)
    ellipse_cy = target * 0.80
    rx, ry = target * 0.42, target * 0.135

    # outer soft halo (sapphire), largest + dimmest
    halo = glow_ellipse((c.sw, c.sh), c.spt((target / 2, ellipse_cy)), c.s(rx * 1.55), c.s(ry * 1.9),
                         (V2_SAPPHIRE_MID[0], V2_SAPPHIRE_MID[1], V2_SAPPHIRE_MID[2], 70), blur=c.s(14))
    c.img.alpha_composite(halo)

    # mid glow ring (brighter sapphire)
    mid = glow_ellipse((c.sw, c.sh), c.spt((target / 2, ellipse_cy)), c.s(rx * 1.1), c.s(ry * 1.3),
                        (V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 110), blur=c.s(6))
    c.img.alpha_composite(mid)

    # bright core light-pool (warm gold-white, where the character "stands")
    core = radial_gradient_ellipse(
        (c.sw, c.sh), c.spt((target / 2, ellipse_cy)), c.s(rx * 0.9), c.s(ry * 0.85),
        inner_color=(255, 250, 235, 150), outer_color=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 0))
    c.img.alpha_composite(core)

    # crisp thin rim ellipse (gold), the only hard-edge element
    cx_ss, cy_ss = c.spt((target / 2, ellipse_cy))
    rx_ss, ry_ss = c.s(rx), c.s(ry)
    lw = max(1, round(c.s(1.6)))
    canvas_draw_ellipse_outline(c, (cx_ss, cy_ss), rx_ss, ry_ss, (V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 210), lw)

    # inner secondary rim (sapphire), slightly smaller, for a two-tone edge
    lw2 = max(1, round(c.s(1.2)))
    canvas_draw_ellipse_outline(c, (cx_ss, cy_ss), rx_ss * 0.82, ry_ss * 0.82,
                                 (V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 160), lw2)

    return c.finish()


def canvas_draw_ellipse_outline(canvas: SuperCanvas, center_ss, rx_ss, ry_ss, color, width_px):
    cx, cy = center_ss
    canvas.draw.ellipse([cx - rx_ss, cy - ry_ss, cx + rx_ss, cy + ry_ss], outline=color, width=width_px)


# ---------------------------------------------------------------------------
# 2/3. CharacterSlotFrameV2 / CharacterSlotFrameEmptyV2 - vertical 9-slice
#      dark-glass card frame. Target 260x400 (matches CharacterSelectScene
#      Builder's CardWidth=260; height is a 9-slice design height, the
#      actual on-screen CardHeight=340/CardCreate=250 both stretch the same
#      borders fine since only the border thickness is fixed by 9-slice).
# ---------------------------------------------------------------------------

FRAME_W, FRAME_H = 260, 400
FRAME_BORDER = 16       # target px - 9-slice border on all 4 edges
FRAME_RADIUS = 10
FRAME_MARGIN = 4         # small margin so the glow line isn't clipped at the canvas edge
BADGE_NOTCH_R = 30       # top badge cutout radius (target px)
BADGE_NOTCH_CY = 34      # badge notch center, distance from top edge
NAMEPLATE_BAND_H = 76    # bottom band height reserved for the nameplate/buttons area


def build_slot_frame(empty=False):
    c = SuperCanvas(FRAME_W, FRAME_H, scale=SCALE_V2)
    box = (FRAME_MARGIN, FRAME_MARGIN, FRAME_W - FRAME_MARGIN, FRAME_H - FRAME_MARGIN)

    if not empty:
        draw_beige_panel(
            c, box, radius_t=FRAME_RADIUS,
            outer_line=V2_OUTER_LINE, outer_w_t=1.8,
            inner_line=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 190), inner_w_t=1.2,
            inner_inset_t=4.0, top_color=V2_BG_TOP, bottom_color=V2_BG_BOTTOM)

        # top badge notch: recessed darker circle + thin glow ring, class
        # badge PNGs (ClassBadgeMage/Warrior) overlay exactly on this.
        notch_center = (FRAME_W / 2, BADGE_NOTCH_CY + FRAME_MARGIN)
        draw_gradient_disc(c, notch_center, BADGE_NOTCH_R,
                            top_color=(6, 9, 15), bottom_color=(2, 3, 6),
                            ring_color=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 200), ring_w_t=1.5)

        # divider glow line separating portrait area from the bottom
        # nameplate/button band
        divider_y = FRAME_H - NAMEPLATE_BAND_H
        draw_glow_line_h(c, divider_y, FRAME_MARGIN + 14, FRAME_W - FRAME_MARGIN - 14,
                          thickness_t=1.2, color=(V2_SAPPHIRE_MID[0], V2_SAPPHIRE_MID[1], V2_SAPPHIRE_MID[2], 150),
                          blur_t=2.0)
    else:
        # empty slot: translucent fill, dashed glow outline, no badge notch,
        # a soft "+" glow at the vertical center to invite creation.
        x0, y0, x1, y1 = c.sbox(box)
        radius = c.s(FRAME_RADIUS)
        fill_img = vgrad_rounded_rect((int(x1 - x0), int(y1 - y0)), radius,
                                       (V2_BG_TOP[0], V2_BG_TOP[1], V2_BG_TOP[2], 120),
                                       (V2_BG_BOTTOM[0], V2_BG_BOTTOM[1], V2_BG_BOTTOM[2], 120))
        c.img.paste(fill_img, (int(x0), int(y0)), fill_img)

        draw_dashed_rounded_rect(c, box, radius_t=FRAME_RADIUS,
                                  color=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 200),
                                  width_t=1.6, dash_t=10, gap_t=7)

        plus_cy = FRAME_H / 2 - 10
        plus_size = 30
        pw = max(1, round(c.s(3.0)))
        cx_ss, cy_ss = c.spt((FRAME_W / 2, plus_cy))
        half_ss = c.s(plus_size / 2)
        glow_plus = Image.new("RGBA", (c.sw, c.sh), (0, 0, 0, 0))
        d = ImageDraw.Draw(glow_plus)
        d.line([(cx_ss - half_ss, cy_ss), (cx_ss + half_ss, cy_ss)], fill=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 220), width=pw)
        d.line([(cx_ss, cy_ss - half_ss), (cx_ss, cy_ss + half_ss)], fill=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 220), width=pw)
        glow_plus = glow_plus.filter(ImageFilter.GaussianBlur(c.s(1.2)))
        d2 = ImageDraw.Draw(glow_plus)
        d2.line([(cx_ss - half_ss, cy_ss), (cx_ss + half_ss, cy_ss)], fill=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 255), width=pw)
        d2.line([(cx_ss, cy_ss - half_ss), (cx_ss, cy_ss + half_ss)], fill=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 255), width=pw)
        c.img.alpha_composite(glow_plus)

    return c.finish()


# ---------------------------------------------------------------------------
# 4/5. ClassBadgeMage / ClassBadgeWarrior - small circular class icon badge
#      that overlays CharacterSlotFrameV2's top notch.
# ---------------------------------------------------------------------------

BADGE_TARGET = 72  # diameter-ish canvas; disc radius drawn slightly inside


def build_class_badge(kind):
    c = SuperCanvas(BADGE_TARGET, BADGE_TARGET, scale=SCALE_V2)
    center = (BADGE_TARGET / 2, BADGE_TARGET / 2)
    r = BADGE_TARGET / 2 - 4

    draw_gradient_disc(c, center, r, top_color=(20, 27, 40), bottom_color=(8, 11, 18),
                        ring_color=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 220), ring_w_t=1.8)
    # inner faint accent ring
    draw_ring_disc(c, center, r - 5, disc_fill=None,
                    ring_color=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 90), ring_w_t=1.0)

    cx, cy = c.spt(center)

    if kind == "mage":
        # staff: vertical line + small 4-point sparkle/star at the top
        # (explicitly a sparkle glyph, not a faceted gem - no gem shapes).
        lw = max(1, round(c.s(2.2)))
        staff_top_t, staff_bot_t = 22, 54
        x_t = BADGE_TARGET / 2
        p0 = c.spt((x_t, staff_top_t))
        p1 = c.spt((x_t, staff_bot_t))
        c.draw.line([p0, p1], fill=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 255), width=lw)

        star_cy_t = 20
        star_r_t = 9
        star_cx, star_cy = c.spt((x_t, star_cy_t))
        star_r = c.s(star_r_t)
        pts = []
        for i in range(8):
            ang = i * math.pi / 4
            rr = star_r if i % 2 == 0 else star_r * 0.4
            pts.append((star_cx + rr * math.cos(ang), star_cy + rr * math.sin(ang)))
        glow = Image.new("RGBA", (c.sw, c.sh), (0, 0, 0, 0))
        ImageDraw.Draw(glow).polygon(pts, fill=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 230))
        glow = glow.filter(ImageFilter.GaussianBlur(c.s(1.0)))
        c.img.alpha_composite(glow)
        c.draw.polygon(pts, fill=(230, 240, 255, 255))
    else:
        # sword silhouette (blade + crossguard + short hilt) over a small
        # shield chevron - simple geometric shapes, symmetric on the
        # vertical axis.
        x_t = BADGE_TARGET / 2
        blade_top = c.spt((x_t, 16))
        blade_bot = c.spt((x_t, 46))
        lw = max(1, round(c.s(3.0)))
        c.draw.line([blade_top, blade_bot], fill=(230, 236, 245, 255), width=lw)
        guard_y = 42
        g0 = c.spt((x_t - 10, guard_y))
        g1 = c.spt((x_t + 10, guard_y))
        gw = max(1, round(c.s(2.4)))
        c.draw.line([g0, g1], fill=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 255), width=gw)
        hilt_top = c.spt((x_t, guard_y))
        hilt_bot = c.spt((x_t, 54))
        c.draw.line([hilt_top, hilt_bot], fill=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 255), width=lw)
        # small shield chevron behind, low center
        chev_pts_t = [(x_t - 12, 50), (x_t, 44), (x_t + 12, 50), (x_t, 58)]
        chev_pts = [c.spt(p) for p in chev_pts_t]
        c.draw.polygon(chev_pts, outline=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 160),
                        width=max(1, round(c.s(1.4))))

    return c.finish()


# ---------------------------------------------------------------------------
# 6. NameplateBar.png - horizontal 9-slice pill for name/level text.
# ---------------------------------------------------------------------------

NAMEPLATE_W, NAMEPLATE_H = 200, 46
NAMEPLATE_BORDER_LR = 14
NAMEPLATE_BORDER_TB = 12


def build_nameplate():
    c = SuperCanvas(NAMEPLATE_W, NAMEPLATE_H, scale=SCALE_V2)
    margin = 2
    box = (margin, margin, NAMEPLATE_W - margin, NAMEPLATE_H - margin)
    fill = (V2_BG_TOP[0], V2_BG_TOP[1], V2_BG_TOP[2], 190)
    line = (V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 130)
    draw_dark_capsule(c, box, fill=fill, line=line, line_w_t=1.0)

    top_y = margin + 4
    bot_y = NAMEPLATE_H - margin - 4
    draw_glow_line_h(c, top_y, margin + 16, NAMEPLATE_W - margin - 16, thickness_t=1.1,
                      color=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 190), blur_t=1.4)
    draw_glow_line_h(c, bot_y, margin + 16, NAMEPLATE_W - margin - 16, thickness_t=1.1,
                      color=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 170), blur_t=1.4)

    return c.finish()


# ---------------------------------------------------------------------------
# 7. Buttons: ButtonSelectV2 / ButtonDeleteV2 / ButtonCreateV2. Each output
#    is a 2-cell horizontal sheet: [Normal | Pressed], fully transparent
#    gutter between cells (per absolute rule 0/4 - alpha-connected-component
#    slicing, no baked pixel grid assumed).
# ---------------------------------------------------------------------------

BTN_CELL_W = 96
BTN_CELL_H = 40
BTN_GUTTER = 24
BTN_CUT_T = BTN_CELL_H * 0.30

CREATE_CELL_W = 160
CREATE_CELL_H = 56
CREATE_CUT_T = CREATE_CELL_H * 0.30


def build_button_sheet(cell_w, cell_h, top_color, bottom_color, inner_line_rgba, chevron_color, cut_t, outer_line=V2_OUTER_LINE):
    sheet_w = cell_w * 2 + BTN_GUTTER
    sheet_h = cell_h
    c = SuperCanvas(sheet_w, sheet_h, scale=SCALE_V2)

    normal_box = (0, 0, cell_w, cell_h)
    draw_hex_cut_button(c, normal_box, cut_t=cut_t, top_color=top_color, bottom_color=bottom_color,
                         outer_line=outer_line, outer_w_t=1.6, inner_line_rgba=inner_line_rgba,
                         inner_w_t=1.0, chevron_color=chevron_color)

    pressed_box = (cell_w + BTN_GUTTER, 0, cell_w * 2 + BTN_GUTTER, cell_h)
    pressed_top = darken(top_color + (255,), PRESSED_DARKEN)[:3] if len(top_color) == 3 else darken(top_color, PRESSED_DARKEN)[:3]
    pressed_bottom = darken(bottom_color + (255,), PRESSED_DARKEN)[:3] if len(bottom_color) == 3 else darken(bottom_color, PRESSED_DARKEN)[:3]
    draw_hex_cut_button(c, pressed_box, cut_t=cut_t, top_color=pressed_top, bottom_color=pressed_bottom,
                         outer_line=outer_line, outer_w_t=1.6,
                         inner_line_rgba=(inner_line_rgba[0], inner_line_rgba[1], inner_line_rgba[2], max(0, inner_line_rgba[3] - 60)),
                         inner_w_t=1.0, chevron_color=chevron_color)

    return c.finish()


def build_button_select():
    return build_button_sheet(
        BTN_CELL_W, BTN_CELL_H,
        top_color=(58, 111, 216), bottom_color=(36, 80, 168),
        inner_line_rgba=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 200),
        chevron_color=(235, 244, 255, 220), cut_t=BTN_CUT_T)


def build_button_delete():
    return build_button_sheet(
        BTN_CELL_W, BTN_CELL_H,
        top_color=(52, 56, 63), bottom_color=(36, 39, 44),
        inner_line_rgba=(V2_DANGER[0], V2_DANGER[1], V2_DANGER[2], 130),
        chevron_color=(210, 200, 200, 180), cut_t=BTN_CUT_T,
        outer_line=(4, 5, 6, 255))


def build_button_create():
    return build_button_sheet(
        CREATE_CELL_W, CREATE_CELL_H,
        top_color=(240, 223, 174), bottom_color=(203, 176, 115),
        inner_line_rgba=(255, 250, 235, 200),
        chevron_color=(40, 32, 16, 210), cut_t=CREATE_CUT_T,
        outer_line=(60, 46, 20, 255))


# ---------------------------------------------------------------------------
# 8. CharacterCreateSpotlight.png - big radial glow behind the selected
#    class card. Landscape, transparent.
# ---------------------------------------------------------------------------

SPOTLIGHT_W, SPOTLIGHT_H = 480, 260


def build_spotlight():
    c = SuperCanvas(SPOTLIGHT_W, SPOTLIGHT_H, scale=SCALE_V2)
    center = (SPOTLIGHT_W / 2, SPOTLIGHT_H / 2)

    outer = radial_gradient_ellipse(
        (c.sw, c.sh), c.spt(center), c.s(SPOTLIGHT_W * 0.5), c.s(SPOTLIGHT_H * 0.5),
        inner_color=(V2_SAPPHIRE_MID[0], V2_SAPPHIRE_MID[1], V2_SAPPHIRE_MID[2], 90),
        outer_color=(V2_SAPPHIRE_MID[0], V2_SAPPHIRE_MID[1], V2_SAPPHIRE_MID[2], 0))
    outer = outer.filter(ImageFilter.GaussianBlur(c.s(6)))
    c.img.alpha_composite(outer)

    mid = radial_gradient_ellipse(
        (c.sw, c.sh), c.spt(center), c.s(SPOTLIGHT_W * 0.32), c.s(SPOTLIGHT_H * 0.32),
        inner_color=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 130),
        outer_color=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 0))
    mid = mid.filter(ImageFilter.GaussianBlur(c.s(4)))
    c.img.alpha_composite(mid)

    core = radial_gradient_ellipse(
        (c.sw, c.sh), c.spt(center), c.s(SPOTLIGHT_W * 0.14), c.s(SPOTLIGHT_H * 0.14),
        inner_color=(255, 253, 245, 170), outer_color=(255, 253, 245, 0))
    c.img.alpha_composite(core)

    return c.finish()


# ---------------------------------------------------------------------------
# 9. LoginPortalFrame.png - 9-slice frame wrapping the login ID input +
#    start button block.
# ---------------------------------------------------------------------------

PORTAL_W, PORTAL_H = 400, 260
PORTAL_BORDER = 20
PORTAL_RADIUS = 14
PORTAL_MARGIN = 4


def build_login_portal_frame():
    c = SuperCanvas(PORTAL_W, PORTAL_H, scale=SCALE_V2)
    box = (PORTAL_MARGIN, PORTAL_MARGIN, PORTAL_W - PORTAL_MARGIN, PORTAL_H - PORTAL_MARGIN)

    draw_beige_panel(
        c, box, radius_t=PORTAL_RADIUS,
        outer_line=V2_OUTER_LINE, outer_w_t=1.8,
        inner_line=(V2_SAPPHIRE[0], V2_SAPPHIRE[1], V2_SAPPHIRE[2], 190), inner_w_t=1.2,
        inner_inset_t=5.0, top_color=(V2_BG_TOP[0], V2_BG_TOP[1], V2_BG_TOP[2], 200),
        bottom_color=(V2_BG_BOTTOM[0], V2_BG_BOTTOM[1], V2_BG_BOTTOM[2], 200))

    # small diamond "rune" accent ticks at each corner - kept fully inside
    # the fixed border-thickness zone (PORTAL_BORDER) so 9-slice stretching
    # of the middle never distorts them.
    tick_r = 4.5
    inset = 13
    corners = [
        (PORTAL_MARGIN + inset, PORTAL_MARGIN + inset),
        (PORTAL_W - PORTAL_MARGIN - inset, PORTAL_MARGIN + inset),
        (PORTAL_MARGIN + inset, PORTAL_H - PORTAL_MARGIN - inset),
        (PORTAL_W - PORTAL_MARGIN - inset, PORTAL_H - PORTAL_MARGIN - inset),
    ]
    for cx_t, cy_t in corners:
        cx, cy = c.spt((cx_t, cy_t))
        r = c.s(tick_r)
        pts = [(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)]
        glow = Image.new("RGBA", (c.sw, c.sh), (0, 0, 0, 0))
        ImageDraw.Draw(glow).polygon(pts, outline=(V2_GOLD[0], V2_GOLD[1], V2_GOLD[2], 220), width=max(1, round(c.s(1.2))))
        c.img.alpha_composite(glow)

    return c.finish()


# ---------------------------------------------------------------------------
# Verification helpers
# ---------------------------------------------------------------------------

def verify_alpha(path, name):
    im = Image.open(path)
    if im.mode != "RGBA":
        print(f"  [ALPHA-FAIL] {name}: mode={im.mode}, no alpha channel")
        return
    w, h = im.size
    arr = im.load()
    corner_opaque = 0
    for (x, y) in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]:
        r, g, b, a = arr[x, y]
        if a > 200:
            corner_opaque += 1
    print(f"  [ALPHA-OK] {name}: {w}x{h} mode={im.mode}, opaque-corners={corner_opaque}/4")


def main():
    outputs = {}
    outputs["CharacterPedestal.png"] = build_pedestal()
    outputs["CharacterSlotFrameV2.png"] = build_slot_frame(empty=False)
    outputs["CharacterSlotFrameEmptyV2.png"] = build_slot_frame(empty=True)
    outputs["ClassBadgeMage.png"] = build_class_badge("mage")
    outputs["ClassBadgeWarrior.png"] = build_class_badge("warrior")
    outputs["NameplateBar.png"] = build_nameplate()
    outputs["ButtonSelectV2.png"] = build_button_select()
    outputs["ButtonDeleteV2.png"] = build_button_delete()
    outputs["ButtonCreateV2.png"] = build_button_create()
    outputs["CharacterCreateSpotlight.png"] = build_spotlight()
    outputs["LoginPortalFrame.png"] = build_login_portal_frame()

    print("\n--- alpha verification ---")
    for name, img in outputs.items():
        path = save(img, name)
        verify_alpha(path, name)


if __name__ == "__main__":
    main()
