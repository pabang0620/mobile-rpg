using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sapphire.Domain.Character;
using Sapphire.Presentation.CharacterFlow;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Builds CharacterCreate.unity: a Mage/Warrior class card pair (each a
    /// CharacterSlotFrameV2 9-sliced panel - same dark-navy/sapphire-glow art
    /// as the CharacterSelect cards' filled state, see
    /// CharacterSelectSceneBuilder - with a class badge, portrait, class
    /// label, one-line class description, and a selection state driven by
    /// brightness/scale/glow rather than a border-color swap), a name
    /// InputField, an error line, a 생성 button, and a top-left back button
    /// to CharacterSelect.
    ///
    /// 2026-09-16 (premium select/create/login rebuild): cards switch from
    /// the flat beige CharacterSlotFrame.png to CharacterSlotFrameV2.png
    /// (same asset CharacterSelectSceneBuilder's filled cards use - see that
    /// file's class doc comment for the border math: top 72/bottom 90
    /// canvas-unit fixed zones regardless of CardHeight, which is exactly
    /// why the same asset works at both this screen's 250 and Select's 340).
    /// The selected-card glow switches from a flat colored Image rect to the
    /// dedicated CharacterCreateSpotlight.png radial glow, and both cards
    /// gain a class badge at the frame's top notch. The "생성" button
    /// switches from ButtonPrimary to the new ButtonCreateV2 (matching the
    /// gold create-button used for empty slots on CharacterSelect).
    ///
    /// 2026-09-15 (D2 fix, still valid): stacks Title/Cards/NameInput/
    /// CreateButton with an explicit >=16px gap between each block (see the
    /// Card/Input/Button Y constants below) - LayoutOverlapGuard.VerifyNoOverlap
    /// asserts this at build time instead of relying on eyeballing a
    /// screenshot again.
    /// </summary>
    internal static class CharacterCreateSceneBuilder
    {
        private const string ScenePath = "Assets/Sapphire/Scenes/CharacterCreate.unity";

        private const float CardWidth = 260f;
        private const float CardHeight = 250f;
        private const float CardGap = 40f;
        private const float CardCenterY = 125f;

        // Same fixed top-zone budget as CharacterSelectSceneBuilder's filled
        // cards (CharacterSlotFrameV2's badge notch is baked at a fixed
        // distance from the top edge regardless of CardHeight - see that
        // file's class doc comment for the border math).
        private const float BadgeCenterYFromTop = -38f;
        private const float BadgeSize = 60f;
        private const float CardTopZoneHeight = 72f;

        // Vertical stack, each gap >=16px per D2 spec ("제목 - 카드 -
        // 입력칸 - 버튼 순으로 최소 16px 간격"): Title[270,330] -> gap20 ->
        // Card[0,250] -> gap20 -> NameInput[-84,-20] -> gap20 ->
        // CreateButton[-176,-104] -> gap8 -> ErrorText[-216,-184]. All well
        // within the 720-tall reference canvas (+-360 from center).
        private const float TitleY = 300f;
        private const float TitleHeight = 60f;
        private const float NameInputY = -52f;
        private const float NameInputHeight = 64f;
        private const float NameInputWidth = 360f;
        private const float CreateButtonY = -140f;
        private const float CreateButtonWidth = 220f;
        private const float CreateButtonHeight = 72f;
        private const float ErrorTextY = -200f;

        // Top-left back-to-CharacterSelect button (D2 spec: "뒤로가기(캐릭터
        // 선택으로) 버튼이 없으면 좌상단에 추가"). Same corner-anchor pattern
        // VillageHubMenuBuilder's top-right "메뉴" button uses, mirrored to
        // the opposite corner.
        private static readonly Vector2 BackButtonAnchoredPosition = new Vector2(20f, -20f);
        private static readonly Vector2 BackButtonSize = new Vector2(200f, 56f);

        // Selected-card visual (D2 spec: brightness 100% + 1.04x scale + a
        // soft glow behind the card, instead of a thick border-color
        // swap - "보석 장식 추가 금지"). Unselected cards get a 0.6 dark
        // tint applied here as the card's initial state; the runtime
        // brightness/scale toggle on selection change lives in
        // CharacterCreateController (Presentation assembly, can't reference
        // this Editor-only class) as its own matching literals - see
        // CharacterCreateController.ApplyCardSelectionVisual.
        //
        // 2026-09-16 (premium rebuild): the glow switched from a flat
        // colored Image rect to the dedicated CharacterCreateSpotlight.png
        // radial glow sprite (see BuildClassCard) - CardGlowColor is retired.
        private static readonly Color UnselectedCardTint = new Color(0.6f, 0.6f, 0.6f, 1f);

        internal static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CharacterFlowUiScaffold.BuildEventSystemAndCanvas();
            Sprite background = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/TitleBackground.png");
            CharacterFlowUiScaffold.BuildFullScreenBackground(canvasGo, background);
            CharacterFlowUiScaffold.BuildLabel(canvasGo, "TitleText", new Vector2(0f, TitleY), new Vector2(600f, TitleHeight), "캐릭터 생성", fontSize: 34);

            Sprite cardFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/CharacterSlotFrameV2.png");
            Sprite mageBadge = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/ClassBadgeMage.png");
            Sprite warriorBadge = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/ClassBadgeWarrior.png");
            Sprite spotlight = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/CharacterCreateSpotlight.png");
            Sprite magePortrait = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/PortraitMage.png");
            Sprite warriorPortrait = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/PortraitWarrior.png");

            float mageCenterX = -(CardWidth + CardGap) / 2f;
            float warriorCenterX = (CardWidth + CardGap) / 2f;
            (Button mageButton, GameObject mageGlow) = BuildClassCard(canvasGo, mageCenterX, cardFrame, mageBadge, spotlight, magePortrait, "법사", "원거리 마법으로 적을 제압하는 마법사", "Mage");
            (Button warriorButton, GameObject warriorGlow) = BuildClassCard(canvasGo, warriorCenterX, cardFrame, warriorBadge, spotlight, warriorPortrait, "전사", "검과 방패로 전선을 지키는 근접 전사", "Warrior");

            Sprite inputFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/InputFieldFrame.png");
            InputField nameInput = BuildNameInput(canvasGo, inputFrame);

            // 2026-09-16 (premium rebuild): main action button ("생성") now
            // uses ButtonCreateV2 (matching CharacterSelect's empty-slot
            // create button) instead of ButtonPrimary. The back/secondary
            // button ("캐릭터 선택으로") is unaffected - still ButtonSecondary.
            Sprite createButtonSprite = VillageHubUiBuilder.LoadNamedSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/ButtonCreateV2.png", "Normal");
            Sprite secondaryButtonSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/ButtonSecondary.png", "Normal");
            Button createButton = CharacterFlowUiScaffold.BuildLabeledButton(canvasGo, createButtonSprite, "CreateButton", new Vector2(0f, CreateButtonY), new Vector2(CreateButtonWidth, CreateButtonHeight), "생성", fontSize: 26);
            // ButtonCreateV2's fill is light champagne-gold - see
            // CharacterSelectSceneBuilder.BuildSlotCard's identical fix for
            // the same asset.
            createButton.GetComponentInChildren<Text>().color = new Color(0.157f, 0.125f, 0.063f, 1f);

            Text errorText = CharacterFlowUiScaffold.BuildLabel(canvasGo, "ErrorText", new Vector2(0f, ErrorTextY), new Vector2(600f, 32f), string.Empty, fontSize: 20);
            errorText.color = new Color(1f, 0.5f, 0.5f);

            BuildBackButton(canvasGo, secondaryButtonSprite);

            VerifyNoOverlap(mageCenterX, warriorCenterX);

            var controllerGo = new GameObject("CharacterCreateController", typeof(CharacterCreateController));
            var controller = controllerGo.GetComponent<CharacterCreateController>();
            VillageHubUiBuilder.AssignField(controller, "mageCardButton", mageButton);
            VillageHubUiBuilder.AssignField(controller, "mageSelectedHighlight", mageGlow);
            VillageHubUiBuilder.AssignField(controller, "warriorCardButton", warriorButton);
            VillageHubUiBuilder.AssignField(controller, "warriorSelectedHighlight", warriorGlow);
            VillageHubUiBuilder.AssignField(controller, "nameInput", nameInput);
            VillageHubUiBuilder.AssignField(controller, "errorText", errorText);
            VillageHubUiBuilder.AssignField(controller, "createButton", createButton);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.Exception("CharacterCreate scene save failed");
            }

            BuildSettingsSceneRegistrar.Register(ScenePath);
        }

        // D4 spec: "생성/선택 화면 주요 요소끼리 겹치면 빌드 실패". Checks the
        // top-level stack elements only (not each card's internal
        // portrait/label/description children) - those are laid out from a
        // single top-down budget inside BuildClassCard with margins already
        // verified by hand (see that method's comments), and checking every
        // internal child here would just re-describe BuildClassCard's own
        // arithmetic instead of guarding against a real regression class.
        private static void VerifyNoOverlap(float mageCenterX, float warriorCenterX)
        {
            Rect titleRect = LayoutOverlapGuard.ToCenterAnchoredCanvasRect(new Vector2(0f, TitleY), new Vector2(600f, TitleHeight));
            Rect mageCardRect = LayoutOverlapGuard.ToCenterAnchoredCanvasRect(new Vector2(mageCenterX, CardCenterY), new Vector2(CardWidth, CardHeight));
            Rect warriorCardRect = LayoutOverlapGuard.ToCenterAnchoredCanvasRect(new Vector2(warriorCenterX, CardCenterY), new Vector2(CardWidth, CardHeight));
            Rect inputRect = LayoutOverlapGuard.ToCenterAnchoredCanvasRect(new Vector2(0f, NameInputY), new Vector2(NameInputWidth, NameInputHeight));
            Rect buttonRect = LayoutOverlapGuard.ToCenterAnchoredCanvasRect(new Vector2(0f, CreateButtonY), new Vector2(CreateButtonWidth, CreateButtonHeight));
            Rect backRect = LayoutOverlapGuard.ToCanvasRect(new Vector2(0f, 1f), new Vector2(0f, 1f), BackButtonAnchoredPosition, BackButtonSize);

            LayoutOverlapGuard.VerifyNoOverlap(
                ("TitleText", titleRect),
                ("MageCard", mageCardRect),
                ("WarriorCard", warriorCardRect),
                ("NameInput", inputRect),
                ("CreateButton", buttonRect),
                ("BackButton", backRect));
        }

        private static Button BuildBackButton(GameObject canvasGo, Sprite buttonSprite)
        {
            var buttonGo = new GameObject("BackToSelectButton", typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = BackButtonSize;
            rect.anchoredPosition = BackButtonAnchoredPosition;
            var image = buttonGo.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            Button button = buttonGo.GetComponent<Button>();
            button.onClick.AddListener(() => SceneManager.LoadScene("CharacterSelect"));

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = VillageHubUiBuilder.LoadKoreanFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 18;
            text.text = "◀ 캐릭터 선택으로";

            return button;
        }

        // Top-down layout budget inside the card (card-local space, y+up,
        // origin at card center, top edge at +CardHeight/2 = +125). 2026-09-16
        // (premium rebuild): topPad grew from 14 to CardTopZoneHeight(72)+gap
        // (6) = 78, to clear CharacterSlotFrameV2's fixed badge-notch zone
        // (same 72-unit zone CharacterSelectSceneBuilder's filled cards use -
        // see that file's class doc comment), so Portrait/Label/Description
        // all shrank to keep the whole stack inside this card's shorter
        // 250-tall budget:
        //   topPad(78) -> Portrait(90, top-pivot) -> gap(8) ->
        //   ClassLabel(24, center-pivot) -> gap(4) -> Description(28,
        //   center-pivot)
        // landing at a bottom edge of -107, comfortably inside the card's
        // own bottom edge (-125). Portrait uses a top-pivot RectTransform so
        // its anchoredPosition is directly "distance below the card's top
        // edge"; Label/Description come from CharacterFlowUiScaffold.BuildLabel,
        // which is center-pivot, so their anchoredPosition.y must be the
        // element's own center-Y in this same card-local space, not a
        // top-edge offset - computed explicitly below instead of nesting
        // arithmetic expressions, after an earlier draft of this method got
        // exactly that conversion wrong.
        private const float CardTopPad = CardTopZoneHeight + 6f;
        private const float CardPortraitHeight = 90f;
        private const float CardPortraitLabelGap = 8f;
        // 2026-09-16 (premium rebuild, real bug found via CharacterSelect
        // screenshot QA - see CharacterSelectSceneBuilder.BuildSlotCard's
        // identical fix comment): box height must exceed fontSize's own line
        // height (~1.2x) or the default Text.verticalOverflow=Truncate clips
        // glyphs to fully invisible, not just trims descenders. Label is
        // fontSize 26 (needs ~31) - grown from 24 to 32. Description
        // (fontSize 14, needs ~17) was already safely oversized at 28.
        private const float CardLabelHeight = 32f;
        private const float CardLabelDescriptionGap = 4f;
        private const float CardDescriptionHeight = 28f;

        private static (Button button, GameObject glow) BuildClassCard(GameObject canvasGo, float centerX, Sprite cardFrame, Sprite badgeSprite, Sprite spotlightSprite, Sprite portrait, string label, string description, string debugName)
        {
            float portraitTop = CardHeight / 2f - CardTopPad;
            float portraitBottom = portraitTop - CardPortraitHeight;
            float labelTop = portraitBottom - CardPortraitLabelGap;
            float labelCenterY = labelTop - CardLabelHeight / 2f;
            float labelBottom = labelTop - CardLabelHeight;
            float descriptionTop = labelBottom - CardLabelDescriptionGap;
            float descriptionCenterY = descriptionTop - CardDescriptionHeight / 2f;

            // 2026-09-16 (premium rebuild): CharacterCreateSpotlight.png radial
            // glow (Simple, preserveAspect) replaces the old flat-color Image
            // rect - same "behind the card, toggled by SetActive" wiring.
            var glowGo = new GameObject("Glow_" + debugName, typeof(Image));
            glowGo.transform.SetParent(canvasGo.transform, false);
            var glowRect = glowGo.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(CardWidth + 220f, CardHeight + 220f);
            glowRect.anchoredPosition = new Vector2(centerX, CardCenterY);
            var glowImage = glowGo.GetComponent<Image>();
            glowImage.sprite = spotlightSprite;
            glowImage.type = Image.Type.Simple;
            glowImage.preserveAspect = true;
            glowImage.raycastTarget = false;

            var cardGo = new GameObject("ClassCard_" + debugName, typeof(Image), typeof(Button));
            cardGo.transform.SetParent(canvasGo.transform, false);
            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);
            cardRect.anchoredPosition = new Vector2(centerX, CardCenterY);
            var cardImage = cardGo.GetComponent<Image>();
            cardImage.sprite = cardFrame;
            cardImage.type = Image.Type.Sliced;
            cardImage.color = UnselectedCardTint;
            Button cardButton = cardGo.GetComponent<Button>();

            var badgeGo = new GameObject("ClassBadge", typeof(Image));
            badgeGo.transform.SetParent(cardGo.transform, false);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
            badgeRect.anchoredPosition = new Vector2(0f, BadgeCenterYFromTop);
            var badgeImage = badgeGo.GetComponent<Image>();
            badgeImage.sprite = badgeSprite;
            badgeImage.type = Image.Type.Simple;
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;

            var portraitGo = new GameObject("Portrait", typeof(Image));
            portraitGo.transform.SetParent(cardGo.transform, false);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 1f);
            portraitRect.anchorMax = new Vector2(0.5f, 1f);
            portraitRect.pivot = new Vector2(0.5f, 1f);
            portraitRect.sizeDelta = new Vector2(CardPortraitHeight, CardPortraitHeight);
            portraitRect.anchoredPosition = new Vector2(0f, -CardTopPad);
            var portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.sprite = portrait;
            portraitImage.type = Image.Type.Simple;
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            Text labelText = CharacterFlowUiScaffold.BuildLabel(cardGo, "Label", new Vector2(0f, labelCenterY), new Vector2(CardWidth - 20f, CardLabelHeight), label, fontSize: 26);
            labelText.verticalOverflow = VerticalWrapMode.Overflow;

            Text descriptionText = CharacterFlowUiScaffold.BuildLabel(cardGo, "Description", new Vector2(0f, descriptionCenterY), new Vector2(CardWidth - 24f, CardDescriptionHeight), description, fontSize: 14);
            // 2026-09-16 (premium rebuild): background flipped from beige to
            // dark navy - a light sapphire-tinted gray now (same family as
            // CharacterSelectSceneBuilder's classLevelText color), not the
            // old dark brownish-gray (#4a4038) tuned for the beige panel.
            descriptionText.color = new Color(0.78f, 0.85f, 0.95f, 1f);
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            descriptionText.raycastTarget = false;

            return (cardButton, glowGo);
        }

        private static InputField BuildNameInput(GameObject canvasGo, Sprite frameSprite)
        {
            Image frame = CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, frameSprite, "NameInput", new Vector2(0f, NameInputY), new Vector2(NameInputWidth, NameInputHeight));
            GameObject frameGo = frame.gameObject;
            var inputField = frameGo.AddComponent<InputField>();

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(frameGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.06f, 0f);
            textRect.anchorMax = new Vector2(0.94f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = VillageHubUiBuilder.LoadKoreanFont();
            text.color = Color.white;
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleLeft;

            var placeholderGo = new GameObject("Placeholder", typeof(Text));
            placeholderGo.transform.SetParent(frameGo.transform, false);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.06f, 0f);
            placeholderRect.anchorMax = new Vector2(0.94f, 1f);
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            var placeholder = placeholderGo.GetComponent<Text>();
            placeholder.font = VillageHubUiBuilder.LoadKoreanFont();
            placeholder.color = new Color(1f, 1f, 1f, 0.5f);
            placeholder.fontSize = 26;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.text = $"이름 ({CharacterNameValidator.MinLength}~{CharacterNameValidator.MaxLength}자, 한글/영문/숫자)";

            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.characterLimit = CharacterNameValidator.MaxLength;

            return inputField;
        }
    }
}
