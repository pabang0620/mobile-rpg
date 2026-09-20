using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Shows a brief highlight over the grid cells a skill's range covers.
    /// Visual-only: no target/collision resolution, just "this is the
    /// area" feedback. Uses a small pool of tinted quad markers so casting
    /// repeatedly does not allocate new GameObjects every time.
    /// </summary>
    public class SkillRangeIndicator : MonoBehaviour
    {
        [SerializeField] private float visibleDuration = 0.1f;
        [SerializeField] private Color tintColor = new Color(0.4f, 0.8f, 1f, 0.35f);

        private readonly List<SpriteRenderer> markerPool = new List<SpriteRenderer>();
        private Sprite markerSprite;
        private Texture2D markerTexture;
        private Coroutine hideRoutine;

        private void Awake()
        {
            markerTexture = new Texture2D(1, 1);
            markerTexture.SetPixel(0, 0, Color.white);
            markerTexture.Apply();
            markerSprite = Sprite.Create(markerTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private void OnDestroy()
        {
            if (markerTexture != null)
            {
                Destroy(markerTexture);
                markerTexture = null;
            }
        }

        public void Show(IReadOnlyList<GridCoord> tiles)
        {
            if (tiles == null)
            {
                HideAll();
                return;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            EnsurePoolSize(tiles.Count);

            for (int i = 0; i < markerPool.Count; i++)
            {
                if (i < tiles.Count)
                {
                    WorldPoint world = GridWorldConversion.GridToWorld(tiles[i]);
                    markerPool[i].transform.position = new Vector3(world.X, world.Y, 0f);
                    markerPool[i].gameObject.SetActive(true);
                }
                else
                {
                    markerPool[i].gameObject.SetActive(false);
                }
            }

            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(visibleDuration);
            HideAll();
            hideRoutine = null;
        }

        private void HideAll()
        {
            foreach (SpriteRenderer marker in markerPool)
            {
                marker.gameObject.SetActive(false);
            }
        }

        private void EnsurePoolSize(int required)
        {
            while (markerPool.Count < required)
            {
                var markerGo = new GameObject("RangeTile_" + markerPool.Count, typeof(SpriteRenderer));
                markerGo.transform.SetParent(transform);
                markerGo.transform.localScale = new Vector3(GridWorldConversion.CellSize, GridWorldConversion.CellSize, 1f);

                var renderer = markerGo.GetComponent<SpriteRenderer>();
                renderer.sprite = markerSprite;
                renderer.color = tintColor;
                renderer.sortingOrder = 5;

                markerGo.SetActive(false);
                markerPool.Add(renderer);
            }
        }
    }
}
