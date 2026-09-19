using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sapphire.Presentation.Login;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Builds Login.unity from scratch: title background/logo, an account-id
    /// InputField, a "게임 시작" button, and an error line. Mirrors
    /// SapphireSceneBuilder's "always start from a brand-new empty scene,
    /// idempotent" approach. Build Settings registration is Login's own
    /// responsibility to put at index 0 (docs/planning/01_PRODUCT.md's "첫
    /// 씬이 로그인" - see CharacterFlowSceneBuilder for the overall order).
    /// </summary>
    internal static class LoginSceneBuilder
    {
        private const string ScenePath = "Assets/Sapphire/Scenes/Login.unity";

        // Sized/centered to contain both AccountIdInput (anchoredPosition
        // (0,-20), size (360,72), so its own top/bottom edges are at
        // 16/-56) and StartButton (anchoredPosition (0,-140), size
        // (280,90), top/bottom edges at -95/-185) with a comfortable margin
        // on every side - matches LoginPortalFrame.png's own authored target
        // size (400x260) exactly, so no unstretched-corner distortion.
        private static readonly Vector2 PortalSize = new Vector2(320f, 220f);
        private const float PortalCenterY = -75f;

        internal static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CharacterFlowUiScaffold.BuildEventSystemAndCanvas();

            CharacterFlowUiScaffold.BuildFullScreenVideoBackground(canvasGo, "Assets/Sapphire/Art/Video/login_village_plaza_loop.mp4");

            Sprite logo = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/TitleLogo.png");
            BuildLogo(canvasGo, logo);

            // 2026-09-16 (premium select/create/login rebuild): wraps the ID
            // input + start button in a single LoginPortalFrame.png backing
            // panel - layout of the two controls themselves is unchanged
            // (still anchoredPosition (0,-20)/(0,-140)), only a background
            // frame is added behind them, sized/centered to contain both
            // with room to spare (PortalCenterY/PortalSize below).
            Sprite portalFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/LoginPortalFrame.png");
            CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, portalFrame, "LoginPortal", new Vector2(0f, PortalCenterY), PortalSize);

            // 2026-09-16 (login input/button redesign task): swapped from the
            // shared InputFieldFrame.png to a dedicated NicknameInputFieldV2.png
            // (dark-navy glass panel with a blue neon glow border) - Character
            // CreateSceneBuilder's own nickname field still uses
            // InputFieldFrame.png unchanged (grep-confirmed before this
            // swap), so that shared asset itself is untouched, only this one
            // call site's sprite reference changed. Layout (position/size)
            // below is unchanged - only the sprite differs.
            // 2026-09-19: switched to simpler InputFieldFrame.png (plain rounded rect)
            // and narrowed width from 360 to 260 for a cleaner look.
            Sprite inputFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/InputFieldFrame.png");
            InputField accountIdInput = BuildAccountIdInput(canvasGo, inputFrame);

            // 2026-09-19: replaced flashy neon LoginStartButtonV2.png with
            // the simpler ButtonSelectV2.png (solid dark button, same asset used
            // in character-select screen). Color-tint transition kept (default).
            Sprite startButtonSprite = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/ButtonSelectV2.png");
            Button startButton = CharacterFlowUiScaffold.BuildLabeledButton(canvasGo, startButtonSprite, "StartButton", new Vector2(0f, -120f), new Vector2(240f, 64f), "게임 시작", fontSize: 26);

            Text errorText = CharacterFlowUiScaffold.BuildLabel(canvasGo, "ErrorText", new Vector2(0f, -165f), new Vector2(400f, 30f), string.Empty, fontSize: 18);
            errorText.color = new Color(1f, 0.5f, 0.5f);

            var controllerGo = new GameObject("LoginScreenController", typeof(LoginScreenController));
            var controller = controllerGo.GetComponent<LoginScreenController>();
            VillageHubUiBuilder.AssignField(controller, "accountIdInput", accountIdInput);
            VillageHubUiBuilder.AssignField(controller, "startButton", startButton);
            VillageHubUiBuilder.AssignField(controller, "errorText", errorText);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.Exception("Login scene save failed");
            }

            BuildSettingsSceneRegistrar.RegisterFirst(ScenePath);
        }

        private static void BuildLogo(GameObject canvasGo, Sprite logoSprite)
        {
            var logoGo = new GameObject("Logo", typeof(Image));
            logoGo.transform.SetParent(canvasGo.transform, false);
            var rect = logoGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(480f, 180f);
            rect.anchoredPosition = new Vector2(0f, -60f);
            var image = logoGo.GetComponent<Image>();
            image.sprite = logoSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }

        private static InputField BuildAccountIdInput(GameObject canvasGo, Sprite frameSprite)
        {
            Image frame = CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, frameSprite, "AccountIdInput", new Vector2(0f, -20f), new Vector2(260f, 60f));
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
            text.supportRichText = false;

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
            placeholder.text = "아이디 입력";

            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.characterLimit = Sapphire.Domain.Account.AccountIdValidator.MaxLength;

            return inputField;
        }
    }
}
