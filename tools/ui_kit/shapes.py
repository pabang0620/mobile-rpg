"""Supersampled drawing primitives shared by every asset in build_ui_kit.py.

Every function takes/returns plain PIL Images in RGBA. The SuperCanvas class
draws at tokens.SS_FACTOR x the final size and downsamples once with LANCZOS
- this is what keeps line widths and left/right symmetry exact (the root
cause diagnosis for why the old gpt-image frames looked hand-wavy/AI-made).
"""
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import math

from tokens import SS_FACTOR, FONT_KR_BOLD, FONT_INDEX_KR


class SuperCanvas:
    """Unit convention (2026-09-15 bugfix - see build log for the incident
    this replaced): every coordinate/length a caller hands to a shapes.py
    drawing function - box corners, centers, radii, line widths, ALL of
    them, never just some - is in TARGET/design-token units (the same units
    tokens.py's numbers are written in, e.g. "radius=4" really means 4
    design px). `scale` (typically build_ui_kit.SCALE) is this canvas's
    target-to-native multiplier; self.w/self.h (the file's final saved size)
    = target_w/target_h * scale. self.img/self.draw are a further
    SS_FACTOR-times-bigger supersample surface for antialiasing - .s()/
    .sbox()/.spt() apply BOTH multipliers (scale, then SS_FACTOR) in one
    step so calling code never has to juggle two unit systems itself.

    (The first build pass mixed "already native px" and "still target px"
    values across different call sites and only scaled some of each box's
    coordinates - every shape rendered ~4x undersized, confined near the
    canvas origin. Funnelling every draw call through this single explicit
    target-space contract is the fix.)
    """

    def __init__(self, target_w, target_h, scale=1):
        self.scale = scale
        self.w = round(target_w * scale)
        self.h = round(target_h * scale)
        self.sw, self.sh = self.w * SS_FACTOR, self.h * SS_FACTOR
        self.img = Image.new("RGBA", (self.sw, self.sh), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.img)

    def s(self, v):
        """Scale a single TARGET-space length/coord into supersampled space."""
        return v * self.scale * SS_FACTOR

    def sbox(self, box):
        return tuple(v * self.scale * SS_FACTOR for v in box)

    def spt(self, pt):
        return (pt[0] * self.scale * SS_FACTOR, pt[1] * self.scale * SS_FACTOR)

    def finish(self):
        return self.img.resize((self.w, self.h), Image.LANCZOS)


def font_kr(size_target):
    """size_target is in FINAL (downsampled) px; caller must pre-multiply by
    SS_FACTOR before calling this if drawing on a SuperCanvas."""
    return ImageFont.truetype(FONT_KR_BOLD, size_target, index=FONT_INDEX_KR)


def vertical_gradient(size, top_color, bottom_color):
    """Fast vertical-gradient RGBA image of `size` - builds a 1px-wide
    column (cheap Python loop over height only) and stretches it across the
    full width with a resize, instead of a per-pixel double loop. Needed for
    the title logo's big supersampled canvas (~25M px at full SS_FACTOR) -
    a naive double loop there would be far too slow."""
    w, h = size
    col = Image.new("RGBA", (1, h))
    for y in range(h):
        t = y / max(1, h - 1)
        col.putpixel((0, y), tuple(int(top_color[i] + (bottom_color[i] - top_color[i]) * t) for i in range(4)))
    return col.resize((w, h), Image.NEAREST)


def vgrad_rounded_rect(size, radius, top_color, bottom_color):
    """Return an RGBA image of `size` with a rounded rect filled by a
    vertical linear gradient from top_color to bottom_color (both RGBA)."""
    w, h = size
    grad = Image.new("RGBA", (w, h))
    top = top_color[:3]
    bot = bottom_color[:3]
    a_top = top_color[3]
    a_bot = bottom_color[3]
    px = grad.load()
    for y in range(h):
        t = y / max(1, h - 1)
        r = int(top[0] + (bot[0] - top[0]) * t)
        g = int(top[1] + (bot[1] - top[1]) * t)
        b = int(top[2] + (bot[2] - top[2]) * t)
        a = int(a_top + (a_bot - a_top) * t)
        for x in range(w):
            px[x, y] = (r, g, b, a)
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, w - 1, h - 1], radius=radius, fill=255)
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.paste(grad, (0, 0), mask)
    return out


