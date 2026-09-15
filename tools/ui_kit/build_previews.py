#!/usr/bin/env python3
"""Composite the two mockups requested by the orchestrator:
  _preview_hud.png   - 1280x720 in-game HUD + open menu panel mockup
  _preview_title.png - title/login screen mockup

Placement values here are the AUTHOR's staging-layout choices for the
mockup only (not a guarantee of the exact Unity RectTransform numbers) -
see the final report for the recommended C# constant changes.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))

from PIL import Image, ImageDraw
import tokens as T
from shapes import font_kr

GAMES_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
KIT = os.path.join(GAMES_ROOT, "generated-images", "ui-kit")
DIAG = os.path.join(GAMES_ROOT, "generated-images", "diagnostics")
ART = os.path.join(GAMES_ROOT, "client", "Assets", "Sapphire", "Art", "UI")


def load(name):
    return Image.open(os.path.join(KIT, name)).convert("RGBA")


def paste(base, img, xy):
    base.alpha_composite(img, dest=(int(xy[0]), int(xy[1])))


def crop_menu_icon(sheet, rect_pil):
    x0, y0, x1, y1 = rect_pil
    return sheet.crop((x0, y0, x1, y1))


def build_hud_preview():
    # The full diagnostics screenshot has the OLD gold HUD baked into its
    # pixels (it's a live-game capture from before this task) - compositing
    # new translucent assets on top of it let the old gold bleed through at
    # every edge in the first pass. Fix: sample one verified-clean 130x130
    # grass patch (no UI in it, confirmed by eye) and tile it for the whole
    # background instead of using the raw screenshot.
    bg_src = Image.open(os.path.join(DIAG, "final3_1280_default.png")).convert("RGB")
    patch = bg_src.crop((700, 550, 830, 680))
    bg = Image.new("RGBA", (1280, 720), (30, 30, 30, 255))
    pw, ph = patch.size
    for ty in range(0, 720, ph):
        for tx in range(0, 1280, pw):
            bg.paste(patch, (tx, ty))

    draw = ImageDraw.Draw(bg)
    font = font_kr(20)
    font_sm = font_kr(16)
    font_xs = font_kr(12)

    # --- HP/MP bars + level hex badge, top-left ---
    hp_frame = load("HealthBarFrameGold.png")  # 780x120 native (track+gutter+fill), scale3 of 260x16(+gutter)
    track_native = hp_frame.crop((0, 0, 780, 48)).resize((260, 16), Image.LANCZOS)
    hp_fill_native = hp_frame.crop((0, 72, 780, 120)).resize((260, 16), Image.LANCZOS)
    mp_fill = load("GaugeFillMana.png").resize((260, 16), Image.LANCZOS)
    level_badge = load("LevelBadgeHex.png").resize((64, 64), Image.LANCZOS)

    bar_x = 56
    paste(bg, level_badge, (12, 16))
    paste(bg, track_native, (bar_x, 20))
    paste(bg, hp_fill_native, (bar_x, 20))
    paste(bg, track_native, (bar_x, 40))
    paste(bg, mp_fill, (bar_x, 40))
    draw.text((12 + 32, 16 + 32), "27", font=font_sm, fill=(255, 255, 255, 255), anchor="mm",
               stroke_width=2, stroke_fill=(20, 20, 20, 255))
    draw.text((bar_x + 4, 20 + 8), "185,728 / 245,105", font=font_sm, fill=(255, 255, 255, 255), anchor="lm",
               stroke_width=1, stroke_fill=(20, 20, 20, 255))

    # --- Region nameplate, top-center ---
    plate = load("RegionNameplate.png").resize((180, 32), Image.LANCZOS)
    plate_x = (1280 - plate.width) // 2
    paste(bg, plate, (plate_x, 14))
    draw.text((plate_x + 46, 14 + 16), "사파이어 광장", font=font_sm, fill=(240, 240, 245, 255), anchor="lm")

    # --- Hamburger menu icon, top-right (no background panel) ---
    hamburger = load("MenuHamburgerIcon.png").resize((80, 64), Image.LANCZOS)
    paste(bg, hamburger, (1280 - 20 - 80, 12))

    # --- Movement stick, bottom-left ---
    # 2026-09-15 orchestrator fix: knob crop was (520,144,712,336), centered
    # at x=616 - but the actual knob cell (base_d+gutter+knob_d/2)*SCALE =
    # (160+40+32)*3 = 696 is centered at x=696, so the old crop window was
    # shifted ~80px left and clipped the knob's right half off. Recomputed
    # from build_ui_kit.build_movement_stick's own geometry instead of a
    # guessed offset.
    stick = load("MovementStickGold.png")  # 792x480 native
    base_native = stick.crop((0, 0, 480, 480)).resize((160, 160), Image.LANCZOS)
    knob_native = stick.crop((600, 144, 792, 336)).resize((64, 64), Image.LANCZOS)
    paste(bg, base_native, (30, 720 - 30 - 160))
    paste(bg, knob_native, (30 + 48, 720 - 30 - 160 + 48))

    # --- Skill fan: 4 small rings + 1 big attack ring. Positioned bottom-
    # CENTER-right (not the far bottom-right corner) so the open menu panel
    # (x:[860,1260]) drawn afterward doesn't paint over it - in the real
    # game the fullscreen menu backdrop legitimately does cover the skill
    # fan while the menu is open (VillageHubMenuBuilder's MenuOverlay is
    # fullscreen), but this mockup wants to show every asset at once.
    skillframe = load("SkillButtonFrameGold.png")  # 756x396 native = (80+40+132)*3 x 132*3
    skill_ring = skillframe.crop((0, (396 - 240) // 2, 240, (396 - 240) // 2 + 240)).resize((80, 80), Image.LANCZOS)
    attack_ring = skillframe.crop((360, 0, 360 + 396, 396)).resize((132, 132), Image.LANCZOS)
    attack_cx, attack_cy = 760, 720 - 30 - 66
    paste(bg, attack_ring, (attack_cx - 66, attack_cy - 66))
    import math
    for i, ang_deg in enumerate([200, 160, 120, 80]):
        ang = math.radians(ang_deg)
        r = 118
        cx = attack_cx + r * math.cos(ang)
        cy = attack_cy - r * math.sin(ang)
        paste(bg, skill_ring, (int(cx - 40), int(cy - 40)))

    # --- Open menu panel, right side ---
    # 2026-09-15 orchestrator fix: show the REAL 4-column grid + MenuCatalog
    # section composition (VillageHubMenuBuilder.MenuCatalog) instead of a
    # made-up 2-per-section layout, with icon slots + labels + lock badges
    # like the actual game screen, not bare icons floating on parchment.
    icons_sheet = Image.open(os.path.join(ART, "MenuIconsSet.png")).convert("RGBA")
    icon_rects = {
        "equip": (0, 0, 447, 433), "bag": (447, 0, 868, 433),
        "quest": (868, 0, 1342, 433), "settings": (1342, 0, 1774, 433),
        "skillbook": (0, 433, 447, 887), "charinfo": (447, 433, 868, 887),
        "map": (868, 433, 1342, 887), "dungeon": (1342, 433, 1774, 887),
    }
    lock_sheet = Image.open(os.path.join(ART, "MenuLockBadge.png")).convert("RGBA")
    lock_icon = lock_sheet.crop((239, 50, 1038, 1178))

    sections = [
        ("성장", [("equip", "장비", False), ("bag", "가방", False), ("skillbook", "스킬북", True), ("charinfo", "캐릭터정보", True)]),
        ("모험", [("quest", "퀘스트", False), ("map", "지도", True), ("dungeon", "던전", True)]),
        ("시스템", [("settings", "설정", False)]),
    ]

    content_margin = 20
    panel_w = 400
    panel_x = 1280 - 20 - panel_w
    content_w = panel_w - 2 * content_margin
    cols = 4
    cell_w = content_w / cols
    slot_size = 44
    header_h = 30
    row_h = slot_size + 4 + 16 + 12  # slot + gap + label + section gap

    panel_y = 104
    panel_h = int(content_margin * 2 + len(sections) * (header_h + row_h) + 60)  # + footer button room
    panel_src = load("MenuPanelOdin.png")
    panel = panel_src.resize((panel_w, panel_h), Image.LANCZOS)  # preview-only uniform scale, not true 9-slice
    paste(bg, panel, (panel_x, panel_y))

    divider = load("MenuSectionDivider.png")  # 1200x60, two 600x60 halves
    left_half = divider.crop((0, 0, 600, 60))
    right_half = divider.crop((600, 0, 1200, 60))

    cursor_y = panel_y + content_margin
    for title, items in sections:
        div_h = 20
        text_w, _ = draw.textbbox((0, 0), title, font=font_sm)[2:]
        cx = panel_x + panel_w // 2
        div_w = int(content_w / 2 - text_w / 2 - 6)
        divl = left_half.resize((max(1, div_w), div_h), Image.LANCZOS)
        divr = right_half.resize((max(1, div_w), div_h), Image.LANCZOS)
        paste(bg, divl, (panel_x + content_margin, int(cursor_y)))
        paste(bg, divr, (cx + text_w // 2 + 6, int(cursor_y)))
        draw.text((cx, cursor_y + div_h / 2), title, font=font_sm, fill=T.PANEL_TITLE_TEXT, anchor="mm")
        cursor_y += header_h

        for col, (key, label, locked) in enumerate(items):
            slot_cx = panel_x + content_margin + col * cell_w + cell_w / 2
            slot_x0, slot_y0 = slot_cx - slot_size / 2, cursor_y
            draw.rounded_rectangle([slot_x0, slot_y0, slot_x0 + slot_size, slot_y0 + slot_size],
                                    radius=6, fill=(214, 202, 181, 255), outline=(188, 174, 151, 255), width=1)
            icon = crop_menu_icon(icons_sheet, icon_rects[key]).resize((32, 32), Image.LANCZOS)
            paste(bg, icon, (int(slot_cx - 16), int(slot_y0 + 6)))
            if locked:
                badge = lock_icon.resize((16, 16), Image.LANCZOS)
                paste(bg, badge, (int(slot_x0 + slot_size - 12), int(slot_y0 + slot_size - 12)))
            draw.text((slot_cx, slot_y0 + slot_size + 10), label, font=font_xs, fill=T.PANEL_TITLE_TEXT, anchor="mm")

        cursor_y += row_h

    # footer "캐릭터 선택으로" ButtonSecondary, full content width
    btn_sheet = load("ButtonSecondary.png")
    btn_normal = btn_sheet.crop((0, 0, 1032, 168)).resize((content_w, 40), Image.LANCZOS)
    btn_y = panel_y + panel_h - content_margin - 40
    paste(bg, btn_normal, (panel_x + content_margin, int(btn_y)))
    draw.text((panel_x + panel_w // 2, btn_y + 20), "캐릭터 선택으로", font=font_sm, fill=(255, 255, 255, 255), anchor="mm")

    out = os.path.join(KIT, "_preview_hud.png")
    bg.convert("RGB").save(out.replace(".png", "_flat.png")) if False else None
    bg.save(out)
    print("saved", out, bg.size)


def build_title_preview():
    # 2026-09-15 orchestrator fix: dropped the two empty beige side columns
    # (character-slot-frame demo cards) - they read as unexplained blank
    # panels/UI mistake in the mockup. This screen is LoginScene only per
    # the real game's scene split (CharacterFlowArtImportConfigurator) - so
    # now just logo -> single nickname field -> Primary start button,
    # nothing else competing for the center of the frame.
    bg = Image.open(os.path.join(ART, "Title", "TitleBackground.png")).convert("RGBA")
    bg = bg.resize((1280, int(1280 * bg.height / bg.width)), Image.LANCZOS)
    canvas = Image.new("RGBA", (1280, 720), (10, 10, 15, 255))
    paste(canvas, bg, (0, min(0, 720 - bg.height)))
    draw = ImageDraw.Draw(canvas)
    font_sm = font_kr(18)

    logo = load("TitleLogo.png")
    logo_w = 560
    logo = logo.resize((logo_w, int(logo_w * logo.height / logo.width)), Image.LANCZOS)
    paste(canvas, logo, ((1280 - logo_w) // 2, 60))

    input_frame = load("InputFieldFrame.png")  # 1080x180 native = target 360x60
    input_w, input_h = 320, 44
    input_img = input_frame.resize((input_w, input_h), Image.LANCZOS)
    ix = (1280 - input_w) // 2
    iy = 400
    paste(canvas, input_img, (ix, iy))
    draw.text((ix + 14, iy + input_h // 2), "닉네임 입력", font=font_sm, fill=(120, 108, 92, 255), anchor="lm")

    btn_sheet = load("ButtonPrimary.png")
    btn_normal = btn_sheet.crop((0, 0, 1032, 168)).resize((220, 48), Image.LANCZOS)
    bx = (1280 - 220) // 2
    by = iy + input_h + 24
    paste(canvas, btn_normal, (bx, by))
    draw.text((bx + 110, by + 24), "시작하기", font=font_sm, fill=(255, 255, 255, 255), anchor="mm")

    out = os.path.join(KIT, "_preview_title.png")
    canvas.save(out)
    print("saved", out, canvas.size)


if __name__ == "__main__":
    build_hud_preview()
    build_title_preview()
