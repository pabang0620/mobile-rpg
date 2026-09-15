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
    /// CharacterSlotFrame 9-sliced panel - same art as the CharacterSelect
    /// cards, see CharacterSelectSceneBuilder - with a portrait, class label,
    /// one-line class description, and a selection state driven by
    /// brightness/scale/glow rather than a border-color swap), a name
    /// InputField, an error line, a 생성 button, and a top-left back button
    /// to CharacterSelect.
    ///
    /// 2026-09-15 (D2 fix): the previous version used a flat untextured
    /// gray Image for the cards (no dedicated art) and a single thick olive
    /// border swap for the selected state - looked unfinished next to the
    /// CharacterSelect screen's frame art (orchestrator screenshot
    /// flow_create.png). It also stacked Title/Cards/NameInput/CreateButton
    /// with no minimum gap, so the name input's top edge touched the cards'
    /// bottom edge and the 생성 button overlapped the input field. This
    /// version reuses CharacterSlotFrame.png for the cards and lays the
    /// whole vertical stack out from named constants with an explicit
    /// >=16px gap between each block (see the Card/Input/Button Y constants
    /// below) - LayoutOverlapGuard.VerifyNoOverlap asserts this at build
    /// time instead of relying on eyeballing a screenshot again.
    /// </summary>
    internal static class CharacterCreateSceneBuilder
    {
        private const string ScenePath = "Assets/Sapphire/Scenes/CharacterCreate.unity";

        private const float CardWidth = 260f;
        private const float CardHeight = 250f;
        private const float CardGap = 40f;
        private const float CardCenterY = 125f;

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
        // soft gold glow behind the card, instead of a thick border-color
        // swap - "보석 장식 추가 금지"). Unselected cards get a 0.6 dark
        // tint applied here as the card's initial state; the runtime
        // brightness/scale toggle on selection change lives in
        // CharacterCreateController (Presentation assembly, can't reference
        // this Editor-only class) as its own matching literals - see
        // CharacterCreateController.ApplyCardSelectionVisual.
        private static readonly Color UnselectedCardTint = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color CardGlowColor = new Color(1f, 0.85f, 0.3f, 0.25f);

        internal static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CharacterFlowUiScaffold.BuildEventSystemAndCanvas();
            Sprite background = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/TitleBackground.png");
            CharacterFlowUiScaffold.BuildFullScreenBackground(canvasGo, background);
            CharacterFlowUiScaffold.BuildLabel(canvasGo, "TitleText", new Vector2(0f, TitleY), new Vector2(600f, TitleHeight), "캐릭터 생성", fontSize: 34);

            Sprite cardFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/CharacterSlotFrame.png");
            Sprite magePortrait = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/PortraitMage.png");
            Sprite warriorPortrait = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/PortraitWarrior.png");

            float mageCenterX = -(CardWidth + CardGap) / 2f;
            float warriorCenterX = (CardWidth + CardGap) / 2f;
            (Button mageButton, GameObject mageGlow) = BuildClassCard(canvasGo, mageCenterX, cardFrame, magePortrait, "법사", "원거리 마법으로 적을 제압하는 마법사", "Mage");
            (Button warriorButton, GameObject warriorGlow) = BuildClassCard(canvasGo, warriorCenterX, cardFrame, warriorPortrait, "전사", "검과 방패로 전선을 지키는 근접 전사", "Warrior");

            Sprite inputFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/InputFieldFrame.png");
            InputField nameInput = BuildNameInput(canvasGo, inputFrame);

            // 2026-09-15 (gemless MapleStory-M rebuild): main action button
            // ("생성") uses ButtonPrimary, the back/secondary button
            // ("캐릭터 선택으로") uses ButtonSecondary - see
            // HudArtImportConfigurator.ConfigureButtons.
            Sprite primaryButtonSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/ButtonPrimary.png", "Normal");
            Sprite secondaryButtonSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/ButtonSecondary.png", "Normal");
            Button createButton = CharacterFlowUiScaffold.BuildLabeledButton(canvasGo, primaryButtonSprite, "CreateButton", new Vector2(0f, CreateButtonY), new Vector2(CreateButtonWidth, CreateButtonHeight), "생성", fontSize: 26);

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
        // origin at card center, top edge at +CardHeight/2 = +125):
        //   topPad(14) -> Portrait(130, top-pivot) -> gap(8) ->
        //   ClassLabel(30, center-pivot) -> gap(6) -> Description(40,
        //   center-pivot)
        // landing at a bottom edge of -103, i.e. 22px inside the card's own
        // bottom edge (-125) - comfortably clear of CharacterSlotFrame's
        // border for this card size (see CharacterFlowArtImportConfigurator's
        // border-measurement comment; ~12 canvas units at this target
        // width). Portrait uses a top-pivot RectTransform so its
        // anchoredPosition is directly "distance below the card's top edge";
        // Label/Description come from CharacterFlowUiScaffold.BuildLabel,
        // which is center-pivot, so their anchoredPosition.y must be the
        // element's own center-Y in this same card-local space, not a
        // top-edge offset - computed explicitly below instead of nesting
        // arithmetic expressions, after an earlier draft of this method got
        // exactly that conversion wrong.
        private const float CardTopPad = 14f;
        private const float CardPortraitHeight = 130f;
        private const float CardPortraitLabelGap = 8f;
        private const float CardLabelHeight = 30f;
        private const float CardLabelDescriptionGap = 6f;
        private const float CardDescriptionHeight = 40f;

        private static (Button button, GameObject glow) BuildClassCard(GameObject canvasGo, float centerX, Sprite cardFrame, Sprite portrait, string label, string description, string debugName)
        {
            float portraitTop = CardHeight / 2f - CardTopPad;
            float portraitBottom = portraitTop - CardPortraitHeight;
            float labelTop = portraitBottom - CardPortraitLabelGap;
            float labelCenterY = labelTop - CardLabelHeight / 2f;
            float labelBottom = labelTop - CardLabelHeight;
            float descriptionTop = labelBottom - CardLabelDescriptionGap;
            float descriptionCenterY = descriptionTop - CardDescriptionHeight / 2f;

            var glowGo = new GameObject("Glow_" + debugName, typeof(Image));
            glowGo.transform.SetParent(canvasGo.transform, false);
            var glowRect = glowGo.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(CardWidth + 28f, CardHeight + 28f);
            glowRect.anchoredPosition = new Vector2(centerX, CardCenterY);
            var glowImage = glowGo.GetComponent<Image>();
            glowImage.color = CardGlowColor;
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

            CharacterFlowUiScaffold.BuildLabel(cardGo, "Label", new Vector2(0f, labelCenterY), new Vector2(CardWidth - 20f, CardLabelHeight), label, fontSize: 26);

            Text descriptionText = CharacterFlowUiScaffold.BuildLabel(cardGo, "Description", new Vector2(0f, descriptionCenterY), new Vector2(CardWidth - 24f, CardDescriptionHeight), description, fontSize: 14);
            descriptionText.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
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
