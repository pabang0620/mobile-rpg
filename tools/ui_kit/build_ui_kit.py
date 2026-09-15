#!/usr/bin/env python3
"""Sapphire UI kit v3 - procedural (non-AI-image) MapleStory-grammar rebuild.

Run: python3 build_ui_kit.py
Outputs every PNG into generated-images/ui-kit/ (never touches client/).
All colors/widths/radii come from tokens.py - change values there, not here,
to keep every asset consistent (single source of truth, per orchestrator
instruction).

Unit convention (see shapes.SuperCanvas's docstring for the full incident
report): every function below builds a SuperCanvas(target_w, target_h,
scale=SCALE) and then passes box/center/radius/width arguments to shapes.py
functions in that SAME target-space (the plain design-token numbers, e.g.
"radius=4" really means 4 target px) - never pre-multiplied by SCALE.
SuperCanvas.s()/.sbox()/.spt() do the target->native->supersample
conversion in one place. All new/redesigned 9-slice assets use SCALE=3
(native saved px = target on-screen px * 3), so pixelsPerUnit = 100 *
native/target = 300 for every one of them.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))

from PIL import Image, ImageDraw
import tokens as T
from PIL import ImageFont
from shapes import (
    SuperCanvas, draw_beige_panel, draw_dark_capsule, draw_pin_icon,
    draw_hex_cut_button, draw_ring_disc, draw_gradient_disc, draw_hexagon_badge,
    draw_fading_line_h, apply_soft_shadow,
)

OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(__file__))), "generated-images", "ui-kit")
os.makedirs(OUT_DIR, exist_ok=True)

SCALE = 3  # native saved px = target on-screen px * SCALE for every asset below
PPU = 100 * SCALE  # = 300, universal pixelsPerUnit for every 9-sliced asset here


def save(img, name):
    path = os.path.join(OUT_DIR, name)
    img.save(path)
    print(f"saved {name}: {img.size} {img.mode}")
    return path


# ---------------------------------------------------------------------------
# 1-3. Beige parchment panels: MenuPanelOdin / MessagePanelFrameGold /
#      CharacterSlotFrame (all 9-slice, same double-line treatment).
# ---------------------------------------------------------------------------

def build_beige_panel(target_w, target_h, margin_t=6):
    c = SuperCanvas(target_w, target_h, scale=SCALE)
    box_t = (margin_t, margin_t, target_w - margin_t, target_h - margin_t)
    draw_beige_panel(
        c, box_t,
        radius_t=4, outer_line=T.PANEL_OUTER_LINE, outer_w_t=1.5,
        inner_line=T.PANEL_INNER_LINE, inner_w_t=1.0, inner_inset_t=3.0,
        top_color=T.PANEL_TOP, bottom_color=T.PANEL_BOTTOM)
    return c.finish()


def build_menu_panel_odin():
    save(build_beige_panel(400, 1000), "MenuPanelOdin.png")  # height is a layout choice, stretched at runtime


def build_message_panel():
    save(build_beige_panel(560, 320), "MessagePanelFrameGold.png")


def build_character_slot_frame():
    save(build_beige_panel(260, 650), "CharacterSlotFrame.png")


def build_input_field_frame():
    # Draws directly with c.draw/c.img (no shapes.py helper for a flat-fill
    # rounded rect exists) - every coordinate below is explicit TARGET-space
    # until it's run through c.sbox()/c.s() right at the c.draw call.
    target_w, target_h = 360, 60
    c = SuperCanvas(target_w, target_h, scale=SCALE)
    m = 6
    box = c.sbox((m, m, target_w - m, target_h - m))
    radius = c.s(4)
    fill_mask = Image.new("L", (c.sw, c.sh), 0)
    ImageDraw.Draw(fill_mask).rounded_rectangle(box, radius=radius, fill=255)
    fill_layer = Image.new("RGBA", (c.sw, c.sh), T.INPUT_FILL)
    c.img.paste(fill_layer, (0, 0), fill_mask)
    c.draw.rounded_rectangle([box[0], box[1], box[2] - 1, box[3] - 1], radius=radius,
                              outline=T.INPUT_LINE, width=max(1, round(c.s(T.INPUT_LINE_W))))
    save(c.finish(), "InputFieldFrame.png")


# ---------------------------------------------------------------------------
# 4. MenuSectionDivider - thin fading line, Left+Right halves, no tick/box.
#    Also reused for any panel-title underline (same visual language).
# ---------------------------------------------------------------------------

def build_menu_section_divider():
    # 2026-09-15 orchestrator fix: dropped the chevron ("V") marks - they
    # read as stray letters next to the section title ("V 성장 V"). Now:
    # plain line, fading out at the OUTER edge, stopping DIVIDER_TEXT_GAP
    # short of the text edge, with one small dot at the line's inner end.
    target_strip_h, target_cell_w, target_fade_w = 20, 200, 40
    gap = T.DIVIDER_TEXT_GAP
    dot_r = T.DIVIDER_DOT_DIAMETER / 2
    c = SuperCanvas(target_cell_w * 2, target_strip_h, scale=SCALE)
    mid_y = target_strip_h / 2

    # Left half: outer edge (x=0) fades in, line runs up to (target_cell_w -
    # gap - dot diameter), then the dot, then a `gap`-wide empty zone before
    # the text starts at x=target_cell_w.
    line_end_l = target_cell_w - gap - T.DIVIDER_DOT_DIAMETER
    draw_fading_line_h(c, mid_y, 0, line_end_l, T.DIVIDER_LINE_W, T.DIVIDER_LINE, fade_from="left", fade_len_t=target_fade_w)
    draw_ring_disc(c, (line_end_l + dot_r, mid_y), dot_r, T.DIVIDER_DOT, None, 0)

    # Right half: mirror.
    line_start_r = target_cell_w + gap + T.DIVIDER_DOT_DIAMETER
    draw_fading_line_h(c, mid_y, line_start_r, target_cell_w * 2, T.DIVIDER_LINE_W, T.DIVIDER_LINE, fade_from="right", fade_len_t=target_fade_w)
    draw_ring_disc(c, (line_start_r - dot_r, mid_y), dot_r, T.DIVIDER_DOT, None, 0)

    save(c.finish(), "MenuSectionDivider.png")


# ---------------------------------------------------------------------------
# 5. RegionNameplate - dark translucent capsule with a pin icon, item 2.
# ---------------------------------------------------------------------------

def build_region_nameplate():
    target_h, left_zone, right_zone, mid = 32, 50, 24, 106
    target_w = left_zone + mid + right_zone
    c = SuperCanvas(target_w, target_h, scale=SCALE)
    draw_dark_capsule(c, (0, 0, target_w, target_h), T.REGION_FILL, T.REGION_LINE, T.REGION_LINE_W)
    draw_pin_icon(c, (left_zone * 0.5, target_h / 2), size_t=16, color=T.REGION_PIN_COLOR)
    save(c.finish(), "RegionNameplate.png")


# ---------------------------------------------------------------------------
# 6. ButtonSecondary / ButtonPrimary - hex-cut pill, normal+pressed cells.
# ---------------------------------------------------------------------------

def _darken(rgb, f):
    return tuple(int(v * f) for v in rgb[:3])


def build_hex_button(name, top_color, bottom_color):
    target_w, target_h, gutter = 344, 56, 40
    c = SuperCanvas(target_w, target_h * 2 + gutter, scale=SCALE)
    inner_line_rgba = (255, 255, 255, T.BTN_INNER_LINE_ALPHA)
    cut_t = T.BTN_CUT_RATIO * target_h

    draw_hex_cut_button(c, (0, 0, target_w, target_h), cut_t,
                         top_color, bottom_color, T.BTN_OUTER_LINE, T.BTN_OUTER_LINE_W,
                         inner_line_rgba, T.BTN_INNER_LINE_W, T.BTN_CHEVRON_COLOR)

    py0 = target_h + gutter
    draw_hex_cut_button(c, (0, py0, target_w, py0 + target_h), cut_t,
                         _darken(top_color, T.PRESSED_DARKEN), _darken(bottom_color, T.PRESSED_DARKEN),
                         T.BTN_OUTER_LINE, T.BTN_OUTER_LINE_W, inner_line_rgba, T.BTN_INNER_LINE_W, T.BTN_CHEVRON_COLOR)

    save(c.finish(), name)


# ---------------------------------------------------------------------------
# 7. MenuHamburgerIcon - background-less line icon + soft shadow, item 6.
# ---------------------------------------------------------------------------

def build_hamburger_icon():
    target_w, target_h, pad = 40, 32, 20  # pad = transparent margin for the blur
    canvas_w_t, canvas_h_t = target_w + pad * 2, target_h + pad * 2
    c = SuperCanvas(canvas_w_t, canvas_h_t, scale=SCALE)
    cx_t, cy_t = canvas_w_t / 2, canvas_h_t / 2
    line_w = max(1, round(c.s(4)))
    for dy_t in (-9, 0, 9):
        y = c.s(cy_t + dy_t)
        x0, x1 = c.s(cx_t - target_w / 2), c.s(cx_t + target_w / 2)
        c.draw.line([(x0, y), (x1, y)], fill=T.HAMBURGER_LINE_COLOR, width=line_w)
    img = c.finish()
    img = apply_soft_shadow(img, blur=3, offset=(0, 2), color=T.HAMBURGER_SHADOW_COLOR)
    save(img, "MenuHamburgerIcon.png")


# ---------------------------------------------------------------------------
# 8. LevelBadgeHex - flat-top hexagon badge, item 1.
# ---------------------------------------------------------------------------

def build_level_badge():
    target_r, pad = 24, 8
    side_t = target_r * 2 + pad * 2
    c = SuperCanvas(side_t, side_t, scale=SCALE)
    draw_hexagon_badge(c, (side_t / 2, side_t / 2), target_r, T.LEVEL_BADGE_FILL, T.LEVEL_BADGE_LINE, T.LEVEL_BADGE_LINE_W)
    save(c.finish(), "LevelBadgeHex.png")


# ---------------------------------------------------------------------------
# 9-10. HealthBarFrameGold (Track+Fill) + GaugeFillMana - slim flat bars.
# ---------------------------------------------------------------------------

def _draw_bar_capsule(c, box_t, fill, line, line_w_t, highlight=None):
    draw_dark_capsule(c, box_t, fill, line, line_w_t)
    if highlight is not None:
        x0, y0, x1, y1 = c.sbox(box_t)
        hi_h = c.s(2)
        hx0, hx1 = x0 + c.s(6), x1 - c.s(6)
        hy = y0 + c.s(4)
        c.draw.rounded_rectangle([hx0, hy, hx1, hy + hi_h], radius=hi_h / 2, fill=highlight)


def build_health_bar_frame():
    target_w, target_h, gutter = 260, 16, 8
    c = SuperCanvas(target_w, target_h * 2 + gutter, scale=SCALE)

    _draw_bar_capsule(c, (0, 0, target_w, target_h), T.BAR_TRACK_FILL, T.BAR_TRACK_LINE, 1.0)
    fill_y0 = target_h + gutter
    _draw_bar_capsule(c, (0, fill_y0, target_w, fill_y0 + target_h), T.HP_FILL_COLOR, None, 0, highlight=T.BAR_FILL_HIGHLIGHT)

    save(c.finish(), "HealthBarFrameGold.png")


def build_mana_fill():
    target_w, target_h = 260, 16
    c = SuperCanvas(target_w, target_h, scale=SCALE)
    _draw_bar_capsule(c, (0, 0, target_w, target_h), T.MP_FILL_COLOR, None, 0, highlight=T.BAR_FILL_HIGHLIGHT)
    save(c.finish(), "GaugeFillMana.png")


# ---------------------------------------------------------------------------
# 11. SkillButtonFrameGold - Skill ring (small, plain) + BasicAttack ring
#     (large, warm brown gradient disc), item 3. Diameters match this
#     codebase's own live constants (VillageHubSkillMenuBuilder:
#     skillButtonSize=80, basicAttackSize=132) so the report's recommended
#     Image sizeDelta needs no change, only the sprite art does.
# ---------------------------------------------------------------------------

def build_skill_button_frame():
    skill_d, attack_d, gutter = 80, 132, 40
    canvas_w_t = skill_d + gutter + attack_d
    canvas_h_t = attack_d
    c = SuperCanvas(canvas_w_t, canvas_h_t, scale=SCALE)

    skill_center = (skill_d / 2, canvas_h_t / 2)
    draw_ring_disc(c, skill_center, skill_d / 2, T.SKILL_DISC_FILL, T.SKILL_RING_COLOR, T.SKILL_RING_W)

    attack_center = (skill_d + gutter + attack_d / 2, canvas_h_t / 2)
    draw_gradient_disc(c, attack_center, attack_d / 2, T.ATTACK_DISC_TOP, T.ATTACK_DISC_BOTTOM, T.ATTACK_RING_COLOR, T.ATTACK_RING_W)

    save(c.finish(), "SkillButtonFrameGold.png")


# ---------------------------------------------------------------------------
# 12. MovementStickGold - thin ring base (near-invisible fill) + translucent
#     knob, item 3. Diameters match VillageHubUiBuilder.BuildVirtualMovementPad
#     (padSize=160, knobSize=64).
# ---------------------------------------------------------------------------

def build_movement_stick():
    base_d, knob_d, gutter = 160, 64, 40
    canvas_w_t = base_d + gutter + knob_d
    canvas_h_t = base_d
    c = SuperCanvas(canvas_w_t, canvas_h_t, scale=SCALE)

    base_center = (base_d / 2, canvas_h_t / 2)
    draw_ring_disc(c, base_center, base_d / 2 - 2, T.STICK_BASE_FILL, T.STICK_RING_COLOR, T.STICK_RING_W)

    knob_center = (base_d + gutter + knob_d / 2, canvas_h_t / 2)
    draw_ring_disc(c, knob_center, knob_d / 2 - 2, T.STICK_KNOB_FILL, T.STICK_KNOB_LINE, 1.5)

    save(c.finish(), "MovementStickGold.png")


# ---------------------------------------------------------------------------
# 13. TitleLogo - bold display wordmark, item 8. 2026-09-15 orchestrator fix:
#     plain Noto Sans Bold read as "default gothic" - switched to Baloo 2
#     ExtraBold (Latin, variable font pinned to wght=800) + Jua (Korean
#     subtitle, on its own navy ribbon). scale=1 (default): this asset works
#     directly in its own final canvas size (1942x809, unchanged from the
#     original file), same as before the earlier bugfix.
# ---------------------------------------------------------------------------

def _layered_logo_text(c: SuperCanvas, text, font, cy):
    """3-layer text: white-alpha outer ring -> dark navy ring -> 2-stop
    white->pale-blue gradient fill, all sharing one (x,y) anchor so the
    rings stay perfectly concentric (PIL's text stroke always grows
    outward from the same glyph outline regardless of stroke_width)."""
    bbox = c.draw.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = (c.sw - tw) / 2 - bbox[0]
    y = cy - th / 2 - bbox[1]

    outer_stroke = max(1, round(c.s(T.LOGO_OUTLINE_W + T.LOGO_OUTER_LINE_W)))
    inner_stroke = max(1, round(c.s(T.LOGO_OUTLINE_W)))
    c.draw.text((x, y), text, font=font, fill=T.LOGO_OUTER_LINE_COLOR,
                stroke_width=outer_stroke, stroke_fill=T.LOGO_OUTER_LINE_COLOR)
    c.draw.text((x, y), text, font=font, fill=T.LOGO_OUTLINE_COLOR,
                stroke_width=inner_stroke, stroke_fill=T.LOGO_OUTLINE_COLOR)

    mask = Image.new("L", (c.sw, c.sh), 0)
    ImageDraw.Draw(mask).text((x, y), text, font=font, fill=255)
    from shapes import vertical_gradient
    grad = vertical_gradient((c.sw, c.sh), T.LOGO_TEXT_TOP, T.LOGO_TEXT_BOTTOM)
    c.img.paste(grad, (0, 0), mask)
    return bbox, x, y


def build_title_logo():
    w, h = 1942, 809
    c = SuperCanvas(w, h)

    latin = ImageFont.truetype(T.FONT_LOGO_LATIN, c.s(200))
    latin.set_variation_by_axes([T.FONT_LOGO_LATIN_WGHT])
    _layered_logo_text(c, "Sapphire", latin, c.s(260))

    # Korean subtitle sits on its own small navy ribbon (not stroked/
    # gradient-filled like the wordmark) - draw the ribbon first, text on top.
    hangul = ImageFont.truetype(T.FONT_LOGO_KR, c.s(52))
    sub_bbox = c.draw.textbbox((0, 0), "달빛의 균열", font=hangul)
    sub_w, sub_h = sub_bbox[2] - sub_bbox[0], sub_bbox[3] - sub_bbox[1]
    pad_x, pad_y = c.s(28), c.s(14)
    ribbon_cy = c.s(460)
    ribbon_box = [c.sw / 2 - sub_w / 2 - pad_x, ribbon_cy - sub_h / 2 - pad_y,
                  c.sw / 2 + sub_w / 2 + pad_x, ribbon_cy + sub_h / 2 + pad_y]
    c.draw.rounded_rectangle(ribbon_box, radius=c.s(10), fill=T.LOGO_RIBBON_FILL)
    c.draw.text((c.sw / 2 - sub_w / 2 - sub_bbox[0], ribbon_cy - sub_h / 2 - sub_bbox[1]),
                "달빛의 균열", font=hangul, fill=T.LOGO_RIBBON_TEXT)

    img = c.finish()
    img = apply_soft_shadow(img, blur=T.LOGO_SHADOW_BLUR, offset=T.LOGO_SHADOW_OFFSET_T, color=T.LOGO_SHADOW_COLOR)
    save(img, "TitleLogo.png")


def main():
    build_menu_panel_odin()
    build_message_panel()
    build_character_slot_frame()
    build_input_field_frame()
    build_menu_section_divider()
    build_region_nameplate()
    build_hex_button("ButtonSecondary.png", T.BTN_SECONDARY_TOP, T.BTN_SECONDARY_BOTTOM)
    build_hex_button("ButtonPrimary.png", T.BTN_PRIMARY_TOP, T.BTN_PRIMARY_BOTTOM)
    build_hamburger_icon()
    build_level_badge()
    build_health_bar_frame()
    build_mana_fill()
    build_skill_button_frame()
    build_movement_stick()
    build_title_logo()
    print("done. PPU for every 9-sliced asset above =", PPU)


if __name__ == "__main__":
    main()