def draw_beige_panel(canvas: SuperCanvas, box, radius_t, outer_line, outer_w_t,
                      inner_line, inner_w_t, inner_inset_t, top_color, bottom_color):
    """MapleStory-grammar parchment panel: vertical-gradient beige fill,
    1.5px dark outer stroke, 1px lighter inner stroke inset 3px in. `box` is
    in FINAL/native px (same space as canvas.w/h); every other *_t arg is a
    length in that same space - this function scales all of it to
    supersample space itself so callers never touch canvas.s directly."""
    x0, y0, x1, y1 = canvas.sbox(box)
    w = x1 - x0
    h = y1 - y0
    radius = canvas.s(radius_t)
    fill_img = vgrad_rounded_rect((int(w), int(h)), radius, top_color, bottom_color)
    canvas.img.paste(fill_img, (int(x0), int(y0)), fill_img)
    canvas.draw.rounded_rectangle(
        [x0, y0, x1 - 1, y1 - 1], radius=radius, outline=outer_line, width=max(1, round(canvas.s(outer_w_t))))
    inset = canvas.s(inner_inset_t)
    canvas.draw.rounded_rectangle(
        [x0 + inset, y0 + inset, x1 - 1 - inset, y1 - 1 - inset],
        radius=max(1, radius - inset), outline=inner_line, width=max(1, round(canvas.s(inner_w_t))))


def draw_dark_capsule(canvas: SuperCanvas, box, fill, line, line_w_t):
    """`box` is FINAL/native px."""
    x0, y0, x1, y1 = canvas.sbox(box)
    h = y1 - y0
    radius = h / 2
    canvas.draw.rounded_rectangle([x0, y0, x1, y1], radius=radius, fill=fill)
    if line_w_t > 0:
        canvas.draw.rounded_rectangle(
            [x0, y0, x1 - 1, y1 - 1], radius=radius, outline=line, width=max(1, round(canvas.s(line_w_t))))


def draw_pin_icon(canvas: SuperCanvas, center, size_t, color):
    """Small teardrop map-pin marker. `center` and `size_t` are FINAL px."""
    cx, cy = canvas.spt(center)
    r = canvas.s(size_t) * 0.30
    top_cy = cy - canvas.s(size_t) * 0.15
    canvas.draw.ellipse([cx - r, top_cy - r, cx + r, top_cy + r], fill=color)
    tail_len = canvas.s(size_t) * 0.55
    canvas.draw.polygon([
        (cx - r * 0.65, top_cy + r * 0.55),
        (cx + r * 0.65, top_cy + r * 0.55),
        (cx, top_cy + tail_len),
    ], fill=color)
    hole_r = r * 0.34
    canvas.draw.ellipse([cx - hole_r, top_cy - hole_r, cx + hole_r, top_cy + hole_r], fill=(0, 0, 0, 90))


def _octagon_points_ss(box_ss, cut_ss):
    """box_ss/cut_ss already in SUPERSAMPLE space - internal helper so the
    two public entry points (draw_hex_cut_button's outer + inner rings)
    share one implementation instead of re-deriving the 8 corner points."""
    x0, y0, x1, y1 = box_ss
    c = min(cut_ss, (x1 - x0) / 2 - 1, (y1 - y0) / 2 - 1)
    return [
        (x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c),
        (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c),
    ], c


def draw_hex_cut_button(canvas: SuperCanvas, box, cut_t, top_color, bottom_color,
                         outer_line, outer_w_t, inner_line_rgba, inner_w_t, chevron_color):
    """Hex-cut (octagon-corner) pill button used for Secondary/Primary.
    `box`/`cut_t` are FINAL/native px."""
    box_ss = canvas.sbox(box)
    cut_ss = canvas.s(cut_t)
    pts, c_ss = _octagon_points_ss(box_ss, cut_ss)
    x0, y0, x1, y1 = box_ss
    w, h = int(x1 - x0), int(y1 - y0)
    grad = Image.new("RGBA", (w, h))
    px = grad.load()
    for y in range(h):
        t = y / max(1, h - 1)
        r = int(top_color[0] + (bottom_color[0] - top_color[0]) * t)
        g = int(top_color[1] + (bottom_color[1] - top_color[1]) * t)
        b = int(top_color[2] + (bottom_color[2] - top_color[2]) * t)
        for x in range(w):
            px[x, y] = (r, g, b, 255)
    mask = Image.new("L", (w, h), 0)
    local_pts = [(px_ - x0, py_ - y0) for px_, py_ in pts]
    ImageDraw.Draw(mask).polygon(local_pts, fill=255)
    canvas.img.paste(grad, (int(x0), int(y0)), mask)

    canvas.draw.polygon(pts, outline=outer_line, width=max(1, round(canvas.s(outer_w_t))))
    inset_ss = canvas.s(2.0)
    inner_pts, _ = _octagon_points_ss(
        (x0 + inset_ss, y0 + inset_ss, x1 - inset_ss, y1 - inset_ss), max(canvas.s(0.5), cut_ss - inset_ss))
    canvas.draw.polygon(inner_pts, outline=inner_line_rgba, width=max(1, round(canvas.s(inner_w_t))))

    # small chevron decoration just inside each cut end
    chev_w = max(1, round(canvas.s(1.2)))
    for side in (-1, 1):
        cx = (x0 + c_ss * 0.75) if side < 0 else (x1 - c_ss * 0.75)
        cy_mid = (y0 + y1) / 2
        arm = (y1 - y0) * 0.16
        canvas.draw.line([(cx - side * arm * 0.5, cy_mid - arm), (cx, cy_mid), (cx - side * arm * 0.5, cy_mid + arm)],
                          fill=chevron_color, width=chev_w, joint="curve")


