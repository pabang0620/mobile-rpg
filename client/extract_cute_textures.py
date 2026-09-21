from PIL import Image, ImageFilter
import os

brain_dir = r'C:\Users\darac\.gemini\antigravity\brain\30c79ebb-9e0c-4432-9d21-cdbfd419c0cb'
out_dir = r'C:\Users\darac\OneDrive\Desktop\games\mobile-rpg\client\Assets\Sapphire\WorldArt\Environment\SeamlessCute'
os.makedirs(out_dir, exist_ok=True)

def make_seamless(img):
    w, h = img.size
    # Simple seamless by mirroring
    # A 64x64 mirrored to 128x128
    new_img = Image.new('RGB', (w*2, h*2))
    new_img.paste(img, (0, 0))
    new_img.paste(img.transpose(Image.FLIP_LEFT_RIGHT), (w, 0))
    new_img.paste(img.transpose(Image.FLIP_TOP_BOTTOM), (0, h))
    new_img.paste(img.transpose(Image.FLIP_LEFT_RIGHT).transpose(Image.FLIP_TOP_BOTTOM), (w, h))
    
    # Resize back to 64x64 so it tiles perfectly
    final = new_img.resize((w, h), Image.NEAREST)
    return final

# TS01 - Grass and Dirt
path1 = os.path.join(brain_dir, 'ts01_ground_path_1789908088379.jpg')
if os.path.exists(path1):
    img1 = Image.open(path1).resize((1024, 1024))
    grass = img1.crop((64, 64, 128, 128))
    grass_seamless = make_seamless(grass)
    grass_512 = grass_seamless.resize((512, 512), Image.NEAREST)
    grass_512.save(os.path.join(out_dir, 'Grass0.png'))
    
    dirt = img1.crop((576, 512, 640, 576))
    dirt_seamless = make_seamless(dirt)
    dirt_512 = dirt_seamless.resize((512, 512), Image.NEAREST)
    dirt_512.save(os.path.join(out_dir, 'Dirt0.png'))

# TS03 - Water
path3 = os.path.join(brain_dir, 'ts03_water_foam_1789908125734.jpg')
if os.path.exists(path3):
    img3 = Image.open(path3).resize((1024, 1024))
    water = img3.crop((0, 0, 64, 64))
    water_seamless = make_seamless(water)
    water_512 = water_seamless.resize((512, 512), Image.NEAREST)
    water_512.save(os.path.join(out_dir, 'Water0.png'))

# TS02 - Stone/Cliff
path2 = os.path.join(brain_dir, 'ts02_elevation_cliff_1789908107373.jpg')
if os.path.exists(path2):
    img2 = Image.open(path2).resize((1024, 1024))
    stone = img2.crop((320, 0, 384, 64))
    stone_seamless = make_seamless(stone)
    stone_512 = stone_seamless.resize((512, 512), Image.NEAREST)
    stone_512.save(os.path.join(out_dir, 'Stone0.png'))

print('Done extracting cute textures!')
