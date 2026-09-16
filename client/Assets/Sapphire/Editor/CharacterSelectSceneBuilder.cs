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
    /// slot cards in a row (each a CharacterSlotFrameV2/EmptyV2 9-sliced
    /// panel with a "filled" sub-view - class badge/pedestal/portrait/
    /// nameplate/선택/삭제 - and an "empty" sub-view - a lone 생성 button,
    /// toggled at runtime by CharacterSelectController) plus a shared
    /// ConfirmDialog for the delete confirmation.
    ///
    /// 2026-09-16 (premium select/create/login rebuild): replaces the flat
    /// beige CharacterSlotFrame.png with the new dark-navy/sapphire-glow
    /// CharacterSlotFrameV2 (filled)/CharacterSlotFrameEmptyV2 (empty) pair
    /// from tools/ui_kit/build_title_kit_v2.py - see
    /// CharacterFlowArtImportConfigurator.SlotFrameBorder's doc comment for
    /// the border math this layout is built against (top 72/bottom 90
    /// canvas-unit fixed zones for the badge notch and the
    /// nameplate+buttons footer respectively, regardless of CardHeight).
    ///
    /// 2026-09-15 (D3 fix, still valid): CardWidth was 220 while the frame
    /// art's calibrated width is 260 - widened to match, which also frees up
    /// enough interior width for the select/delete buttons to clear the
    /// frame's border (see CardButtonEdgeInset below). The name/class+level
    /// text also switches to a two-line "big name / small class·Lv" layout
    /// (orchestrator screenshot flow_select.png showed only "법사 Lv.1",
    /// no name line - turned out to be a real dynamic-font bug, see
    /// NameText's fontSize comment in BuildSlotCard, not just a layout
    /// issue).
    /// </summary>
    internal static class CharacterSelectSceneBuilder
    {
        private const string ScenePath = "Assets/Sapphire/Scenes/CharacterSelect.unity";
        private const float CardWidth = 260f;
        private const float CardHeight = 340f;
        private const float CardGap = 20f;

        // Top-down layout budget inside each filled card (card-local space,
        // y+up, top edge at +CardHeight/2 = +170, bottom edge at -170).
        // Fixed zones (independent of CardHeight, see the class doc comment
        // above): badge notch zone = top 72 units, nameplate+buttons footer
        // zone = bottom 90 units. Everything between (the "middle zone",
        // here 340-72-90=178 units, from y=+98 to y=-80) holds the pedestal
        // + portrait.
        private const float CardTopZoneHeight = 72f;
        private const float CardBottomZoneHeight = 90f;
        private const float BadgeCenterYFromTop = -38f;
        private const float BadgeSize = 60f;
        private const float PedestalSize = 220f;
        private const float PedestalCenterYFromTop = -128f;
        private const float CardPortraitTopGap = 6f;
        private const float CardPortraitHeight = 170f;

        // Footer stack (from the bottom-zone's own top edge, y=-80,
        // downward to the card's bottom edge, y=-170 - 90 units total):
        // pad(2) -> NameplateBar(44, center-pivot, holds Name+Class/Lv text)
        // -> gap(4) -> Select/Delete buttons(40, center-pivot), landing
        // exactly at the card's bottom edge (2+44+4+40=90) - see
        // VerifyButtonsInsideCard for the left/right inset check.
        private const float FooterTopPad = 2f;
        private const float CardNameplateHeight = 44f;
        private const float NameplateButtonGap = 4f;
        // Note: ButtonSelectV2/DeleteV2's own authored "native cell" size is
        // 96x40 target, but their 9-slice border (40 native/13.3 canvas
        // units, see CharacterFlowArtImportConfigurator.SelectDeleteButtonBorder)
        // is small enough that Image.Type.Sliced renders correctly at any
        // width comfortably above 2*13.3 - kept at the original 70/16 (not
        // widened to 96) so both buttons still fit inside CardButtonEdgeInset
        // (VerifyButtonsInsideCard checks this at build time).
        private const float CardButtonHeight = 40f;
        private const float CardButtonWidth = 70f;
        private const float CardButtonGap = 16f;

        // D3 spec (still valid): select/delete must stay this far in from
        // every card edge. Checked at build time by VerifyButtonsInsideCard.
        private const float CardButtonEdgeInset = 48f;

        internal static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = CharacterFlowUiScaffold.BuildEventSystemAndCanvas();
            Sprite background = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/TitleBackground.png");
            CharacterFlowUiScaffold.BuildFullScreenBackground(canvasGo, background);
            CharacterFlowUiScaffold.BuildLabel(canvasGo, "TitleText", new Vector2(0f, 300f), new Vector2(600f, 60f), "캐릭터 선택", fontSize: 34);

            Sprite cardFrameFilled = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/CharacterSlotFrameV2.png");
            Sprite cardFrameEmpty = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/CharacterSlotFrameEmptyV2.png");
            Sprite mageBadge = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/ClassBadgeMage.png");
            Sprite warriorBadge = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/ClassBadgeWarrior.png");
            Sprite pedestal = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/CharacterPedestal.png");
            Sprite nameplate = VillageHubUiBuilder.LoadSingleSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/NameplateBar.png");
            Sprite selectButtonSprite = VillageHubUiBuilder.LoadNamedSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/ButtonSelectV2.png", "Normal");
            Sprite deleteButtonSprite = VillageHubUiBuilder.LoadNamedSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/ButtonDeleteV2.png", "Normal");
            Sprite createButtonSprite = VillageHubUiBuilder.LoadNamedSprite(CharacterFlowArtImportConfigurator.TitleArtDir + "/ButtonCreateV2.png", "Normal");
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
                slotCards[i] = BuildSlotCard(canvasGo, cardFrameFilled, pedestal, nameplate, selectButtonSprite, deleteButtonSprite, createButtonSprite, i, centerX);
            }

            VerifyNoOverlap(cardCentersX);

            ConfirmDialog confirmDialog = BuildConfirmDialog(canvasGo, menuButton);

            var controllerGo = new GameObject("CharacterSelectController", typeof(CharacterSelectController));
            var controller = controllerGo.GetComponent<CharacterSelectController>();
            VillageHubUiBuilder.AssignField(controller, "magePortrait", magePortrait);
            VillageHubUiBuilder.AssignField(controller, "warriorPortrait", warriorPortrait);
            VillageHubUiBuilder.AssignField(controller, "cardFrameFilled", cardFrameFilled);
            VillageHubUiBuilder.AssignField(controller, "cardFrameEmpty", cardFrameEmpty);
            VillageHubUiBuilder.AssignField(controller, "mageBadge", mageBadge);
            VillageHubUiBuilder.AssignField(controller, "warriorBadge", warriorBadge);
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

        private static CharacterSlotCardView BuildSlotCard(GameObject canvasGo, Sprite cardFrameFilled, Sprite pedestalSprite, Sprite nameplateSprite, Sprite selectButtonSprite, Sprite deleteButtonSprite, Sprite createButtonSprite, int index, float centerX)
        {
            Image card = CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, cardFrameFilled, "SlotCard_" + index, new Vector2(centerX, 0f), new Vector2(CardWidth, CardHeight));
            GameObject cardGo = card.gameObject;

            GameObject filledRoot = BuildChildRoot(cardGo, "Filled");

            // Class badge, sits in the frame's baked top notch (see class
            // doc comment) - sprite assigned per-slot by CharacterSelectController.
            var badgeGo = new GameObject("ClassBadge", typeof(Image));
            badgeGo.transform.SetParent(filledRoot.transform, false);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
            badgeRect.anchoredPosition = new Vector2(0f, BadgeCenterYFromTop);
            var badgeImage = badgeGo.GetComponent<Image>();
            badgeImage.type = Image.Type.Simple;
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;

            // Pedestal glow, BEHIND the portrait (added first) so the
            // character's feet appear to stand on it.
            var pedestalGo = new GameObject("Pedestal", typeof(Image));
            pedestalGo.transform.SetParent(filledRoot.transform, false);
            var pedestalRect = pedestalGo.GetComponent<RectTransform>();
            pedestalRect.anchorMin = new Vector2(0.5f, 1f);
            pedestalRect.anchorMax = new Vector2(0.5f, 1f);
            pedestalRect.pivot = new Vector2(0.5f, 0.5f);
            pedestalRect.sizeDelta = new Vector2(PedestalSize, PedestalSize);
            pedestalRect.anchoredPosition = new Vector2(0f, PedestalCenterYFromTop);
            var pedestalImage = pedestalGo.GetComponent<Image>();
            pedestalImage.sprite = pedestalSprite;
            pedestalImage.type = Image.Type.Simple;
            pedestalImage.preserveAspect = true;
            pedestalImage.raycastTarget = false;

            var portraitGo = new GameObject("Portrait", typeof(Image));
            portraitGo.transform.SetParent(filledRoot.transform, false);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 1f);
            portraitRect.anchorMax = new Vector2(0.5f, 1f);
            portraitRect.pivot = new Vector2(0.5f, 1f);
            portraitRect.sizeDelta = new Vector2(CardPortraitHeight, CardPortraitHeight);
            portraitRect.anchoredPosition = new Vector2(0f, -(CardTopZoneHeight + CardPortraitTopGap));
            var portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.type = Image.Type.Simple;
            portraitImage.preserveAspect = true;

            // Footer stack (card-local space, from the bottom fixed-zone's
            // own top edge at -(CardHeight/2-CardBottomZoneHeight) down to
            // the card's bottom edge - see class doc comment for the exact
            // budget): pad(2) -> NameplateBar(44) -> gap(4) ->
            // Select/Delete buttons(40), landing exactly on the bottom edge.
            float footerTop = -(CardHeight / 2f - CardBottomZoneHeight);
            float nameplateTop = footerTop - FooterTopPad;
            float nameplateCenterY = nameplateTop - CardNameplateHeight / 2f;
            float nameplateBottom = nameplateTop - CardNameplateHeight;
            float buttonTop = nameplateBottom - NameplateButtonGap;
            float buttonCenterY = buttonTop - CardButtonHeight / 2f;

            Image nameplate = CharacterFlowUiScaffold.BuildSlicedPanel(filledRoot, nameplateSprite, "Nameplate", new Vector2(0f, nameplateCenterY), new Vector2(CardWidth - 24f, CardNameplateHeight));
            nameplate.raycastTarget = false;

            // 2026-09-15 (D3 fix, still valid): fontSize was 24, the ONLY Text
            // component in the entire game using that exact size - a legacy
            // uGUI + dynamic OTF font bug renders zero glyphs the first time
            // a brand-new size is requested at runtime. Kept at
            // already-established sizes (22/16) here too.
            //
            // 2026-09-16 (premium rebuild, real bug found via screenshot -
            // NOT the font-size bug above): the tight 44-unit nameplate
            // footer budget made the first draft's box heights (20/16)
            // SMALLER than fontSize 22/16 actually need (~1.2x line height,
            // so ~26/~19) - with the default Text.verticalOverflow=Truncate,
            // this clipped the glyphs to fully invisible instead of just
            // trimming descenders. Heights grown past each font's own line
            // height (26/20) and verticalOverflow explicitly set to Overflow
            // as a second guard (the two lines sit close together in a small
            // pill, so "may bleed a couple px past the box" is harmless here -
            // nothing else occupies that space) - confirmed by re-capturing
            // v2_select.png and reading the pixels, not by theory alone.
            Text nameText = CharacterFlowUiScaffold.BuildLabel(filledRoot, "NameText", new Vector2(0f, nameplateCenterY + 11f), new Vector2(CardWidth - 32f, 26f), string.Empty, fontSize: 22);
            Text classLevelText = CharacterFlowUiScaffold.BuildLabel(filledRoot, "ClassLevelText", new Vector2(0f, nameplateCenterY - 13f), new Vector2(CardWidth - 32f, 20f), string.Empty, fontSize: 16);
            nameText.verticalOverflow = VerticalWrapMode.Overflow;
            classLevelText.verticalOverflow = VerticalWrapMode.Overflow;
            // 2026-09-16 (premium rebuild): background flipped from beige to
            // dark navy - bright sapphire-tinted text now, not the old dark
            // brownish-gray (#4a4038) that was tuned for the beige panel.
            classLevelText.color = new Color(0.72f, 0.83f, 1f, 1f);

            float buttonCenterX = (CardButtonWidth + CardButtonGap) / 2f;
            Button selectButton = CharacterFlowUiScaffold.BuildLabeledButton(filledRoot, selectButtonSprite, "SelectButton", new Vector2(-buttonCenterX, buttonCenterY), new Vector2(CardButtonWidth, CardButtonHeight), "선택", fontSize: 16);
            Button deleteButton = CharacterFlowUiScaffold.BuildLabeledButton(filledRoot, deleteButtonSprite, "DeleteButton", new Vector2(buttonCenterX, buttonCenterY), new Vector2(CardButtonWidth, CardButtonHeight), "삭제", fontSize: 16);

            VerifyButtonsInsideCard(index, buttonCenterX, buttonCenterY);

            GameObject emptyRoot = BuildChildRoot(cardGo, "Empty");
            Button createButton = CharacterFlowUiScaffold.BuildLabeledButton(emptyRoot, createButtonSprite, "CreateButton", Vector2.zero, new Vector2(150f, 64f), "+ 생성", fontSize: 22);
            // ButtonCreateV2's fill is a light champagne-gold (top_color
            // (240,223,174) in build_title_kit_v2.py) - the default white
            // label text (BuildLabeledButton) would be near-illegible there,
            // so it's switched to the same dark warm-brown the generator
            // itself uses for that button's own chevron accent (chevron_color
            // (40,32,16,210)).
            createButton.GetComponentInChildren<Text>().color = new Color(0.157f, 0.125f, 0.063f, 1f);

            return new CharacterSlotCardView
            {
                cardBackground = card,
                filledRoot = filledRoot,
                emptyRoot = emptyRoot,
                classBadgeImage = badgeImage,
                portraitImage = portraitImage,
                nameText = nameText,
                classLevelText = classLevelText,
                selectButton = selectButton,
                deleteButton = deleteButton,
                createButton = createButton,
            };
        }

        // 2026-09-16 (premium rebuild): the D3-era check inset ALL 4 edges by
        // CardButtonEdgeInset(48), which assumed buttons floated well above
        // the card's bottom edge. The new footer design deliberately lands
        // Select/Delete flush with the card's own bottom edge, inside the
        // frame's fixed (non-stretching) bottom zone (CardBottomZoneHeight) -
        // so the Y check is against that zone's actual height instead of a
        // symmetric inset, while X keeps the original left/right inset
        // (still clearing the frame's side border/corner radius). Card and
        // button rects are both computed relative to the card's own local
        // center (0,0) here, which is equivalent to canvas space shifted by
        // -centerX - fine for a pure-containment check since it doesn't
        // depend on the card's absolute canvas position.
        private static void VerifyButtonsInsideCard(int index, float buttonCenterX, float buttonCenterY)
        {
            var cardLocalRect = new Rect(-CardWidth / 2f, -CardHeight / 2f, CardWidth, CardHeight);
            var innerSafeRect = new Rect(
                cardLocalRect.xMin + CardButtonEdgeInset,
                cardLocalRect.yMin,
                cardLocalRect.width - 2f * CardButtonEdgeInset,
                CardBottomZoneHeight);

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
