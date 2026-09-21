from PIL import Image, ImageDraw, ImageFilter
import math, os, random

W, H = 32, 32
random.seed(42)

# Better Perlin noise approximation
def noise2d(x, y, seed=0):
    import hashlib
    def fade(t): return t*t*t*(t*(t*6-15)+10)
    def lerp(a,b,t): return a+t*(b-a)
    def grad(h,x,y):
        h=h&3
        u = x if h<2 else y
        v = y if h<2 else x
        return (u if h&1==0 else -u)+(v if h&2==0 else -v)
    xi,yi=int(math.floor(x))&255,int(math.floor(y))&255
    xf,yf=x-math.floor(x),y-math.floor(y)
    u,v=fade(xf),fade(yf)
    def p(n):
        return int(hashlib.md5(f"{n}_{seed}".encode()).hexdigest(),16)&255
    aa=p(p(xi)+yi); ab=p(p(xi)+yi+1)
    ba=p(p(xi+1)+yi); bb=p(p(xi+1)+yi+1)
    x1=lerp(grad(aa,xf,yf),grad(ba,xf-1,yf),u)
    x2=lerp(grad(ab,xf,yf-1),grad(bb,xf-1,yf-1),u)
    return (lerp(x1,x2,v)+1)/2

def fbm(x, y, octaves=4, seed=0):
    val = 0; amp = 1; freq = 1; total_amp = 0
    for i in range(octaves):
        val += noise2d(x*freq, y*freq, seed+i*100) * amp
        total_amp += amp; amp *= 0.5; freq *= 2
    return val / total_amp

# Distance from point to a curved path defined by control points
def dist_to_curve(px, py, points, segments=50):
    min_d = 9999
    for i in range(len(points)-1):
        x0,y0 = points[i]; x1,y1 = points[i+1]
        for t_i in range(segments+1):
            t = t_i/segments
            cx = x0 + (x1-x0)*t
            cy = y0 + (y1-y0)*t
            d = math.sqrt((px-cx)**2 + (py-cy)**2)
            if d < min_d: min_d = d
    return min_d

# ========== TERRAIN DEFINITIONS ==========

# River: meandering from NW to SE
river_points = []
for i in range(20):
    t = i/19
    rx = 3 + t * 18
    ry = 26 - t * 20 + math.sin(t*math.pi*3)*3.5 + math.sin(t*math.pi*5)*1.2
    river_points.append((rx, ry))

# Main road: south gate -> plaza -> north gate
main_road = []
for i in range(20):
    t = i/19
    ry = 2 + t * 28
    rx = 16 + math.sin(t*math.pi*2.5)*1.8 + math.sin(t*math.pi*1.2)*0.8
    main_road.append((rx, ry))

# East branch: plaza -> east
east_road = []
for i in range(12):
    t = i/11
    rx = 16 + t * 11
    ry = 16 + math.sin(t*math.pi*1.5)*1.5
    east_road.append((rx, ry))

# West branch: plaza -> west (toward lake)
west_road = []
for i in range(10):
    t = i/9
    rx = 16 - t * 10
    ry = 15 + math.sin(t*math.pi*1.8)*1.2
    west_road.append((rx, ry))

# NW path: to well/garden area
nw_path = []
for i in range(8):
    t = i/7
    rx = 14 - t*5
    ry = 19 + t*5 + math.sin(t*math.pi*2)*0.8
    nw_path.append((rx, ry))

def is_forest(x, y):
    border = min(x, y, W-1-x, H-1-y)
    n = fbm(x*0.12, y*0.12, 3, seed=7) * 5
    if border + n < 5: return True
    # NE grove
    d = math.sqrt((x-26)**2 + (y-27)**2)
    if d < 3 + fbm(x*0.3, y*0.3, 2, seed=20)*2: return True
    # SE grove
    d2 = math.sqrt((x-27)**2 + (y-5)**2)
    if d2 < 2.5 + fbm(x*0.3, y*0.3, 2, seed=30)*1.5: return True
    return False

def is_river(x, y):
    d = dist_to_curve(x, y, river_points)
    width = 1.3 + fbm(x*0.2, y*0.2, 2, seed=50)*0.8
    return d < width

