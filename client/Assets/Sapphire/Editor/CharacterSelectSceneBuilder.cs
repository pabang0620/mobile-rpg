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
    ///
    /// 2026-09-15 (D3 fix): CardWidth was 220 while
    /// CharacterFlowArtImportConfigurator.CharacterSlotFrameTargetWidth
    /// (the width CharacterSlotFrame.png's pixelsPerUnit is actually
    /// calibrated against) is 260 - widened to match, which also frees up
    /// enough interior width for the select/delete buttons to clear the
    /// frame's border (see CardButtonEdgeInset below). The name/class+level
    /// text also switches to a two-line "big name / small class·Lv" layout
    /// (orchestrator screenshot flow_select.png showed only "법사 Lv.1",
    /// no name line - turned out to be a real dynamic-font bug, see
    /// NameText's fontSize comment in BuildSlotCard, not just a layout
    /// issue), and the portrait grows to fill more of the card's top area.
    /// </summary>
    internal static class CharacterSelectSceneBuilder
    {
        private const string ScenePath = "Assets/Sapphire/Scenes/CharacterSelect.unity";
        private const float CardWidth = 260f;
        private const float CardHeight = 340f;
        private const float CardGap = 20f;

        // Top-down layout budget inside each filled card (card-local space,
        // y+up, top edge at +CardHeight/2 = +170): topPad(16) ->
        // Portrait(150, top-pivot) -> gap(8) -> NameText(32, center-pivot) ->
        // gap(4) -> ClassLevelText(24, center-pivot) -> gap(8) ->
        // Select/Delete buttons(40, center-pivot), landing with a 58px
        // clearance to the card's bottom edge - see CardButtonBottomPad
        // below for why that number matters (it's the same measurement the
        // buttons' left/right inset uses).
        private const float CardTopPad = 16f;
        private const float CardPortraitHeight = 150f;
        private const float CardPortraitNameGap = 8f;
        private const float CardNameHeight = 32f;
        private const float CardNameClassGap = 4f;
        private const float CardClassHeight = 24f;
        private const float CardClassButtonGap = 8f;
        private const float CardButtonHeight = 40f;
        private const float CardButtonWidth = 70f;
        private const float CardButtonGap = 16f;

        // D3 spec: "카드 내부 여백(테두리 실측 border 36px + 12) 안쪽에
        // 배치" - select/delete must stay this far in from every card edge.
        // Both the button block's computed bottom clearance (58px, from the
        // layout budget above) and its left/right inset (below) are checked
        // against this at build time by VerifyButtonsInsideCard.
        private const float CardButtonEdgeInset = 48f;

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
            var cardCentersX = new float[CharacterRoster.MaxSlots];
            float rowWidth = CharacterRoster.MaxSlots * CardWidth + (CharacterRoster.MaxSlots - 1) * CardGap;
            float leftmostCenterX = -(rowWidth / 2f) + CardWidth / 2f;
            for (int i = 0; i < CharacterRoster.MaxSlots; i++)
            {
                float centerX = leftmostCenterX + i * (CardWidth + CardGap);
                cardCentersX[i] = centerX;
                slotCards[i] = BuildSlotCard(canvasGo, cardFrame, menuButton, i, centerX);
            }

            VerifyNoOverlap(cardCentersX);

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

        // D4 spec: "생성/선택 화면 주요 요소끼리 겹치면 빌드 실패". Checks the
        // title against every card and every card against every other card
        // (cheap - MaxSlots is 4) - the per-card select/delete-vs-border
        // check is separate (VerifyButtonsInsideCard, called from
        // BuildSlotCard where the button rects are already at hand).
        private static void VerifyNoOverlap(float[] cardCentersX)
        {
            Rect titleRect = LayoutOverlapGuard.ToCenterAnchoredCanvasRect(new Vector2(0f, 300f), new Vector2(600f, 60f));
            var elements = new System.Collections.Generic.List<(string, Rect)> { ("TitleText", titleRect) };
            for (int i = 0; i < cardCentersX.Length; i++)
            {
                Rect cardRect = LayoutOverlapGuard.ToCenterAnchoredCanvasRect(new Vector2(cardCentersX[i], 0f), new Vector2(CardWidth, CardHeight));
                elements.Add(("SlotCard_" + i, cardRect));
            }

            LayoutOverlapGuard.VerifyNoOverlap(elements.ToArray());
        }

        private static CharacterSlotCardView BuildSlotCard(GameObject canvasGo, Sprite cardFrame, Sprite buttonSprite, int index, float centerX)
        {
            Image card = CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, cardFrame, "SlotCard_" + index, new Vector2(centerX, 0f), new Vector2(CardWidth, CardHeight));
            GameObject cardGo = card.gameObject;

            GameObject filledRoot = BuildChildRoot(cardGo, "Filled");

            float portraitTop = CardHeight / 2f - CardTopPad;
            float portraitBottom = portraitTop - CardPortraitHeight;
            float nameTop = portraitBottom - CardPortraitNameGap;
            float nameCenterY = nameTop - CardNameHeight / 2f;
            float nameBottom = nameTop - CardNameHeight;
            float classTop = nameBottom - CardNameClassGap;
            float classCenterY = classTop - CardClassHeight / 2f;
            float classBottom = classTop - CardClassHeight;
            float buttonTop = classBottom - CardClassButtonGap;
            float buttonCenterY = buttonTop - CardButtonHeight / 2f;

            var portraitGo = new GameObject("Portrait", typeof(Image));
            portraitGo.transform.SetParent(filledRoot.transform, false);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 1f);
            portraitRect.anchorMax = new Vector2(0.5f, 1f);
            portraitRect.pivot = new Vector2(0.5f, 1f);
            portraitRect.sizeDelta = new Vector2(CardPortraitHeight, CardPortraitHeight);
            portraitRect.anchoredPosition = new Vector2(0f, -CardTopPad);
            var portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.type = Image.Type.Simple;
            portraitImage.preserveAspect = true;

            // 2026-09-15 (D3 fix): fontSize was 24, the ONLY Text component
            // in the entire game using that exact size (every other size is
            // shared by 2-3 Text components elsewhere, all of them baked at
            // scene-build time so their glyph atlas entries exist before any
            // runtime script runs) - confirmed by direct pixel-level capture
            // testing that a Text whose runtime-assigned .text is the FIRST
            // thing anywhere to request glyphs at a brand-new, never-before-
            // used font size renders zero visible vertices (legacy uGUI +
            // dynamic OTF font issue), while the exact same characters at an
            // already-established size (classLevelText's baked-elsewhere 22)
            // render fine. Switched to 22 (already used 3x, including this
            // very card's own baked "+ 생성"/확인/취소 button labels) rather
            // than hunting the underlying engine bug further.
            Text nameText = CharacterFlowUiScaffold.BuildLabel(filledRoot, "NameText", new Vector2(0f, nameCenterY), new Vector2(CardWidth - 24f, CardNameHeight), string.Empty, fontSize: 22);
            Text classLevelText = CharacterFlowUiScaffold.BuildLabel(filledRoot, "ClassLevelText", new Vector2(0f, classCenterY), new Vector2(CardWidth - 24f, CardClassHeight), string.Empty, fontSize: 16);
            classLevelText.color = new Color(0.85f, 0.85f, 0.9f, 1f);

            float buttonCenterX = (CardButtonWidth + CardButtonGap) / 2f;
            Button selectButton = CharacterFlowUiScaffold.BuildLabeledButton(filledRoot, buttonSprite, "SelectButton", new Vector2(-buttonCenterX, buttonCenterY), new Vector2(CardButtonWidth, CardButtonHeight), "선택", fontSize: 16);
            Button deleteButton = CharacterFlowUiScaffold.BuildLabeledButton(filledRoot, buttonSprite, "DeleteButton", new Vector2(buttonCenterX, buttonCenterY), new Vector2(CardButtonWidth, CardButtonHeight), "삭제", fontSize: 16);

            VerifyButtonsInsideCard(index, buttonCenterX, buttonCenterY);

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

        // D3 spec: select/delete must sit inside the card's own border
        // padding (CardButtonEdgeInset, 48px in from every edge). Card and
        // button rects are both computed relative to the card's own local
        // center (0,0) here, which is equivalent to canvas space shifted by
        // -centerX - fine for a pure-containment check since it doesn't
        // depend on the card's absolute canvas position.
        private static void VerifyButtonsInsideCard(int index, float buttonCenterX, float buttonCenterY)
        {
            var cardLocalRect = new Rect(-CardWidth / 2f, -CardHeight / 2f, CardWidth, CardHeight);
            var innerSafeRect = new Rect(
                cardLocalRect.xMin + CardButtonEdgeInset,
                cardLocalRect.yMin + CardButtonEdgeInset,
                cardLocalRect.width - 2f * CardButtonEdgeInset,
                cardLocalRect.height - 2f * CardButtonEdgeInset);

            var selectRect = new Rect(-buttonCenterX - CardButtonWidth / 2f, buttonCenterY - CardButtonHeight / 2f, CardButtonWidth, CardButtonHeight);
            var deleteRect = new Rect(buttonCenterX - CardButtonWidth / 2f, buttonCenterY - CardButtonHeight / 2f, CardButtonWidth, CardButtonHeight);

            LayoutOverlapGuard.VerifyContained($"SlotCard_{index}.SelectButton", selectRect, $"SlotCard_{index}.SafeInterior", innerSafeRect);
            LayoutOverlapGuard.VerifyContained($"SlotCard_{index}.DeleteButton", deleteRect, $"SlotCard_{index}.SafeInterior", innerSafeRect);
            LayoutOverlapGuard.VerifyNoOverlap(($"SlotCard_{index}.SelectButton", selectRect), ($"SlotCard_{index}.DeleteButton", deleteRect));
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
