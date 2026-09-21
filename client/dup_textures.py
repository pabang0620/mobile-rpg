from PIL import Image, ImageFilter
import os

out_dir = r'C:\Users\darac\OneDrive\Desktop\games\mobile-rpg\client\Assets\Sapphire\Art\World\SlimeKingdom\SeamlessCute'

grass0 = Image.open(os.path.join(out_dir, 'Grass0.png'))
dirt0 = Image.open(os.path.join(out_dir, 'Dirt0.png'))
stone0 = Image.open(os.path.join(out_dir, 'Stone0.png'))
water0 = Image.open(os.path.join(out_dir, 'Water0.png'))

for i in range(1, 4):
    grass0.rotate(i*90).save(os.path.join(out_dir, f'Grass{i}.png'))
    dirt0.rotate(i*90).save(os.path.join(out_dir, f'Dirt{i}.png'))
    stone0.rotate(i*90).save(os.path.join(out_dir, f'Stone{i}.png'))
    water0.rotate(i*90).save(os.path.join(out_dir, f'Water{i}.png'))

print('Duplicated base textures!')
