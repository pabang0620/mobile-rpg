# Modular64 source prompts

2026-09-21. Generated with the built-in image generation tool (not API/CLI).
These are material sources, not final maps or pre-certified seamless game tiles.
The Editor builder enforces the exact 64px tile layout and compatible boundaries.

Both returned source images are 1254×1254 pixels (the prompt requests 1024×1024,
but tool output dimensions are not assumed). The game atlas is generated at the
exact 1024×1024 target size using 64px cells. Raw sources are preserved unchanged.

2026-09-22 validation: GrassMaster contains nonzero partial alpha (107–236),
despite being requested as continuous material. DirtMaster is fully opaque.
The builder reads terrain RGB independently of source alpha; output coverage
belongs to the generated topology mask (0 or 255). Fully transparent source
pixels are rejected. This is material ingestion, not background extraction.

## GrassMaster.png

Use case: stylized-concept. Asset type: seamless grass material source for a production 64x64 pixel-art top-down fantasy RPG tileset. Generate a square 1024x1024 image showing ONLY an uninterrupted grass ground material, not a map or atlas. Original bright but softly muted fantasy meadow green palette, tiny sparse clean pixel grass blades and minuscule subtle earth freckles, no flowers, no bushes, no trees, no objects. Top-down orthographic flat diffuse lighting. Low contrast, homogeneous density everywhere, no large dark patches, no clumps outlining squares, no vignette, no border, no grid lines, no text. Crisp pixel art, limited palette, no antialiasing or blurry photographic texture. Small clustered pixels, quiet walkable lawn with about 75 percent calm green negative space and 25 percent delicate grass detail. All four edges seamless/tileable and same value and detail density as the center. This image will be used as material for algorithmically assembled 64 pixel modular tiles, not displayed as one map illustration.

## DirtMaster.png

Use case: stylized-concept. Asset type: seamless dirt ground material source for a 64x64 top-down fantasy pixel-art RPG tileset. Create square 1024x1024 image completely filled with one warm golden tan earth material, uniformly lit from overhead, tiny sparse ochre soil freckles and minuscule pale pebbles, restrained 5-8 color palette, flat calm ground. Crisp pixel-art clusters, no antialiasing. No road shape, no edges, no grass, no background transparency, no objects, no scene, no grid, no text, no border, no vignette, no large patches or central highlight. Very low contrast, about 80 percent calm flat tan negative space. Seamlessly tileable across all four edges. Intended as material source used to assemble precisely connected modular dirt road tiles, not a finished map illustration. Match a bright friendly fantasy green meadow with blue-gray cliffs and teal water.

## CliffMaster.png

Use case: stylized-concept. Asset type: seamless cliff rock material source for a production 64x64 top-down fantasy pixel-art RPG tileset. Create a square image completely filled by one continuous blue-gray fantasy stone cliff-face material. Medium-light desaturated slate blue and cool gray palette, small hand-placed pixel clusters suggesting layered rock strata, sparse short cracks and subtle moss-green flecks, readable on a mobile screen. Orthographic flat material study with a slight top-down fantasy game sensibility, uniform lighting and density. Crisp pixel art, limited palette, no antialiasing. No cliff silhouette, no ledges, no ground, no grass field, no water, no stairs, no shadow, no objects, no scene, no grid, no square cells, no text, no border, no vignette, no large focal boulder. Low-to-medium contrast and seamlessly tileable across all four edges. This is source material that will be algorithmically formed into independent 64 pixel modular cliff tiles, not a finished map or atlas. Original design, bright readable Korean mobile fantasy RPG mood.

Tool output: 1254×1254, fully opaque. The source is preserved unchanged; TS02 generation owns the exact 64px geometry and edge conditioning.
