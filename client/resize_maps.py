from PIL import Image
import os

brain = r'C:\Users\darac\.gemini\antigravity\brain\30c79ebb-9e0c-4432-9d21-cdbfd419c0cb'
baked = r'C:\Users\darac\OneDrive\Desktop\games\mobile-rpg\client\Assets\Sapphire\Art\World\BakedMaps'
os.makedirs(baked, exist_ok=True)

# VillageHub: 32x32 tiles @ 64px = 2048x2048
village = Image.open(os.path.join(brain, 'village_realistic_map_1789911943504.jpg'))
village = village.resize((2048, 2048), Image.LANCZOS)
village.save(os.path.join(baked, 'VillageHub_Baked.png'))

# SlimeKingdom: 40x30 tiles @ 64px = 2560x1920
slime = Image.open(os.path.join(brain, 'slime_kingdom_map_1789911964035.jpg'))
slime = slime.resize((2560, 1920), Image.LANCZOS)
slime.save(os.path.join(baked, 'SlimeKingdom_Baked.png'))

print('Maps resized and saved!')
