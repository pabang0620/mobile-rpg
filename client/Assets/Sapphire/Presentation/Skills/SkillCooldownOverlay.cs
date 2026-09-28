using UnityEngine;
using UnityEngine.UI;
using Sapphire.Domain.Skills;
using Sapphire.Presentation.Combat;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// Cooldown / mana feedback drawn over one skill button: a dark radial
    /// sweep that shrinks as the cooldown runs out, the remaining seconds, and
    /// a blue-grey dim while the player cannot afford the skill's mana.
    ///
    /// Built entirely at runtime by RadialSkillMenu.Awake (children are created
    /// here, nothing is serialized), so the scene builders need no change and
    /// there is no scene-save listener trap. Reads state only - it never
    /// mutates mana or cooldowns.
    /// </summary>
    public sealed class SkillCooldownOverlay : MonoBehaviour
    {
        private static readonly Color SweepColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color NoManaTint = new Color(0.45f, 0.5f, 0.75f, 1f);

        private PlayerCombatController combat;
        private string skillId;
        private float cooldownSeconds;
        private int manaCost;
        private Image buttonImage;
        private Color buttonBaseColor;
        private Image sweep;
        private Text secondsText;
        private string shownSeconds;

        public static SkillCooldownOverlay Attach(Button button, string skillId, PlayerCombatController combat)
        {
            if (button == null || combat == null || !SkillCombatCatalog.TryGet(skillId, out SkillCombatSpec spec))
            {
                return null;
            }

            var overlay = button.gameObject.AddComponent<SkillCooldownOverlay>();
            overlay.Build(button, spec, combat);
            return overlay;
        }

        private void Build(Button button, SkillCombatSpec spec, PlayerCombatController owner)
        {
            combat = owner;
            skillId = spec.SkillId;
            cooldownSeconds = spec.CooldownSeconds;
            manaCost = spec.ManaCost;
            buttonImage = button.targetGraphic as Image;
            buttonBaseColor = buttonImage != null ? buttonImage.color : Color.white;

            var sweepGo = new GameObject("CooldownSweep", typeof(RectTransform), typeof(Image));
            sweepGo.transform.SetParent(button.transform, false);
            Stretch(sweepGo.GetComponent<RectTransform>());
            sweep = sweepGo.GetComponent<Image>();
            sweep.sprite = buttonImage != null ? buttonImage.sprite : null;
            sweep.type = Image.Type.Filled;
            sweep.fillMethod = Image.FillMethod.Radial360;
            sweep.fillOrigin = (int)Image.Origin360.Top;
            sweep.fillClockwise = false;
            sweep.color = SweepColor;
            sweep.raycastTarget = false;
            sweep.enabled = false;

            Text label = button.GetComponentInChildren<Text>(true);
            // Radial skill buttons carry only Image/Icon children (no Text), so fall
            // back to Unity's built-in font - only digits are drawn.
            Font font = label != null ? label.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                var textGo = new GameObject("CooldownSeconds", typeof(RectTransform), typeof(Text), typeof(Outline));
                textGo.transform.SetParent(button.transform, false);
                Stretch(textGo.GetComponent<RectTransform>());
                secondsText = textGo.GetComponent<Text>();
                secondsText.font = font;
                secondsText.fontSize = 30;
                secondsText.fontStyle = FontStyle.Bold;
                secondsText.alignment = TextAnchor.MiddleCenter;
                secondsText.color = Color.white;
                secondsText.raycastTarget = false;
                textGo.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
                secondsText.enabled = false;
            }
        }

        private void Update()
        {
            if (combat == null || combat.Cooldowns == null)
            {
                return;
            }

            double remaining = combat.Cooldowns.RemainingSeconds(skillId, Time.time);
            bool cooling = remaining > 0.0 && cooldownSeconds > 0f;

            sweep.enabled = cooling;
            if (cooling)
            {
                sweep.fillAmount = Mathf.Clamp01((float)(remaining / cooldownSeconds));
            }

            if (secondsText != null)
            {
                secondsText.enabled = cooling;
                if (cooling)
                {
                    string label = remaining >= 1.0
                        ? Mathf.CeilToInt((float)remaining).ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : remaining.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                    if (label != shownSeconds)
                    {
                        shownSeconds = label;
                        secondsText.text = label;
                    }
                }
            }

            if (buttonImage != null)
            {
                bool affordable = combat.Mana == null || combat.Mana.CurrentMp >= manaCost;
                buttonImage.color = affordable ? buttonBaseColor : buttonBaseColor * NoManaTint;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
