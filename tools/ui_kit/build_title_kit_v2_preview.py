#!/usr/bin/env python3
"""Composite _preview_select.png - a CharacterSelect mockup using the new
title-kit-v2 assets (build_title_kit_v2.py) over the existing
TitleBackground.png + PortraitMage/PortraitWarrior.png, for the
orchestrator's own visual sanity check (per spec: "네가 직접 열어보고
대칭/발광 품질 확인"). Placement values here are mockup staging choices
only, not a guarantee of exact Unity RectTransform numbers - the handoff
report's relative proportions are the actual spec.

Output: generated-images/title-kit-v2/_preview_select.png only.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))

from PIL import Image, ImageDraw
from shapes import font_kr

GAMES_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
KIT = os.path.join(GAMES_ROOT, "generated-images", "title-kit-v2")
TITLE_ART = os.path.join(GAMES_ROOT, "client", "Assets", "Sapphire", "Art", "UI", "Title")


def load_kit(name):
    return Image.open(os.path.join(KIT, name)).convert("RGBA")


def load_title(name):
    return Image.open(os.path.join(TITLE_ART, name)).convert("RGBA")


def paste(base, img, xy):
    base.alpha_composite(img, dest=(int(xy[0]), int(xy[1])))


def build():
    W, H = 1280, 720
    bg_src = Image.open(os.path.join(TITLE_ART, "TitleBackground.png")).convert("RGB")
    bg_ratio = bg_src.width / bg_src.height
    target_ratio = W / H
    if bg_ratio > target_ratio:
        new_h = H
        new_w = int(H * bg_ratio)
    else:
        new_w = W
        new_h = int(W / bg_ratio)
    bg_resized = bg_src.resize((new_w, new_h), Image.LANCZOS)
    left = (new_w - W) // 2
    top = (new_h - H) // 2
    bg = bg_resized.crop((left, top, left + W, top + H)).convert("RGBA")
    # darken slightly under the card row so the new dark-glass frames read
    # clearly against a bright sky background
    overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(overlay).rectangle([0, 300, W, H], fill=(5, 8, 14, 110))
    bg.alpha_composite(overlay)

    draw = ImageDraw.Draw(bg)
    title_font = font_kr(34)
    draw.text((W / 2, 60), "캐릭터 선택", font=title_font, fill=(240, 244, 255, 255), anchor="mm")

    # 4 slots: Mage(filled), Warrior(filled), Empty, Empty
    disp_w = 230
    frame_native = load_kit("CharacterSlotFrameV2.png")
    disp_h = int(frame_native.height * (disp_w / frame_native.width))
    gap = 26
    row_w = disp_w * 4 + gap * 3
    start_x = (W - row_w) / 2
    row_top = 150

    pedestal = load_kit("CharacterPedestal.png")
    badge_mage = load_kit("ClassBadgeMage.png")
    badge_warrior = load_kit("ClassBadgeWarrior.png")
    nameplate = load_kit("NameplateBar.png")
    btn_select = load_kit("ButtonSelectV2.png")
    btn_delete = load_kit("ButtonDeleteV2.png")
    btn_create = load_kit("ButtonCreateV2.png")
    frame_empty_native = load_kit("CharacterSlotFrameEmptyV2.png")

    def resized(img, w):
        h = int(img.height * (w / img.width))
        return img.resize((w, h), Image.LANCZOS)

    frame_disp = resized(frame_native, disp_w)
    frame_empty_disp = resized(frame_empty_native, disp_w)

    # badge notch center in FRAME target space was (FRAME_W/2, BADGE_NOTCH_CY+FRAME_MARGIN) = (130, 38)
    # over FRAME_W,FRAME_H = 260,400 -> ratio (0.5, 0.095)
    badge_ratio_x, badge_ratio_y = 0.5, 0.095
    badge_disp_w = int(disp_w * 0.30)
    badge_disp = badge_disp_w

    # divider at FRAME_H - NAMEPLATE_BAND_H = 400-76=324 -> ratio 0.81
    divider_ratio_y = 324 / 400
    portrait_area_top_ratio = 0.16
    portrait_area_bottom_ratio = divider_ratio_y - 0.02

    def portrait_slot(cx, filled_img):
        # pedestal sits just above the divider line, character stands on it
        ped_w = int(disp_w * 0.72)
        ped_disp = resized(pedestal, ped_w)
        ped_x = cx - ped_w / 2
        ped_y = row_top + disp_h * (portrait_area_bottom_ratio) - ped_disp.height * 0.62
        paste(bg, ped_disp, (ped_x, ped_y))

        if filled_img is not None:
            area_top = row_top + disp_h * portrait_area_top_ratio
            area_bottom = row_top + disp_h * portrait_area_bottom_ratio
            area_h = area_bottom - area_top
            area_w = disp_w * 0.86
            port_ratio = filled_img.width / filled_img.height
            port_h = area_h
            port_w = port_h * port_ratio
            if port_w > area_w:
                port_w = area_w
                port_h = port_w / port_ratio
            port_disp = filled_img.resize((int(port_w), int(port_h)), Image.LANCZOS)
            px = cx - port_disp.width / 2
            py = area_bottom - port_disp.height
            paste(bg, port_disp, (px, py))

    for i in range(4):
        cx = start_x + disp_w / 2 + i * (disp_w + gap)
        x = start_x + i * (disp_w + gap)

        if i < 2:
            paste(bg, frame_disp, (x, row_top))
            portrait = load_title("PortraitMage.png" if i == 0 else "PortraitWarrior.png")
            portrait_slot(cx, portrait)

            badge = resized(badge_mage if i == 0 else badge_warrior, badge_disp)
            bx = x + disp_w * badge_ratio_x - badge.width / 2
            by = row_top + disp_h * badge_ratio_y - badge.height / 2
            paste(bg, badge, (bx, by))

            np_w = int(disp_w * 0.78)
            np_disp = resized(nameplate, np_w)
            np_x = cx - np_disp.width / 2
            np_y = row_top + disp_h * (divider_ratio_y + 0.02)
            paste(bg, np_disp, (np_x, np_y))
            name_font = font_kr(15)
            draw.text((cx, np_y + np_disp.height * 0.42), "법사" if i == 0 else "전사",
                      font=name_font, fill=(235, 240, 250, 255), anchor="mm")
            lvl_font = font_kr(11)
            draw.text((cx, np_y + np_disp.height * 0.42 + 17), "Lv. 12" if i == 0 else "Lv. 9",
                      font=lvl_font, fill=(210, 220, 235, 220), anchor="mm")

            btn_row_y = np_y + np_disp.height + 10
            sel_cell = btn_select.crop((0, 0, 96, 40))
            del_cell = btn_delete.crop((0, 0, 96, 40))
            btn_w = int(disp_w * 0.36)
            sel_disp = resized(sel_cell, btn_w)
            del_disp = resized(del_cell, btn_w)
            btn_gap = 8
            total_w = sel_disp.width + del_disp.width + btn_gap
            bx0 = cx - total_w / 2
            paste(bg, sel_disp, (bx0, btn_row_y))
            paste(bg, del_disp, (bx0 + sel_disp.width + btn_gap, btn_row_y))
            btn_font = font_kr(13)
            draw.text((bx0 + sel_disp.width / 2, btn_row_y + sel_disp.height / 2), "선택",
                      font=btn_font, fill=(255, 255, 255, 255), anchor="mm")
            draw.text((bx0 + sel_disp.width + btn_gap + del_disp.width / 2, btn_row_y + del_disp.height / 2), "삭제",
                      font=btn_font, fill=(230, 225, 225, 255), anchor="mm")
        else:
            paste(bg, frame_empty_disp, (x, row_top))
            create_cell = btn_create.crop((0, 0, 160, 56))
            cbtn_w = int(disp_w * 0.6)
            cbtn_disp = resized(create_cell, cbtn_w)
            cby = row_top + disp_h * 0.62
            cbx = cx - cbtn_disp.width / 2
            paste(bg, cbtn_disp, (cbx, cby))
            btn_font2 = font_kr(15)
            draw.text((cx, cby + cbtn_disp.height / 2), "+ 생성", font=btn_font2, fill=(40, 32, 16, 255), anchor="mm")

    out_path = os.path.join(KIT, "_preview_select.png")
    bg.save(out_path)
    print("saved", out_path, bg.size, bg.mode)


if __name__ == "__main__":
    build()
