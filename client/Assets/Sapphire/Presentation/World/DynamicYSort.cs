using UnityEngine;

namespace Sapphire.Presentation.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class DynamicYSort : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        public int OrderOffset = 0;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + OrderOffset;
        }
    }
}
