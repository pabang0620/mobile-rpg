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
    /// Builds CharacterSelect.unity: up to CharacterRoster.MaxSlots (4)
    /// slot cards in a row (each a CharacterSlotFrame 9-sliced panel with a
    /// "filled" sub-view - portrait/name/class+level/선택/삭제 - and an
    /// "empty" sub-view - a lone 생성 button, toggled at runtime by
    /// CharacterSelectController) plus a shared ConfirmDialog for the
    /// delete confirmation.
    /// </summary>
    internal static class CharacterSelectSceneBuilder
    {
        private const string ScenePath = "Assets/Sapphire/Scenes/CharacterSelect.unity";
        private const float CardWidth = 220f;
        private const float CardHeight = 320f;
        private const float CardGap = 20f;

        internal static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CharacterFlowUiScaffold.BuildEventSystemAndCanvas();
            Sprite background = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/TitleBackground.png");
            CharacterFlowUiScaffold.BuildFullScreenBackground(canvasGo, background);
            CharacterFlowUiScaffold.BuildLabel(canvasGo, "TitleText", new Vector2(0f, 300f), new Vector2(600f, 60f), "캐릭터 선택", fontSize: 34);

            Sprite cardFrame = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/CharacterSlotFrame.png");
            Sprite menuButton = VillageHubUiBuilder.LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MenuButtonGold.png");
            Sprite magePortrait = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/PortraitMage.png");
            Sprite warriorPortrait = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/PortraitWarrior.png");

            var slotCards = new CharacterSlotCardView[CharacterRoster.MaxSlots];
            float rowWidth = CharacterRoster.MaxSlots * CardWidth + (CharacterRoster.MaxSlots - 1) * CardGap;
            float leftmostCenterX = -(rowWidth / 2f) + CardWidth / 2f;
            for (int i = 0; i < CharacterRoster.MaxSlots; i++)
            {
                float centerX = leftmostCenterX + i * (CardWidth + CardGap);
                slotCards[i] = BuildSlotCard(canvasGo, cardFrame, menuButton, i, centerX);
            }

            ConfirmDialog confirmDialog = BuildConfirmDialog(canvasGo, menuButton);

            var controllerGo = new GameObject("CharacterSelectController", typeof(CharacterSelectController));
            var controller = controllerGo.GetComponent<CharacterSelectController>();
            VillageHubUiBuilder.AssignField(controller, "magePortrait", magePortrait);
            VillageHubUiBuilder.AssignField(controller, "warriorPortrait", warriorPortrait);
            VillageHubUiBuilder.AssignField(controller, "slotCards", slotCards);
            VillageHubUiBuilder.AssignField(controller, "confirmDialog", confirmDialog);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.Exception("CharacterSelect scene save failed");
            }

            BuildSettingsSceneRegistrar.Register(ScenePath);
        }

        private static CharacterSlotCardView BuildSlotCard(GameObject canvasGo, Sprite cardFrame, Sprite buttonSprite, int index, float centerX)
        {
            Image card = CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, cardFrame, "SlotCard_" + index, new Vector2(centerX, 0f), new Vector2(CardWidth, CardHeight));
            GameObject cardGo = card.gameObject;

            GameObject filledRoot = BuildChildRoot(cardGo, "Filled");
            var portraitGo = new GameObject("Portrait", typeof(Image));
            portraitGo.transform.SetParent(filledRoot.transform, false);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.sizeDelta = new Vector2(120f, 120f);
            portraitRect.anchoredPosition = new Vector2(0f, 70f);
            var portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.type = Image.Type.Simple;
            portraitImage.preserveAspect = true;

            Text nameText = CharacterFlowUiScaffold.BuildLabel(filledRoot, "NameText", new Vector2(0f, -20f), new Vector2(CardWidth - 20f, 30f), string.Empty, fontSize: 22);
            Text classLevelText = CharacterFlowUiScaffold.BuildLabel(filledRoot, "ClassLevelText", new Vector2(0f, -52f), new Vector2(CardWidth - 20f, 26f), string.Empty, fontSize: 18);

            Button selectButton = CharacterFlowUiScaffold.BuildLabeledButton(filledRoot, buttonSprite, "SelectButton", new Vector2(-50f, -120f), new Vector2(90f, 44f), "선택", fontSize: 18);
            Button deleteButton = CharacterFlowUiScaffold.BuildLabeledButton(filledRoot, buttonSprite, "DeleteButton", new Vector2(50f, -120f), new Vector2(90f, 44f), "삭제", fontSize: 18);

            GameObject emptyRoot = BuildChildRoot(cardGo, "Empty");
            Button createButton = CharacterFlowUiScaffold.BuildLabeledButton(emptyRoot, buttonSprite, "CreateButton", Vector2.zero, new Vector2(150f, 64f), "+ 생성", fontSize: 22);

            return new CharacterSlotCardView
            {
                filledRoot = filledRoot,
                emptyRoot = emptyRoot,
                portraitImage = portraitImage,
                nameText = nameText,
                classLevelText = classLevelText,
                selectButton = selectButton,
                deleteButton = deleteButton,
                createButton = createButton,
            };
        }

        private static GameObject BuildChildRoot(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private static ConfirmDialog BuildConfirmDialog(GameObject canvasGo, Sprite buttonSprite)
        {
            Sprite panelSprite = VillageHubUiBuilder.LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MessagePanelFrameGold.png");
            Image panel = CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, panelSprite, "ConfirmDialog", Vector2.zero, new Vector2(500f, 260f));
            GameObject panelGo = panel.gameObject;

            Text title = CharacterFlowUiScaffold.BuildLabel(panelGo, "TitleText", new Vector2(0f, 70f), new Vector2(440f, 40f), string.Empty, fontSize: 26);
            Text body = CharacterFlowUiScaffold.BuildLabel(panelGo, "BodyText", new Vector2(0f, 10f), new Vector2(440f, 80f), string.Empty, fontSize: 20);
            Button confirmButton = CharacterFlowUiScaffold.BuildLabeledButton(panelGo, buttonSprite, "ConfirmButton", new Vector2(-90f, -80f), new Vector2(140f, 60f), "확인", fontSize: 22);
            Button cancelButton = CharacterFlowUiScaffold.BuildLabeledButton(panelGo, buttonSprite, "CancelButton", new Vector2(90f, -80f), new Vector2(140f, 60f), "취소", fontSize: 22);

            var dialog = panelGo.AddComponent<ConfirmDialog>();
            VillageHubUiBuilder.AssignField(dialog, "root", panelGo);
            VillageHubUiBuilder.AssignField(dialog, "titleText", title);
            VillageHubUiBuilder.AssignField(dialog, "bodyText", body);
            VillageHubUiBuilder.AssignField(dialog, "confirmButton", confirmButton);
            VillageHubUiBuilder.AssignField(dialog, "cancelButton", cancelButton);
            return dialog;
        }
    }
}
