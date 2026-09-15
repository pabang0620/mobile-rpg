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
    /// the bottom-right radial skill menu (basic attack + fan of skill
    /// buttons + a 5th button outside the fan + range indicator/RadialSkillMenu
    /// wiring), the top-left HP/MP gauges + level text, the top-center region
    /// name banner, and the right-side Odin-style menu panel. Split out of
    /// <see cref="SapphireSceneBuilder"/> (UI responsibility only - grid/tile/
    /// fence generation lives in <see cref="VillageHubTerrainBuilder"/>).
    /// 2026-09-16 (REMEDIATION_PLAN.md Phase 2/3): reworked the skill fan
    /// geometry (exactly 4 fan buttons + 1 outside button, verified non-
    /// overlapping), added the MP gauge + level text + region banner, replaced
    /// the movement pad's builtin circle sprites with MovementStickGold, and
    /// replaced the old flat 7-item text-list main menu with a data-driven
    /// (MenuCatalog) Odin-style sectioned icon grid.
    /// </summary>
    internal static class VillageHubUiBuilder
    {
        internal static UiBuildResult Build(PlayerGridController playerController, PlayerInputReader playerInputReader, SkillCastFeedback castFeedback)
        {
            BuildEventSystem();
            GameObject canvasGo = BuildCanvas();

            Sprite panelSprite = LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MessagePanelFrameGold.png");
            Sprite buttonSprite = LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MenuButtonGold.png");

            SimpleMessagePanel messagePanel = BuildMessagePanel(canvasGo, panelSprite, buttonSprite);

            BuildVirtualMovementPad(canvasGo, playerInputReader);
            VillageHubSkillMenuBuilder.Build(canvasGo, playerController, castFeedback);
            BuildGauges(canvasGo);
            BuildRegionNameBanner(canvasGo);
            VillageHubMenuBuilder.Build(canvasGo, messagePanel);

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
            // 2026-09-15 (Phase 1, REMEDIATION_PLAN.md D1(b)): reference flipped
            // from the previous 720x1280 (portrait) to 1280x720 (landscape) -
            // docs/planning/01_PRODUCT.md is explicit about a landscape,
            // 1280x720-based screen, and the old portrait value was a leftover
            // from an unrelated discarded experiment project, never the SSOT.
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo;
        }

        // --- UI: message panel (also reused as the Odin menu's per-item
        // "coming soon" sub-panel, see VillageHubMenuBuilder.Build ->
        // MainMenuPanel.Select) ---

        private static SimpleMessagePanel BuildMessagePanel(GameObject canvasGo, Sprite panelSprite, Sprite buttonSprite)
        {
            GameObject panelGo = BuildMessagePanelFrame(canvasGo, panelSprite);
            Text title = BuildMessagePanelTitleText(panelGo);
            Text text = BuildMessagePanelText(panelGo);
            Button button = BuildMessagePanelCloseButton(panelGo, buttonSprite);

            var messagePanel = panelGo.AddComponent<SimpleMessagePanel>();
            AssignField(messagePanel, "root", panelGo);
            AssignField(messagePanel, "titleText", title);
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

        private static Text BuildMessagePanelTitleText(GameObject panelGo)
        {
            var textGo = new GameObject("TitleText", typeof(Text));
            textGo.transform.SetParent(panelGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.08f, 0.72f);
            textRect.anchorMax = new Vector2(0.92f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = LoadKoreanFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 30;
            text.text = string.Empty;
            return text;
        }

        private static Text BuildMessagePanelText(GameObject panelGo)
        {
            var textGo = new GameObject("MessageText", typeof(Text));
            textGo.transform.SetParent(panelGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.08f, 0.35f);
            textRect.anchorMax = new Vector2(0.92f, 0.7f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = LoadKoreanFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 24;
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
            buttonText.font = LoadKoreanFont();
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            buttonText.fontSize = 26;
            buttonText.text = "닫기";

            return button;
        }

        // --- Left virtual movement pad: fixed-center touch/mouse stick docked
        // bottom-left. 2026-09-16 (REMEDIATION_PLAN.md Phase 2 item 8): swapped
        // the builtin "Knob" UI sprite (2 plain white translucent circles) for
        // MovementStickGold.png's base ring/knob art - VirtualMovementPad's own
        // drag logic is untouched, only the sprites/sizes change here.

        private static void BuildVirtualMovementPad(GameObject canvasGo, PlayerInputReader playerInputReader)
        {
            Sprite baseSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/MovementStickGold.png", "MovementStickGold_Base");
            Sprite knobSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/MovementStickGold.png", "MovementStickGold_Knob");
            const float padSize = 160f;
            const float knobSize = padSize * 0.4f; // 64, per spec ("40% of base")

            var padGo = new GameObject("VirtualMovementPad", typeof(Image));
            padGo.transform.SetParent(canvasGo.transform, false);
            var padRect = padGo.GetComponent<RectTransform>();
            padRect.anchorMin = Vector2.zero;
            padRect.anchorMax = Vector2.zero;
            padRect.pivot = new Vector2(0.5f, 0.5f);
            padRect.sizeDelta = new Vector2(padSize, padSize);
            padRect.anchoredPosition = new Vector2(padSize * 0.75f, padSize * 0.75f);
            var padImage = padGo.GetComponent<Image>();
            padImage.sprite = baseSprite;
            padImage.preserveAspect = true;

            var knobGo = new GameObject("Knob", typeof(Image));
            knobGo.transform.SetParent(padGo.transform, false);
            var knobRect = knobGo.GetComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(knobSize, knobSize);
            knobRect.anchoredPosition = Vector2.zero;
            var knobImage = knobGo.GetComponent<Image>();
            knobImage.sprite = knobSprite;
            knobImage.preserveAspect = true;
            knobImage.raycastTarget = false;

            var pad = padGo.AddComponent<VirtualMovementPad>();
            AssignField(pad, "background", padRect);
            AssignField(pad, "knob", knobRect);

            AssignField(playerInputReader, "virtualPad", pad);
        }

        // --- Top-left HP + MP gauges + level text (2026-09-16, Phase 2 items
        // 1-3): HealthBarView generalized into GaugeView (Presentation/UI) so
        // the same component drives both bars. There is no stat system in this
        // slice yet, so both gauges are built fixed at 100% fill.

        private static void BuildGauges(GameObject canvasGo)
        {
            Sprite trackSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/HealthBarFrameGold.png", "HealthBarFrame_Track");
            Sprite hpFillSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/HealthBarFrameGold.png", "HealthBarFrame_Fill");
            // 2026-09-16 (F7 fix): was the builtin flat-white sprite tinted
            // sapphire via Image.color - looked visibly flatter/blurrier than
            // the HP fill's painted gradient. GaugeFillMana.png is that same
            // gradient, hue-rotated to blue (see
            // HudArtImportConfigurator.ConfigureGaugeFillMana), so the MP
            // gauge now renders with Image.color left white (no tint needed).
            Sprite mpFillSprite = LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/GaugeFillMana.png");

            // barHeight/fill-anchor values re-verified (not re-derived - see
            // ArtImportConfigurator.ConfigureHealthBarFrame's 2026-09-16 note,
            // already measured against the Track sprite's own tight-cropped
            // alpha bbox rather than the old naive half-cell split): Track tight
            // crop is 1744x358 (aspect ~4.872), so barHeight = 260/4.872 ~= 53.4
            // keeps Image.Type.Simple from squishing it. The interior navy
            // window (where the Fill sits) measured at x=[0.096,0.901]
            // y=[0.249,0.757] of that same tight crop.
            const float barWidth = 260f;
            const float barHeight = 53.4f;
            const float gaugeGap = 6f; // vertical gap between the HP and MP bars

            GaugeView hpGauge = BuildGauge(canvasGo, "HealthBar", trackSprite, hpFillSprite,
                new Vector2(20f, -20f), barWidth, barHeight);

            GaugeView mpGauge = BuildGauge(canvasGo, "ManaBar", trackSprite, mpFillSprite,
                new Vector2(20f, -20f - barHeight - gaugeGap), barWidth, barHeight);
            _ = hpGauge;
            _ = mpGauge;

            BuildLevelText(canvasGo, barWidth, barHeight, gaugeGap);
        }

        private static GaugeView BuildGauge(GameObject canvasGo, string name, Sprite trackSprite, Sprite fillSprite, Vector2 anchoredPosition, float barWidth, float barHeight, Color? fillColor = null)
        {
            var barGo = new GameObject(name, typeof(Image));
            barGo.transform.SetParent(canvasGo.transform, false);
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.sizeDelta = new Vector2(barWidth, barHeight);
            barRect.anchoredPosition = anchoredPosition;
            var trackImage = barGo.GetComponent<Image>();
            trackImage.sprite = trackSprite;
            trackImage.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(Image));
            fillGo.transform.SetParent(barGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.096f, 0.249f);
            fillRect.anchorMax = new Vector2(0.901f, 0.757f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGo.GetComponent<Image>();
            fillImage.sprite = fillSprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.raycastTarget = false;
            if (fillColor.HasValue)
            {
                fillImage.color = fillColor.Value;
            }

            var gaugeView = barGo.AddComponent<GaugeView>();
            AssignField(gaugeView, "fillImage", fillImage);
            return gaugeView;
        }

        private static void BuildLevelText(GameObject canvasGo, float barWidth, float barHeight, float gaugeGap)
        {
            var levelGo = new GameObject("LevelText", typeof(Text));
            levelGo.transform.SetParent(canvasGo.transform, false);
            var levelRect = levelGo.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0f, 1f);
            levelRect.anchorMax = new Vector2(0f, 1f);
            levelRect.pivot = new Vector2(0f, 1f);
            levelRect.sizeDelta = new Vector2(80f, barHeight * 2f + gaugeGap);
            levelRect.anchoredPosition = new Vector2(20f + barWidth + 12f, -20f);
            var levelText = levelGo.GetComponent<Text>();
            levelText.font = LoadKoreanFont();
            levelText.alignment = TextAnchor.MiddleLeft;
            levelText.color = Color.white;
            levelText.fontSize = 26;
            levelText.text = "Lv.1";
            levelText.raycastTarget = false;
        }

        // --- Top-center region name banner (2026-09-16, Phase 2 item 4):
        // reserves the SSOT's "지역/보스 HP" slot with the region name for now
        // (no boss-HP system exists yet).

        private static void BuildRegionNameBanner(GameObject canvasGo)
        {
            Sprite bannerSprite = LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/MenuSectionHeader.png", "MenuSectionHeader");
            const float bannerWidth = 360f;
            // MenuSectionHeader's cropped sprite is 2138x281 (aspect ~7.61) -
            // matching that aspect at bannerWidth=360 keeps the banner
            // unsquished: 360/7.61 ~= 47.3.
            const float bannerHeight = 47.3f;

            var bannerGo = new GameObject("RegionNameBanner", typeof(Image));
            bannerGo.transform.SetParent(canvasGo.transform, false);
            var bannerRect = bannerGo.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.sizeDelta = new Vector2(bannerWidth, bannerHeight);
            bannerRect.anchoredPosition = new Vector2(0f, -16f);
            var bannerImage = bannerGo.GetComponent<Image>();
            bannerImage.sprite = bannerSprite;
            bannerImage.type = Image.Type.Sliced;
            bannerImage.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(bannerGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = LoadKoreanFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 22;
            text.text = "사파이어 광장";
            text.raycastTarget = false;
        }

        // 2026-09-16: every Text in this file used to render with Unity's
        // builtin LegacyRuntime.ttf (an Arial-family font with no Korean glyph
        // coverage) despite every label in this file being Korean text ("메뉴",
        // "장비", "이 기능은 다음 슬라이스에서 연결됩니다", ...) - a real,
        // previously-unaddressed rendering defect (missing-glyph boxes/tofu),
        // not something this task's scope introduced but directly affecting
        // every UI label this pass touches. Fixed at this single call site
        // (internal so VillageHubMenuBuilder's split-out menu code picks it up
        // too) so every caller across both files (message panel text/title,
        // close button, region banner, level text, Odin menu headers/labels)
        // picks it up automatically. Fonts/NotoSansCJKkr-Regular.otf already
        // ships in the project (docs/planning/01_PRODUCT.md's UI section:
        // "한글은 동봉 NotoSansCJKkr 폰트") but nothing loaded it until now.
        private static Font koreanFont;

        internal static Font LoadKoreanFont()
        {
            if (koreanFont == null)
            {
                koreanFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Sapphire/Fonts/NotoSansCJKkr-Regular.otf");
                if (koreanFont == null)
                {
                    throw new Exception("Korean font not found at Assets/Sapphire/Fonts/NotoSansCJKkr-Regular.otf");
                }
            }

            return koreanFont;
        }

        // internal (not private): VillageHubMenuBuilder (split out of this
        // file for the same "keep files under ~500 lines" reason
        // ArtImportConfigurator/VillageHubTerrainBuilder were already split
        // out of SapphireSceneBuilder) also needs these two loaders.
        internal static Sprite LoadNamedSprite(string path, string name)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);
            if (sprite == null)
            {
                throw new Exception($"Sprite '{name}' not found at {path}");
            }

            return sprite;
        }

        internal static Sprite LoadSingleSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new Exception("Sprite missing at " + path);
            }

            return sprite;
        }

        // internal (not private): VillageHubSkillMenuBuilder used to carry its
        // own copy of this exact method (code-reviewer flagged the duplication
        // as a maintenance risk for a reflection helper - a future fix, e.g.
        // the null-target guard just below, is easy to apply to one copy and
        // forget the other) - it now calls this one instead, same pattern as
        // the Load* helpers above.
        internal static void AssignField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                throw new Exception($"AssignField target is null (field '{fieldName}')");
            }

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
