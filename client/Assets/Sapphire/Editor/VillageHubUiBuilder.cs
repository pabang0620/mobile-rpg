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
    /// panel (with its close button), the bottom-left virtual movement pad,
    /// the bottom-right radial skill menu (basic attack + skill circle
    /// buttons + range indicator/RadialSkillMenu wiring), and the top-left HP
    /// bar (visual scaffold, see <see cref="HealthBarView"/>). Split out of
    /// <see cref="SapphireSceneBuilder"/> (UI responsibility only - grid/tile/
    /// fence generation lives in <see cref="VillageHubTerrainBuilder"/>).
    /// 2026-09-14: replaced the old bottom horizontal skill bar with this
    /// left-pad/right-radial-menu layout (see docs/DECISIONS.md); later the
    /// same day, replaced every UI texture (message panel, skill button
    /// frames, skill icons) with newly generated art and added the HP bar.
    /// </summary>
    internal static class VillageHubUiBuilder
    {
        internal static UiBuildResult Build(PlayerGridController playerController, PlayerInputReader playerInputReader, SkillCastFeedback castFeedback)
        {
            BuildEventSystem();
            GameObject canvasGo = BuildCanvas();

            Sprite panelSprite = LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MessagePanelFrame.png");
            Sprite buttonSprite = LoadSingleSprite(SapphireSceneBuilder.RootArtDir + "/WideButton.png");

            SimpleMessagePanel messagePanel = BuildMessagePanel(canvasGo, panelSprite, buttonSprite);

            BuildVirtualMovementPad(canvasGo, playerInputReader);
            BuildRadialSkillMenu(canvasGo, playerController, castFeedback);
            BuildHealthBar(canvasGo);

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

        // --- Left virtual movement pad: fixed-center touch/mouse stick docked
        // bottom-left (2026-09-14, the old build had keyboard-only input).
        // Uses Unity's builtin circular "Knob" UI sprite for both the
        // background ring and the knob - a plain shape needs no new art here,
        // unlike the basic-attack/haste buttons below which needed real icons
        // (see LoadSkillIcon / docs/ASSET_STATUS.md).

        private static void BuildVirtualMovementPad(GameObject canvasGo, PlayerInputReader playerInputReader)
        {
            Sprite circleSprite = LoadBuiltinCircleSprite();
            const float padSize = 200f;

            var padGo = new GameObject("VirtualMovementPad", typeof(Image));
            padGo.transform.SetParent(canvasGo.transform, false);
            var padRect = padGo.GetComponent<RectTransform>();
            padRect.anchorMin = Vector2.zero;
            padRect.anchorMax = Vector2.zero;
            padRect.pivot = new Vector2(0.5f, 0.5f);
            padRect.sizeDelta = new Vector2(padSize, padSize);
            padRect.anchoredPosition = new Vector2(150f, 150f);
            var padImage = padGo.GetComponent<Image>();
            padImage.sprite = circleSprite;
            padImage.color = new Color(1f, 1f, 1f, 0.35f);

            var knobGo = new GameObject("Knob", typeof(Image));
            knobGo.transform.SetParent(padGo.transform, false);
            var knobRect = knobGo.GetComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(padSize * 0.4f, padSize * 0.4f);
            knobRect.anchoredPosition = Vector2.zero;
            var knobImage = knobGo.GetComponent<Image>();
            knobImage.sprite = circleSprite;
            knobImage.color = new Color(1f, 1f, 1f, 0.7f);
            knobImage.raycastTarget = false;

            var pad = padGo.AddComponent<VirtualMovementPad>();
            AssignField(pad, "background", padRect);
            AssignField(pad, "knob", knobRect);

            AssignField(playerInputReader, "virtualPad", pad);
        }

        // --- Right-side radial skill menu: a big center "기본공격" button plus
        // a fan of skill buttons above/left of it (so the fan opens toward the
        // screen center and stays on-screen). 2026-09-14 full UI asset
        // replacement: buttons now use the real SkillButtonFrame.png circular
        // frame art (2 cells - a plain ring for skill slots, a larger ring for
        // the basic-attack button) instead of the Unity builtin "Knob" sprite
        // the movement pad above still uses. Click or key (J for attack, 1-5
        // for skills) both call the same RadialSkillMenu methods.

        // 2026-09-14 bug found via live playtest: this root used to be anchored
        // to the canvas's BOTTOM-LEFT corner (anchorMin/Max = zero) with a fixed
        // anchoredPosition.x = 590, which only lands on the right side if the
        // canvas is exactly the 720-wide reference resolution. CanvasScaler is
        // ScaleWithScreenSize (BuildCanvas), so the canvas's actual unit width
        // equals screenWidth/scaleFactor and only equals 720 when the runtime
        // aspect ratio matches the 720x1280 reference exactly - any other
        // window size (confirmed locally via a stale Windows
        // "Screenmanager Resolution Width/Height" registry override landing on
        // 1201x700) makes the effective canvas ~1257 units wide, so x=590
        // lands near dead-center instead of the right side. The old
        // leftmostEdge assertion below still "passed" because it re-derived
        // the same wrong 720-wide assumption instead of checking real layout.
        // Fix: anchor to the BOTTOM-RIGHT corner instead and offset left from
        // there - anchoredPosition is then always measured from the true right
        // edge regardless of the canvas's actual resolved width.
        private const float ScreenHalfWidth = 360f;

        private static void BuildRadialSkillMenu(GameObject canvasGo, PlayerGridController playerController, SkillCastFeedback castFeedback)
        {
            Sprite skillFrameSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/SkillButtonFrame.png", "SkillButtonFrame_Skill");
            Sprite basicAttackFrameSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/SkillButtonFrame.png", "SkillButtonFrame_BasicAttack");

            var rootGo = new GameObject("RadialSkillMenu", typeof(RectTransform));
            rootGo.transform.SetParent(canvasGo.transform, false);
            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0f);
            rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = Vector2.zero;
            rootRect.anchoredPosition = new Vector2(-130f, 150f);

            Sprite attackIcon = LoadSkillIcon("SkillIcons_BasicAttack");
            Button attackButton = BuildRadialButton(rootGo, basicAttackFrameSprite, "AttackButton", Vector2.zero, 140f, attackIcon, "기본공격", "J");

            const float skillRadius = 130f;
            const float skillButtonSize = 96f;
            const float arcStartDeg = 100f;
            const float arcEndDeg = 190f;
            var skillButtons = new Button[SkillCatalog.All.Length];

            for (int i = 0; i < SkillCatalog.All.Length; i++)
            {
                float t = SkillCatalog.All.Length > 1 ? i / (float)(SkillCatalog.All.Length - 1) : 0f;
                float angleRad = Mathf.Lerp(arcStartDeg, arcEndDeg, t) * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * skillRadius;

                SkillDefinition skill = SkillCatalog.All[i];
                Sprite iconSprite = LoadSkillIcon(skill.IconSpriteName);
                skillButtons[i] = BuildRadialButton(rootGo, skillFrameSprite, "SkillButton_" + i, offset, skillButtonSize, iconSprite, null, (i + 1).ToString());
            }

            // rootRect is now anchored to the canvas's bottom-right corner, so
            // anchoredPosition.x is a leftward offset FROM the true right edge
            // (always negative here). The widest swing's distance from that
            // right edge is this offset's magnitude plus the fan radius and
            // half the button size - as long as that stays under half of the
            // narrowest canvas width we still want to support (the 720
            // reference width), the menu can't cross into the left half on any
            // canvas the CanvasScaler actually produces.
            float distanceFromRightEdge = -rootRect.anchoredPosition.x + skillRadius + skillButtonSize * 0.5f;
            if (distanceFromRightEdge > ScreenHalfWidth)
            {
                throw new Exception($"RadialSkillMenu's widest swing sits {distanceFromRightEdge} units left of the right edge, further than half the {ScreenHalfWidth * 2}-wide reference canvas - it could cross into the screen's left half, opposite the virtual movement pad.");
            }

            BuildSkillSystems(attackButton, skillButtons, playerController, castFeedback);
        }

        // All 6 skill icons (basic attack + the 5 SkillCatalog entries) now live
        // in the single SkillIconsSet.png sheet (2026-09-14 full UI asset
        // replacement, see ArtImportConfigurator.ConfigureSkillIconsSet) -
        // replaces the old 2-sheet SkillIcons.png/SkillIconsExtra.png fallback.
        private static Sprite LoadSkillIcon(string spriteName)
        {
            return LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/SkillIconsSet.png", spriteName);
        }

        private static Button BuildRadialButton(GameObject parent, Sprite circleSprite, string name, Vector2 anchoredPosition, float size, Sprite iconSprite, string labelText, string keyHint)
        {
            var buttonGo = new GameObject(name, typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent.transform, false);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(size, size);
            buttonRect.anchoredPosition = anchoredPosition;
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.sprite = circleSprite;
            Button button = buttonGo.GetComponent<Button>();

            if (iconSprite != null)
            {
                var iconGo = new GameObject("Icon", typeof(Image));
                iconGo.transform.SetParent(buttonGo.transform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                float inset = size * 0.2f;
                iconRect.offsetMin = new Vector2(inset, inset);
                iconRect.offsetMax = new Vector2(-inset, -inset);
                var iconImage = iconGo.GetComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            else if (!string.IsNullOrEmpty(labelText))
            {
                var labelGo = new GameObject("Label", typeof(Text));
                labelGo.transform.SetParent(buttonGo.transform, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var label = labelGo.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.black;
                label.fontSize = 24;
                label.text = labelText;
                label.raycastTarget = false;
            }

            if (!string.IsNullOrEmpty(keyHint))
            {
                var hintGo = new GameObject("KeyHint", typeof(Text));
                hintGo.transform.SetParent(buttonGo.transform, false);
                var hintRect = hintGo.GetComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0.62f, 0f);
                hintRect.anchorMax = new Vector2(1f, 0.32f);
                hintRect.offsetMin = Vector2.zero;
                hintRect.offsetMax = Vector2.zero;
                var hintText = hintGo.GetComponent<Text>();
                hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hintText.alignment = TextAnchor.LowerRight;
                hintText.color = Color.black;
                hintText.fontSize = 18;
                hintText.text = keyHint;
                hintText.raycastTarget = false;
            }

            return button;
        }

        private static void BuildSkillSystems(Button attackButton, Button[] skillButtons, PlayerGridController playerController, SkillCastFeedback castFeedback)
        {
            var skillSystemsGo = new GameObject("SkillSystems");
            var rangeIndicator = skillSystemsGo.AddComponent<SkillRangeIndicator>();
            var radialSkillMenu = skillSystemsGo.AddComponent<RadialSkillMenu>();
            AssignField(radialSkillMenu, "basicAttackButton", attackButton);
            AssignField(radialSkillMenu, "skillButtons", skillButtons);
            AssignField(radialSkillMenu, "player", playerController);
            AssignField(radialSkillMenu, "rangeIndicator", rangeIndicator);
            AssignField(radialSkillMenu, "castFeedback", castFeedback);
        }

        // --- Top-left HP bar: visual scaffold only (2026-09-14, new
        // HealthBarView component in Presentation/UI). There is no combat/damage
        // system in this slice yet, so the bar is built fixed at 100% fill - see
        // HealthBarView's class doc for how a future combat system should wire in
        // real values.

        private static void BuildHealthBar(GameObject canvasGo)
        {
            Sprite trackSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/HealthBarFrame.png", "HealthBarFrame_Track");
            Sprite fillSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/HealthBarFrame.png", "HealthBarFrame_Fill");

            const float barWidth = 260f;
            const float barHeight = 70f;

            var barGo = new GameObject("HealthBar", typeof(Image));
            barGo.transform.SetParent(canvasGo.transform, false);
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.sizeDelta = new Vector2(barWidth, barHeight);
            barRect.anchoredPosition = new Vector2(20f, -20f);
            var trackImage = barGo.GetComponent<Image>();
            trackImage.sprite = trackSprite;
            trackImage.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(Image));
            fillGo.transform.SetParent(barGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.06f, 0.18f);
            fillRect.anchorMax = new Vector2(0.94f, 0.82f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGo.GetComponent<Image>();
            fillImage.sprite = fillSprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.raycastTarget = false;

            var healthBarView = barGo.AddComponent<HealthBarView>();
            AssignField(healthBarView, "fillImage", fillImage);
        }

        private static Sprite LoadBuiltinCircleSprite()
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (sprite == null)
            {
                throw new Exception("Builtin circle sprite (UI/Skin/Knob.psd) not found");
            }

            return sprite;
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
