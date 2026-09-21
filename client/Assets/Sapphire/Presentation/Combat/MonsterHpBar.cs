using UnityEngine;

namespace Sapphire.Presentation.Combat
{
    public class MonsterHpBar : MonoBehaviour
    {
        private Transform fillTransform;
        private SpriteRenderer fillRenderer;
        private SpriteRenderer bgRenderer;

        private const float BarWidth = 0.6f;
        private const float BarHeight = 0.08f;
        private const float YOffset = 0.55f;

        public static MonsterHpBar Create(Transform parent)
        {
            var barRoot = new GameObject("HpBar");
            barRoot.transform.SetParent(parent, false);
            barRoot.transform.localPosition = new Vector3(0f, YOffset, 0f);

            var bgGo = new GameObject("HpBarBg", typeof(SpriteRenderer));
            bgGo.transform.SetParent(barRoot.transform, false);
            bgGo.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);
            var bgSr = bgGo.GetComponent<SpriteRenderer>();
            bgSr.sprite = CreateWhitePixelSprite();
            bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            bgSr.sortingOrder = 90;

            var fillGo = new GameObject("HpBarFill", typeof(SpriteRenderer));
            fillGo.transform.SetParent(barRoot.transform, false);
            fillGo.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);
            var fillSr = fillGo.GetComponent<SpriteRenderer>();
            fillSr.sprite = CreateWhitePixelSprite();
            fillSr.color = new Color(0.2f, 0.9f, 0.2f, 0.9f);
            fillSr.sortingOrder = 91;

            var hpBar = barRoot.AddComponent<MonsterHpBar>();
            hpBar.fillTransform = fillGo.transform;
            hpBar.fillRenderer = fillSr;
            hpBar.bgRenderer = bgSr;

            barRoot.SetActive(false);

            return hpBar;
        }

        public void UpdateFill(float normalizedHp)
        {
            gameObject.SetActive(true);

            float clamped = Mathf.Clamp01(normalizedHp);
            if (fillTransform != null)
            {
                fillTransform.localScale = new Vector3(BarWidth * clamped, BarHeight, 1f);
                fillTransform.localPosition = new Vector3(-(BarWidth * (1f - clamped)) * 0.5f, 0f, 0f);
            }

            if (fillRenderer != null)
            {
                if (clamped > 0.5f)
                    fillRenderer.color = new Color(0.2f, 0.9f, 0.2f, 0.9f);
                else if (clamped > 0.25f)
                    fillRenderer.color = new Color(0.9f, 0.7f, 0.1f, 0.9f);
                else
                    fillRenderer.color = new Color(0.9f, 0.15f, 0.1f, 0.9f);
            }
        }

        private static Sprite CreateWhitePixelSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