def draw_ring_disc(canvas: SuperCanvas, center, radius_t, disc_fill, ring_color, ring_w_t):
    """`center`/`radius_t` are FINAL/native px."""
    cx, cy = canvas.spt(center)
    r = canvas.s(radius_t)
    if disc_fill is not None:
        canvas.draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=disc_fill)
    if ring_w_t > 0:
        w = max(1, round(canvas.s(ring_w_t)))
        canvas.draw.ellipse([cx - r + w / 2, cy - r + w / 2, cx + r - w / 2, cy + r - w / 2], outline=ring_color, width=w)


def draw_gradient_disc(canvas: SuperCanvas, center, radius_t, top_color, bottom_color, ring_color, ring_w_t):
    """`center`/`radius_t` are FINAL/native px."""
    cx, cy = canvas.spt(center)
    r = int(canvas.s(radius_t))
    d = r * 2
    grad = Image.new("RGBA", (d, d))
    px = grad.load()
    for y in range(d):
        t = y / max(1, d - 1)
        col = tuple(int(top_color[i] + (bottom_color[i] - top_color[i]) * t) for i in range(3)) + (255,)
        for x in range(d):
            px[x, y] = col
    mask = Image.new("L", (d, d), 0)
    ImageDraw.Draw(mask).ellipse([0, 0, d - 1, d - 1], fill=255)
    canvas.img.paste(grad, (int(cx - r), int(cy - r)), mask)
    if ring_w_t > 0:
        w = max(1, round(canvas.s(ring_w_t)))
        canvas.draw.ellipse([cx - r + w / 2, cy - r + w / 2, cx + r - w / 2, cy + r - w / 2], outline=ring_color, width=w)


def draw_hexagon_badge(canvas: SuperCanvas, center, radius_t, fill, line, line_w_t):
    """`center`/`radius_t` are FINAL/native px."""
    cx, cy = canvas.spt(center)
    r = canvas.s(radius_t)
    pts = []
    for i in range(6):
        ang = math.pi / 6 + i * math.pi / 3  # flat-top hexagon
        pts.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    canvas.draw.polygon(pts, fill=fill)
    if line_w_t > 0:
        canvas.draw.polygon(pts, outline=line, width=max(1, round(canvas.s(line_w_t))))


def draw_fading_line_h(canvas: SuperCanvas, y_t, x0_t, x1_t, thickness_t, color_rgba, fade_from, fade_len_t):
    """Horizontal 1px-ish line, alpha ramps from 0 to color_rgba's alpha over
    fade_len_t starting at fade_from ('left' or 'right') and staying solid on
    the other side - used for the divider halves."""
    y = canvas.s(y_t)
    x0, x1 = canvas.s(x0_t), canvas.s(x1_t)
    th = max(1, round(canvas.s(thickness_t)))
    steps = max(1, int(x1 - x0))
    fade_len = canvas.s(fade_len_t)
    base_a = color_rgba[3]
    for i in range(steps):
        x = x0 + i
        if fade_from == "left":
            dist = x - x0
        else:
            dist = x1 - x
        a = base_a if dist >= fade_len else int(base_a * (dist / max(1, fade_len)))
        if a <= 0:
            continue
        canvas.draw.line([(x, y - th / 2), (x, y + th / 2)], fill=(color_rgba[0], color_rgba[1], color_rgba[2], a))


def draw_chevron(canvas: SuperCanvas, center, size_t, color, width_t, direction="down"):
    """`center`/`size_t` are FINAL/native px."""
    cx, cy = canvas.spt(center)
    half = canvas.s(size_t) / 2
    w = max(1, round(canvas.s(width_t)))
    if direction == "down":
        pts = [(cx - half, cy - half * 0.4), (cx, cy + half * 0.5), (cx + half, cy - half * 0.4)]
    else:
        pts = [(cx - half, cy + half * 0.4), (cx, cy - half * 0.5), (cx + half, cy + half * 0.4)]
    canvas.draw.line(pts, fill=color, width=w, joint="curve")


def apply_soft_shadow(base_rgba: Image.Image, blur, offset, color):
    """Return a new image = base composited over a blurred colored shadow of
    its own alpha, offset by `offset` (target-space, caller must pre-scale)."""
    w, h = base_rgba.size
    alpha = base_rgba.split()[3]
    shadow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    shadow_layer = Image.new("RGBA", (w, h), color)
    shadow.paste(shadow_layer, offset, alpha)
    shadow = shadow.filter(ImageFilter.GaussianBlur(blur))
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out = Image.alpha_composite(out, shadow)
    out = Image.alpha_composite(out, base_rgba)
    return out
