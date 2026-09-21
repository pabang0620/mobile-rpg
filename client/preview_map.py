from PIL import Image, ImageDraw
import math, os

W, H = 32, 32

def perlin_noise(x, y):
    # Simple approximation of Mathf.PerlinNoise
    import hashlib
    def fade(t): return t * t * t * (t * (t * 6 - 15) + 10)
    def lerp(a, b, t): return a + t * (b - a)
    def grad(h, x, y):
        h = h & 3
        if h == 0: return x + y
        if h == 1: return -x + y
        if h == 2: return x - y
        return -x - y
    
    xi, yi = int(math.floor(x)) & 255, int(math.floor(y)) & 255
    xf, yf = x - math.floor(x), y - math.floor(y)
    u, v = fade(xf), fade(yf)
    
    seed = 42
    def p(n): return int(hashlib.md5(f"{n}_{seed}".encode()).hexdigest(), 16) & 255
    
    aa = p(p(xi) + yi)
    ab = p(p(xi) + yi + 1)
    ba = p(p(xi + 1) + yi)
    bb = p(p(xi + 1) + yi + 1)
    
    x1 = lerp(grad(aa, xf, yf), grad(ba, xf - 1, yf), u)
    x2 = lerp(grad(ab, xf, yf - 1), grad(bb, xf - 1, yf - 1), u)
    return (lerp(x1, x2, v) + 1) / 2

def is_forest(x, y):
    border = min(x, y, W-1-x, H-1-y)
    n1 = perlin_noise(x*0.25+0.5, y*0.25+0.5) * 3.5
    if border + n1 < 4.5: return True
    dxNE, dyNE = x-26, y-26
    n2 = perlin_noise(x*0.5+10, y*0.5+10) * 2
    if dxNE*dxNE + dyNE*dyNE < 12 + n2: return True
    dxSE, dySE = x-25, y-7
    if dxSE*dxSE + dySE*dySE < 6 + n2: return True
    return False

def is_path(x, y):
    if is_forest(x, y): return False
    dx, dy = x-16, y-16
    if (dx*dx)/25 + (dy*dy)/16 < 1: return True
    wS = math.sin(y*0.4) * 1.2
    if abs(x-16+wS) < 1.8 and 3 <= y <= 12: return True
    wN = math.sin(y*0.35+1) * 1.0
    if abs(x-16+wN) < 1.6 and 20 <= y <= 29: return True
    wE = math.sin(x*0.35) * 1.0
    if abs(y-16+wE) < 1.5 and 20 <= x <= 28: return True
    wW = math.sin(x*0.3+2) * 1.0
    if abs(y-14+wW) < 1.5 and 4 <= x <= 12: return True
    wavNW = math.sin(x*0.5) * 0.8
    if abs(y-22+wavNW) < 1.2 and 6 <= x <= 13: return True
    if 7 <= x <= 10 and 12 <= y <= 16: return True
    return False

def is_liquid(x, y):
    if is_forest(x, y): return False
    ldx, ldy = x-5, y-18
    ln = perlin_noise(x*0.3+5, y*0.3+5) * 3
    lake = ldx*ldx/18 + ldy*ldy/30 < 1 + ln*0.3
    sw = math.sin(y*0.5+1) * 1.5
    stream = abs(x-7+sw) < 1.2 and 4 <= y < 14
    pdx, pdy = x-22, y-9
    pn = perlin_noise(x*0.4+20, y*0.4+20) * 1.5
    pond = pdx*pdx/8 + pdy*pdy/5 < 1 + pn*0.2
    water = lake or stream or pond
    if 6 <= x <= 9 and 13 <= y <= 15: water = False
    return water

# Colors
GRASS = (76, 175, 80)
DIRT = (161, 136, 99)
WATER = (66, 165, 245)
FOREST = (27, 94, 32)
SHORE = (100, 200, 230)

TILE = 20
img = Image.new('RGB', (W*TILE, H*TILE))
draw = ImageDraw.Draw(img)

for x in range(W):
    for y in range(H):
        py = H - 1 - y  # flip Y for display
        if is_forest(x, y):
            c = FOREST
        elif is_liquid(x, y):
            # check shore
            adj_land = False
            for dx2, dy2 in [(0,1),(0,-1),(1,0),(-1,0)]:
                nx, ny = x+dx2, y+dy2
                if 0<=nx<W and 0<=ny<H and not is_liquid(nx,ny) and not is_forest(nx,ny):
                    adj_land = True
            c = SHORE if adj_land else WATER
        elif is_path(x, y):
            c = DIRT
        else:
            c = GRASS
        draw.rectangle([x*TILE, py*TILE, (x+1)*TILE-1, (py+1)*TILE-1], fill=c)

out = r'C:\Users\darac\.gemini\antigravity\brain\30c79ebb-9e0c-4432-9d21-cdbfd419c0cb\village_layout_preview.png'
img.save(out)
print(f'Saved to {out}')
