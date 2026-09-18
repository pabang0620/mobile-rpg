# Slime Kingdom seamless ground V4

Generated with built-in ImageGen. These continuous source images replace the V3 atlas with baked-in dividers. Runtime tiles are independent 512px PNGs with PPU512: exactly one grid cell, with no overlap. Clamp, FullRect, no mipmaps. The earlier PPU508 workaround and perimeter averaging have been removed from Slime Kingdom. Four spatial crops per material are selected by cell-coordinate parity instead of random variation; shoreline crops use the same phase. Initial village settings are unchanged.

Final prompt template:

Create a SINGLE seamless repeating ground texture for an original classic handheld-style 2D fantasy RPG. Material: MATERIAL. Square image completely filled edge to edge with ONE CONTINUOUS homogeneous texture. Crisp restrained 16-bit pixel art, top-down flat terrain, consistent fine scale so it can be subdivided into four equal subtiles. It MUST be seamless, matching opposite edges, no framing, no vignette, no edge shading. NO atlas, NO grid, NO dividing lines, NO border, NO quadrant boundaries, NO panels, NO objects, NO text. Material color and density must stay identical across the whole image. Charming readable game ground, not illustration.

Material substitutions:

- Grass: soft spring mint green meadow grass, sparse short pixel tufts, extremely sparse little clover, uniformly low contrast, no flowers
- Dirt: warm pale sandy beige compact earth, tiny sparse pebbles, extremely soft patches, uniformly low contrast
- Water: calm turquoise aqua water, very gentle small ripple clusters, sparse soft highlights, evenly distributed low contrast, no giant caustic polygons
- Stone: muted warm lavender gray worn limestone ground, small irregular softly shaded stones, subtle sparse moss, low contrast, no big slabs or dark grout

Rebuild: Unity batchmode -executeMethod Sapphire.EditorTools.SlimeSeamlessTileBaker.BakeAndBuild. Produces 16 ground crops and 32 phase-matched shoreline tiles and regenerates scenes. Writes verification/slime-continuous-tiles.png as a repeated-tile visual check. Does not build or launch the player. The old identical-edge assertion was removed: adjacent crops must continue a texture, not repeat the same border pixels.