def is_lake(x, y):
    # Small lake where river widens at SW
    cx, cy = 6, 20
    dx, dy = x-cx, y-cy
    r = math.sqrt(dx*dx/12 + dy*dy/10)
    n = fbm(x*0.2, y*0.2, 3, seed=60)*0.6
    return r < 1.3 + n

def is_pond(x, y):
    cx, cy = 24, 10
    dx, dy = x-cx, y-cy
    r = math.sqrt(dx*dx/6 + dy*dy/4)
    n = fbm(x*0.25, y*0.25, 2, seed=70)*0.4
    return r < 1.0 + n

def is_liquid(x, y):
    if is_forest(x, y): return False
    water = is_river(x, y) or is_lake(x, y) or is_pond(x, y)
    # Bridge cutouts
    if 14 <= x <= 18 and abs(y - river_y_at(x)) < 2.5: 
        # Where main road crosses river
        d_road = dist_to_curve(x, y, main_road)
        if d_road < 2.5: water = False
    # West road bridge
    d_west = dist_to_curve(x, y, west_road)
    if d_west < 2.0 and is_river(x, y): water = False
    return water

def river_y_at(x):
    # approximate river y at given x
    best_y = 16
    best_d = 9999
    for p in river_points:
        if abs(p[0]-x) < best_d:
            best_d = abs(p[0]-x)
            best_y = p[1]
    return best_y

def is_path(x, y):
    if is_forest(x, y): return False
    if is_liquid(x, y): return False
    # Plaza
    dx, dy = x-16, y-16
    if dx*dx/20 + dy*dy/12 < 1: return True
    # Roads
    d_main = dist_to_curve(x, y, main_road)
    if d_main < 1.6: return True
    d_east = dist_to_curve(x, y, east_road)
    if d_east < 1.3: return True
    d_west = dist_to_curve(x, y, west_road)
    if d_west < 1.3: return True
    d_nw = dist_to_curve(x, y, nw_path)
    if d_nw < 1.1: return True
    return False

# ========== RENDER ==========
TILE = 20
GRASS = (86, 190, 86)
DIRT = (175, 145, 90)
WATER = (55, 140, 220)
SHORE = (90, 185, 220)
FOREST = (30, 100, 35)
BRIDGE = (140, 110, 60)

img = Image.new('RGB', (W*TILE, H*TILE))
draw = ImageDraw.Draw(img)

for x in range(W):
    for y in range(H):
        py = H-1-y
        # Grass variation
        gn = fbm(x*0.3, y*0.3, 2, seed=99)
        grass_c = (int(76+gn*20), int(170+gn*15), int(70+gn*20))
        
        if is_forest(x, y):
            fn = fbm(x*0.4, y*0.4, 2, seed=88)
            c = (int(25+fn*15), int(85+fn*20), int(28+fn*15))
        elif is_liquid(x, y):
            adj = False
            for dx2,dy2 in [(0,1),(0,-1),(1,0),(-1,0)]:
                nx,ny = x+dx2,y+dy2
                if 0<=nx<W and 0<=ny<H and not is_liquid(nx,ny) and not is_forest(nx,ny):
                    adj = True
            c = SHORE if adj else WATER
        elif is_path(x, y):
            dn = fbm(x*0.5, y*0.5, 2, seed=77)
            c = (int(165+dn*20), int(135+dn*15), int(80+dn*15))
        else:
            c = grass_c
        draw.rectangle([x*TILE, py*TILE, (x+1)*TILE-1, (py+1)*TILE-1], fill=c)

# Add labels
from PIL import ImageFont
try:
    font = ImageFont.truetype("arial.ttf", 11)
except:
    font = ImageFont.load_default()

labels = [
    (16, H-16, "광장", (255,255,255)),
    (6, H-20, "호수", (255,255,255)),
    (24, H-10, "연못", (255,255,255)),
    (16, H-3, "남문", (255,220,100)),
    (16, H-30, "북문", (255,220,100)),
    (26, H-27, "숲", (200,255,200)),
]
for lx,ly,text,color in labels:
    draw.text((lx*TILE-10, ly*TILE), text, fill=color, font=font)

out = r'C:\Users\darac\.gemini\antigravity\brain\30c79ebb-9e0c-4432-9d21-cdbfd419c0cb\village_natural_preview.png'
img.save(out)
print(f'Saved!')
