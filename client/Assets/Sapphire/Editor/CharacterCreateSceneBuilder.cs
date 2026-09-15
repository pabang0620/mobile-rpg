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
    /// Builds CharacterCreate.unity: a Mage/Warrior class card pair (each
    /// portrait + label + a highlight overlay CharacterCreateController
    /// toggles on selection), a name InputField, an error line, and a 생성
    /// button. The highlight is a plain untextured Image tint (no dedicated
    /// art asset was commissioned for a "selected" frame) rather than a
    /// missing sprite reference, so this scene renders correctly even before
    /// any Title art exists for everything else on it.
    /// </summary>
    internal static class CharacterCreateSceneBuilder
    {
        private const string ScenePath = "Assets/Sapphire/Scenes/CharacterCreate.unity";
        private const float CardWidth = 260f;
        private const float CardHeight = 340f;
        private const float CardGap = 40f;

        internal static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CharacterFlowUiScaffold.BuildEventSystemAndCanvas();
            Sprite background = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/TitleBackground.png");
            CharacterFlowUiScaffold.BuildFullScreenBackground(canvasGo, background);
            CharacterFlowUiScaffold.BuildLabel(canvasGo, "TitleText", new Vector2(0f, 300f), new Vector2(600f, 60f), "캐릭터 생성", fontSize: 34);

            Sprite magePortrait = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/PortraitMage.png");
            Sprite warriorPortrait = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/PortraitWarrior.png");

            (Button mageButton, GameObject mageHighlight) = BuildClassCard(canvasGo, -(CardWidth + CardGap) / 2f, magePortrait, "법사", "Mage");
            (Button warriorButton, GameObject warriorHighlight) = BuildClassCard(canvasGo, (CardWidth + CardGap) / 2f, warriorPortrait, "전사", "Warrior");

            Sprite inputFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/InputFieldFrame.png");
            InputField nameInput = BuildNameInput(canvasGo, inputFrame);

            Sprite menuButton = VillageHubUiBuilder.LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MenuButtonGold.png");
            Button createButton = CharacterFlowUiScaffold.BuildLabeledButton(canvasGo, menuButton, "CreateButton", new Vector2(0f, -230f), new Vector2(220f, 80f), "생성", fontSize: 26);

            Text errorText = CharacterFlowUiScaffold.BuildLabel(canvasGo, "ErrorText", new Vector2(0f, -290f), new Vector2(600f, 32f), string.Empty, fontSize: 20);
            errorText.color = new Color(1f, 0.5f, 0.5f);

            var controllerGo = new GameObject("CharacterCreateController", typeof(CharacterCreateController));
            var controller = controllerGo.GetComponent<CharacterCreateController>();
            VillageHubUiBuilder.AssignField(controller, "mageCardButton", mageButton);
            VillageHubUiBuilder.AssignField(controller, "mageSelectedHighlight", mageHighlight);
            VillageHubUiBuilder.AssignField(controller, "warriorCardButton", warriorButton);
            VillageHubUiBuilder.AssignField(controller, "warriorSelectedHighlight", warriorHighlight);
            VillageHubUiBuilder.AssignField(controller, "nameInput", nameInput);
            VillageHubUiBuilder.AssignField(controller, "errorText", errorText);
            VillageHubUiBuilder.AssignField(controller, "createButton", createButton);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.Exception("CharacterCreate scene save failed");
            }

            BuildSettingsSceneRegistrar.Register(ScenePath);
        }

        private static (Button button, GameObject highlight) BuildClassCard(GameObject canvasGo, float centerX, Sprite portrait, string label, string debugName)
        {
            var highlightGo = new GameObject("Highlight_" + debugName, typeof(Image));
            highlightGo.transform.SetParent(canvasGo.transform, false);
            var highlightRect = highlightGo.GetComponent<RectTransform>();
            highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
            highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
            highlightRect.sizeDelta = new Vector2(CardWidth + 16f, CardHeight + 16f);
            highlightRect.anchoredPosition = new Vector2(centerX, 30f);
            var highlightImage = highlightGo.GetComponent<Image>();
            highlightImage.color = new Color(1f, 0.85f, 0.3f, 0.35f);
            highlightImage.raycastTarget = false;

            var cardGo = new GameObject("ClassCard_" + debugName, typeof(Image), typeof(Button));
            cardGo.transform.SetParent(canvasGo.transform, false);
            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);
            cardRect.anchoredPosition = new Vector2(centerX, 30f);
            var cardImage = cardGo.GetComponent<Image>();
            cardImage.color = new Color(0.08f, 0.09f, 0.14f, 0.85f);
            Button cardButton = cardGo.GetComponent<Button>();

            var portraitGo = new GameObject("Portrait", typeof(Image));
            portraitGo.transform.SetParent(cardGo.transform, false);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.sizeDelta = new Vector2(180f, 180f);
            portraitRect.anchoredPosition = new Vector2(0f, 50f);
            var portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.sprite = portrait;
            portraitImage.type = Image.Type.Simple;
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            CharacterFlowUiScaffold.BuildLabel(cardGo, "Label", new Vector2(0f, -110f), new Vector2(CardWidth - 20f, 40f), label, fontSize: 28);

            return (cardButton, highlightGo);
        }

        private static InputField BuildNameInput(GameObject canvasGo, Sprite frameSprite)
        {
            Image frame = CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, frameSprite, "NameInput", new Vector2(0f, -170f), new Vector2(360f, 72f));
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
