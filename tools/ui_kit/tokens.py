"""Style tokens for Sapphire UI kit v3 ("MapleStory-grammar" pass, 2026-09-15).

SSOT for every color / stroke-width / radius used by build_ui_kit.py and
build_previews.py. Do not hardcode a color/number in either of those files -
add or change it here so every asset stays consistent (per the orchestrator's
"one file owns all tokens" instruction).

Reference (moodboard only, not copied 1:1):
  generated-images/reference/maplestory/maplem_hud_popup.png
  generated-images/reference/maplestory/maple_pc_windows.png

Supersampling: every draw function in shapes.py renders at SS_FACTOR x the
final pixel size, then LANCZOS-downsamples once at the end. This is what
keeps line widths/symmetry exact across every cell of every sheet (the
project's own diagnosis of why the old gpt-image frames looked "AI-made":
uneven line weight + asymmetry).
"""
import os

SS_FACTOR = 4

# ---------------------------------------------------------------------------
# Beige parchment panel (menu / message / character-select / input-field
# frames) - MapleStory reference item 4.
# ---------------------------------------------------------------------------
PANEL_TOP = (233, 224, 208, 255)      # #e9e0d0
PANEL_BOTTOM = (221, 210, 191, 255)   # #ddd2bf
PANEL_OUTER_LINE = (91, 81, 71, 255)  # #5b5147
PANEL_OUTER_LINE_W = 1.5
PANEL_INNER_LINE = (185, 171, 149, 255)  # #b9ab95
PANEL_INNER_LINE_W = 1.0
PANEL_INNER_INSET = 3.0
PANEL_CORNER_RADIUS = 4.0
PANEL_TITLE_TEXT = (74, 64, 56, 255)  # #4a4038
PANEL_ALPHA_MULT = 1.0  # panel itself is opaque parchment, no translucency

ICON_SLOT_FILL = (211, 198, 176, 255)   # #d3c6b0
ICON_SLOT_LINE = (185, 171, 149, 255)   # #b9ab95

# Input field inner fill (item 7 override - flatter/lighter than the general
# panel gradient, own thin border instead of the double outer+inner lines).
INPUT_FILL = (245, 239, 228, 255)   # #f5efe4
INPUT_LINE = (143, 130, 114, 255)   # #8f8272
INPUT_LINE_W = 1.0

# ---------------------------------------------------------------------------
# Divider (section headers inside beige panels + panel title underline).
# 2026-09-15 orchestrator fix: the original chevron ("V") marks read as
# stray letters next to the section title ("V 성장 V") - replaced with a
# plain line + a single small dot at the inner (text-side) end, 12px clear
# gap between the line's inner end and the text itself.
# ---------------------------------------------------------------------------
DIVIDER_LINE = (138, 124, 104, 200)   # muted brown-beige, alpha baked in
DIVIDER_DOT = (74, 64, 56, 230)       # #4a4038, matches panel title text
DIVIDER_LINE_W = 1.0
DIVIDER_TEXT_GAP = 12.0   # target px between the line's inner end and the text
DIVIDER_DOT_DIAMETER = 3.0

# ---------------------------------------------------------------------------
# Region nameplate (item 2) - dark translucent capsule, NOT beige.
# ---------------------------------------------------------------------------
REGION_FILL = (16, 19, 24, int(255 * 0.60))   # #101318 alpha .60
REGION_LINE = (255, 255, 255, int(255 * 0.50))
REGION_LINE_W = 1.0
REGION_PIN_COLOR = (255, 255, 255, int(255 * 0.85))

# ---------------------------------------------------------------------------
# HUD bars (item 1) - HP/MP, no ornate frame.
# ---------------------------------------------------------------------------
BAR_TRACK_FILL = (20, 22, 28, int(255 * 0.55))
BAR_TRACK_LINE = (255, 255, 255, int(255 * 0.12))
HP_FILL_COLOR = (224, 72, 72, 255)    # #e04848
MP_FILL_COLOR = (63, 127, 224, 255)   # #3f7fe0
BAR_FILL_HIGHLIGHT = (255, 255, 255, int(255 * 0.35))  # thin top highlight

LEVEL_BADGE_FILL = (28, 31, 39, int(255 * 0.75))   # #1c1f27 alpha .75
LEVEL_BADGE_LINE = (200, 200, 200, int(255 * 0.60))  # #c8c8c8 alpha .60
LEVEL_BADGE_LINE_W = 1.5

