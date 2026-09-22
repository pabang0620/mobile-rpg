using UnityEngine;
using UnityEngine.Tilemaps;

namespace Sapphire.Presentation.World
{
    /// <summary>Cycles shoreline visuals only; frame layout is frame * 47 + mask ordinal.</summary>
    public sealed class ModularFoamAnimator : MonoBehaviour
    {
        [SerializeField] private Tilemap foamTilemap;
        [SerializeField] private Vector3Int[] cells;
        [SerializeField] private int[] maskOrdinals;
        [SerializeField] private TileBase[] frames;
        private float elapsed;
        private int frame;

        public void Configure(Tilemap tilemap, Vector3Int[] shorelineCells, int[] ordinals, TileBase[] frameTiles)
        {
            foamTilemap = tilemap;
            cells = shorelineCells;
            maskOrdinals = ordinals;
            frames = frameTiles;
            elapsed = 0f;
            frame = 0;
            ApplyFrame();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed < 0.2f) return;
            int steps = Mathf.FloorToInt(elapsed / 0.2f);
            elapsed -= steps * 0.2f;
            frame = (frame + steps) % 4;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (foamTilemap == null || cells == null || maskOrdinals == null ||
                cells.Length != maskOrdinals.Length || frames == null || frames.Length != 188) return;
            for (int i = 0; i < cells.Length; i++)
            {
                int ordinal = maskOrdinals[i];
                if (ordinal < 0 || ordinal >= 47) continue;
                var tile = frames[frame * 47 + ordinal];
                if (tile != null) foamTilemap.SetTile(cells[i], tile);
            }
        }
    }
}
