from PIL import Image, ImageFilter
import os

brain_dir = r'C:\Users\darac\.gemini\antigravity\brain\30c79ebb-9e0c-4432-9d21-cdbfd419c0cb'
out_dir = r'C:\Users\darac\OneDrive\Desktop\games\mobile-rpg\client\Assets\Sapphire\Art\World\SlimeKingdom\SeamlessCute'
os.makedirs(out_dir, exist_ok=True)

def make_seamless(img):
    w, h = img.size
    new_img = Image.new('RGB', (w*2, h*2))
    new_img.paste(img, (0, 0))
    new_img.paste(img.transpose(Image.FLIP_LEFT_RIGHT), (w, 0))
    new_img.paste(img.transpose(Image.FLIP_TOP_BOTTOM), (0, h))
    new_img.paste(img.transpose(Image.FLIP_LEFT_RIGHT).transpose(Image.FLIP_TOP_BOTTOM), (w, h))
    final = new_img.resize((512, 512), Image.BICUBIC)
    return final

# Function to crop avoiding the 2-pixel grid lines on the edges
def get_clean_crop(img, start_x, start_y, size=64, border=4):
    # Crop the exact cell
    cell = img.crop((start_x, start_y, start_x + size, start_y + size))
    # Crop inside the cell to remove grid lines
    inner = cell.crop((border, border, size - border, size - border))
    return inner

# TS01 - Grass and Dirt
path1 = os.path.join(brain_dir, 'ts01_ground_path_1789908088379.jpg')
if os.path.exists(path1):
    img1 = Image.open(path1).resize((1024, 1024))
    
    grass = get_clean_crop(img1, 64, 64)
    grass_512 = make_seamless(grass)
    grass_512.save(os.path.join(out_dir, 'Grass0.png'))
    
    dirt = get_clean_crop(img1, 576, 512)
    dirt_512 = make_seamless(dirt)
    dirt_512.save(os.path.join(out_dir, 'Dirt0.png'))

# TS03 - Water
path3 = os.path.join(brain_dir, 'ts03_water_foam_1789908125734.jpg')
if os.path.exists(path3):
    img3 = Image.open(path3).resize((1024, 1024))
    water = get_clean_crop(img3, 0, 0)
    water_512 = make_seamless(water)
    water_512.save(os.path.join(out_dir, 'Water0.png'))

# TS02 - Stone/Cliff
path2 = os.path.join(brain_dir, 'ts02_elevation_cliff_1789908107373.jpg')
if os.path.exists(path2):
    img2 = Image.open(path2).resize((1024, 1024))
    # Stone cell is around (320, 0)
    stone = get_clean_crop(img2, 320, 0)
    stone_512 = make_seamless(stone)
    stone_512.save(os.path.join(out_dir, 'Stone0.png'))

print('Cleaned textures!')