# ---------------------------------------------------------------------------
# Skill / basic-attack radial buttons + movement stick (item 3).
# ---------------------------------------------------------------------------
SKILL_DISC_FILL = (26, 29, 36, int(255 * 0.55))   # #1a1d24 alpha .55
SKILL_RING_COLOR = (200, 200, 200, int(255 * 0.55))
SKILL_RING_W = 2.0

ATTACK_DISC_TOP = (138, 106, 85, 255)   # #8a6a55
ATTACK_DISC_BOTTOM = (109, 80, 62, 255)  # slightly darker edge
ATTACK_RING_COLOR = (235, 219, 196, int(255 * 0.75))  # bright beige
ATTACK_RING_W = 1.5

STICK_RING_COLOR = (255, 255, 255, int(255 * 0.25))
STICK_RING_W = 2.0
STICK_BASE_FILL = (255, 255, 255, int(255 * 0.06))  # "거의 없음"
STICK_KNOB_FILL = (255, 255, 255, int(255 * 0.35))
STICK_KNOB_LINE = (255, 255, 255, int(255 * 0.45))

# ---------------------------------------------------------------------------
# Buttons (item 5) - hex-cut (octagon-corner) pill, Secondary + Primary.
# ---------------------------------------------------------------------------
BTN_CUT_RATIO = 0.30  # corner cut size as a fraction of button height

BTN_SECONDARY_TOP = (93, 97, 112, 255)     # #5d6170
BTN_SECONDARY_BOTTOM = (74, 78, 91, 255)   # #4a4e5b
BTN_PRIMARY_TOP = (154, 110, 82, 255)      # #9a6e52
BTN_PRIMARY_BOTTOM = (125, 87, 63, 255)    # #7d573f

BTN_OUTER_LINE = (34, 30, 26, 255)         # dark outer stroke
BTN_OUTER_LINE_W = 1.5
BTN_INNER_LINE_ALPHA = 90                  # bright inner line, low alpha over color
BTN_INNER_LINE_W = 1.0
BTN_CHEVRON_COLOR = (255, 255, 255, 130)
BTN_TEXT_COLOR = (255, 255, 255, 255)      # Unity Text overlay, for reference only

PRESSED_DARKEN = 0.82  # multiply RGB by this for pressed state

# ---------------------------------------------------------------------------
# Top-right menu button (item 6) - background-less line icon.
# ---------------------------------------------------------------------------
HAMBURGER_LINE_COLOR = (255, 255, 255, 235)
HAMBURGER_LINE_W = 6.0  # native px at the chosen canvas (icon is small + flat)
HAMBURGER_SHADOW_COLOR = (0, 0, 0, 120)

# ---------------------------------------------------------------------------
# Title logo (item 8). 2026-09-15 orchestrator fix: plain Noto Sans Bold read
# as "default gothic", not a game logo - switched to a bold rounded DISPLAY
# face (Baloo 2 ExtraBold, variable font pinned to wght=800) for the Latin
# wordmark + Jua for the Korean subtitle, both OFL (see fonts/*_OFL.txt).
# Gems/metal explicitly excluded per spec.
# ---------------------------------------------------------------------------
LOGO_TEXT_TOP = (255, 255, 255, 255)       # #ffffff
LOGO_TEXT_BOTTOM = (207, 227, 255, 255)    # #cfe3ff
LOGO_OUTLINE_COLOR = (27, 42, 74, 255)     # #1b2a4a dark navy
LOGO_OUTLINE_W = 6.0    # target px, inner dark outline
LOGO_OUTER_LINE_COLOR = (255, 255, 255, 90)  # white, alpha .35
LOGO_OUTER_LINE_W = 2.0  # target px, outside the dark outline
LOGO_SHADOW_COLOR = (0, 0, 0, 140)
LOGO_SHADOW_OFFSET_T = (0, 4)  # target px, scaled by build script
LOGO_SHADOW_BLUR = 10

LOGO_RIBBON_FILL = (27, 42, 74, int(255 * 0.85))  # #1b2a4a alpha .85
LOGO_RIBBON_TEXT = (255, 255, 255, 255)

FONT_KR_BOLD = "/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc"
FONT_INDEX_KR = 1  # verified via PIL ImageFont.truetype(..., index=1).getname() -> ('Noto Sans CJK KR', 'Bold')

FONT_LOGO_LATIN = os.path.join(os.path.dirname(__file__), "fonts", "Baloo2-Variable.ttf")
FONT_LOGO_LATIN_WGHT = 800  # ExtraBold axis value (font's own max, see build log)
FONT_LOGO_KR = os.path.join(os.path.dirname(__file__), "fonts", "Jua-Regular.ttf")
