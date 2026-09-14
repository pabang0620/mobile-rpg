using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Sapphire.Presentation.Movement;
using Sapphire.Presentation.Skills;
using Sapphire.Presentation.UI;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Result of <see cref="VillageHubUiBuilder.Build"/>: the piece the scene
    /// orchestrator (<see cref="SapphireSceneBuilder"/>) needs to wire into the
    /// composition root.
    /// </summary>
    internal readonly struct UiBuildResult
    {
        internal readonly SimpleMessagePanel MessagePanel;

        internal UiBuildResult(SimpleMessagePanel messagePanel)
        {
            MessagePanel = messagePanel;
        }
    }

    /// <summary>
    /// Builds the VillageHub scene's UI: EventSystem, Canvas, the message
    /// panel (with its close button), and the skill bar (icon buttons + key
    /// hints + range indicator/skill-bar controller wiring). Split out of
    /// <see cref="SapphireSceneBuilder"/> (UI responsibility only - grid/tile/
    /// fence generation lives in <see cref="VillageHubTerrainBuilder"/>).
    /// </summary>
    internal static class VillageHubUiBuilder
    {
        internal static UiBuildResult Build(PlayerGridController playerController, SkillCastFeedback castFeedback)
        {
            BuildEventSystem();
            GameObject canvasGo = BuildCanvas();

            Sprite panelSprite = LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/FantasyPanelBorder.png");
            Sprite buttonSprite = LoadSingleSprite(SapphireSceneBuilder.RootArtDir + "/WideButton.png");

            SimpleMessagePanel messagePanel = BuildMessagePanel(canvasGo, panelSprite, buttonSprite);

            GameObject skillBarGo = BuildSkillBarContainer(canvasGo, panelSprite);
            Button[] skillButtons = BuildSkillButtons(skillBarGo, panelSprite);
            BuildSkillSystems(skillButtons, playerController, castFeedback);

            return new UiBuildResult(messagePanel);
        }

        // --- EventSystem + Canvas ---

        private static void BuildEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static GameObject BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo;
        }

        // --- UI: message panel ---

        private static SimpleMessagePanel BuildMessagePanel(GameObject canvasGo, Sprite panelSprite, Sprite buttonSprite)
        {
            GameObject panelGo = BuildMessagePanelFrame(canvasGo, panelSprite);
            Text text = BuildMessagePanelText(panelGo);
            Button button = BuildMessagePanelCloseButton(panelGo, buttonSprite);

            var messagePanel = panelGo.AddComponent<SimpleMessagePanel>();
            AssignField(messagePanel, "root", panelGo);
            AssignField(messagePanel, "messageText", text);
            AssignField(messagePanel, "closeButton", button);
            return messagePanel;
        }

        private static GameObject BuildMessagePanelFrame(GameObject canvasGo, Sprite panelSprite)
        {
            var panelGo = new GameObject("MessagePanel", typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560, 320);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;
            return panelGo;
        }

        private static Text BuildMessagePanelText(GameObject panelGo)
        {
            var textGo = new GameObject("MessageText", typeof(Text));
            textGo.transform.SetParent(panelGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.08f, 0.35f);
            textRect.anchorMax = new Vector2(0.92f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 28;
            text.text = string.Empty;
            return text;
        }

        private static Button BuildMessagePanelCloseButton(GameObject panelGo, Sprite buttonSprite)
        {
            var buttonGo = new GameObject("CloseButton", typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panelGo.transform, false);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.08f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.08f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(280, 90);
            buttonRect.anchoredPosition = Vector2.zero;
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.sprite = buttonSprite;
            buttonImage.type = Image.Type.Sliced;
            var button = buttonGo.GetComponent<Button>();

            var buttonTextGo = new GameObject("Text", typeof(Text));
            buttonTextGo.transform.SetParent(buttonGo.transform, false);
            var buttonTextRect = buttonTextGo.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            var buttonText = buttonTextGo.GetComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            buttonText.fontSize = 26;
            buttonText.text = "Close";

            return button;
        }

        // --- Skill bar: 4 icon buttons docked bottom-center, click or 1-4 keys.
        // Reuses FantasyPanelBorder.png (sliced) for both the bar background and
        // each button's frame, matching the message panel's style.

        private static GameObject BuildSkillBarContainer(GameObject canvasGo, Sprite panelSprite)
        {
            var skillBarGo = new GameObject("SkillBar", typeof(Image));
            skillBarGo.transform.SetParent(canvasGo.transform, false);
            var skillBarRect = skillBarGo.GetComponent<RectTransform>();
            skillBarRect.anchorMin = new Vector2(0.5f, 0f);
            skillBarRect.anchorMax = new Vector2(0.5f, 0f);
            skillBarRect.pivot = new Vector2(0.5f, 0f);
            skillBarRect.sizeDelta = new Vector2(460, 140);
            skillBarRect.anchoredPosition = new Vector2(0f, 30f);
            var skillBarImage = skillBarGo.GetComponent<Image>();
            skillBarImage.sprite = panelSprite;
            skillBarImage.type = Image.Type.Sliced;
            return skillBarGo;
        }

        private static Button[] BuildSkillButtons(GameObject skillBarGo, Sprite panelSprite)
        {
            var skillButtons = new Button[SkillCatalog.All.Length];
            const float skillButtonSize = 96f;
            const float skillButtonGap = 16f;
            float skillBarStartX = -((skillButtonSize + skillButtonGap) * (SkillCatalog.All.Length - 1)) * 0.5f;

            for (int i = 0; i < SkillCatalog.All.Length; i++)
            {
                float x = skillBarStartX + i * (skillButtonSize + skillButtonGap);
                skillButtons[i] = BuildSkillButton(skillBarGo, panelSprite, SkillCatalog.All[i], i, x, skillButtonSize);
            }

            return skillButtons;
        }

        private static Button BuildSkillButton(GameObject skillBarGo, Sprite panelSprite, SkillDefinition skill, int index, float anchoredX, float size)
        {
            var skillButtonGo = new GameObject("SkillButton_" + index, typeof(Image), typeof(Button));
            skillButtonGo.transform.SetParent(skillBarGo.transform, false);
            var skillButtonRect = skillButtonGo.GetComponent<RectTransform>();
            skillButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
            skillButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
            skillButtonRect.sizeDelta = new Vector2(size, size);
            skillButtonRect.anchoredPosition = new Vector2(anchoredX, 0f);
            var skillButtonImage = skillButtonGo.GetComponent<Image>();
            skillButtonImage.sprite = panelSprite;
            skillButtonImage.type = Image.Type.Sliced;
            Button button = skillButtonGo.GetComponent<Button>();

            Sprite iconSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/SkillIcons.png", skill.IconSpriteName);
            var iconGo = new GameObject("Icon", typeof(Image));
            iconGo.transform.SetParent(skillButtonGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(14f, 14f);
            iconRect.offsetMax = new Vector2(-14f, -14f);
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;

            var hintGo = new GameObject("KeyHint", typeof(Text));
            hintGo.transform.SetParent(skillButtonGo.transform, false);
            var hintRect = hintGo.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.6f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0.34f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            var hintText = hintGo.GetComponent<Text>();
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintText.alignment = TextAnchor.LowerRight;
            hintText.color = Color.white;
            hintText.fontSize = 20;
            hintText.text = (index + 1).ToString();

            return button;
        }

        private static void BuildSkillSystems(Button[] skillButtons, PlayerGridController playerController, SkillCastFeedback castFeedback)
        {
            var skillSystemsGo = new GameObject("SkillSystems");
            var rangeIndicator = skillSystemsGo.AddComponent<SkillRangeIndicator>();
            var skillBarController = skillSystemsGo.AddComponent<SkillBarController>();
            AssignField(skillBarController, "skillButtons", skillButtons);
            AssignField(skillBarController, "player", playerController);
            AssignField(skillBarController, "rangeIndicator", rangeIndicator);
            AssignField(skillBarController, "castFeedback", castFeedback);
        }

        private static Sprite LoadNamedSprite(string path, string name)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);
            if (sprite == null)
            {
                throw new Exception($"Sprite '{name}' not found at {path}");
            }

            return sprite;
        }

        private static Sprite LoadSingleSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new Exception("Sprite missing at " + path);
            }

            return sprite;
        }

        private static void AssignField(object target, string fieldName, object value)
        {
            Type type = target.GetType();
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field == null)
            {
                throw new Exception($"Field '{fieldName}' not found on {type.Name}");
            }

            field.SetValue(target, value);
        }
    }
}
