using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sapphire.Presentation.UI;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Builds the right-side Odin-style menu panel (2026-09-16, Phase 3,
    /// REMEDIATION_PLAN.md D2(b)): replaces the old flat 7-item text-list
    /// MainMenuPanel (which violated docs/planning/01_PRODUCT.md line 52 -
    /// "메뉴는 가방/장비/퀘스트/설정 4개만 실제 제공, 미구현 레이드 버튼
    /// 금지") with a sectioned icon grid built entirely from MenuCatalog's
    /// data, so adding/removing/relabeling an item never requires touching
    /// this class - only Presentation/UI/MenuCatalog.cs. Split out of
    /// <see cref="VillageHubUiBuilder"/> (same "don't let one file grow past
    /// ~500 lines" split this codebase already applies between
    /// SapphireSceneBuilder / ArtImportConfigurator / VillageHubTerrainBuilder /
    /// VillageHubUiBuilder) - this file owns only the menu panel, the rest of
    /// the HUD (gauges, skill fan, movement pad, region banner) stays in
    /// VillageHubUiBuilder.
    /// </summary>
    internal static class VillageHubMenuBuilder
    {
        private const float OdinPanelWidth = 400f;
        // 2026-09-16 (F6.1 fix): the MainMenuButton (see Build below) is
        // anchored top-right, anchoredPosition (-20,-20), sizeDelta (150,72) -
        // its bottom edge sits 20+72=92 units below the canvas top. The old
        // 24-unit top margin put the panel's top edge well inside that
        // button's rect, so the open panel visually covered/clipped the
        // button's own "메뉴" label (confirmed in the orchestrator's
        // reference screenshot). 92 + 12px clearance = 104.
        private const float OdinPanelTopMargin = 104f;
        private const float OdinPanelBottomMargin = 20f;
        // 2026-09-16 (F6.4 fix): raised from 36 to 40 alongside the
        // MenuSectionHeader crop/border recompute in HudArtImportConfigurator
        // (see that file's ConfigureMenuSectionHeader for the measurement) -
        // headers now render with less border-eaten space so the section
        // title text sits centered in the header band instead of overlapping
        // its top edge.
        private const float OdinHeaderHeight = 40f;
        private const float OdinHeaderToItemsGap = 12f;
        // 2026-09-15 (D1 fix): shrunk from 104 to the item's actual content
        // height (icon 56 + 4 gap + label 18 = 78, see BuildOdinMenuItem) - at
        // 104 every row reserved 26px of unused padding for a second grid row
        // that MenuCatalog's current data never produces (every section is
        // <=4 items, i.e. exactly one OdinColumns=4-wide row), and that slack
        // was what let the grid's total content height run past the panel's
        // actual available space and collide with the character-select
        // footer button (see BuildCharacterSelectButton + the
        // VerifyFooterClearance check at the end of Build below, which is
        // what actually guards this now instead of eyeballing it again).
        private const float OdinItemRowHeight = 78f;
        // 2026-09-15 (D1 fix): 22 -> 8, same reason as OdinItemRowHeight - see
        // VerifyFooterClearance for the arithmetic this is tuned against.
        private const float OdinSectionGap = 8f;
        private const float OdinIconSize = 56f;
        // 2026-09-15 (D1 fix): the footer button's own height, pulled out of
        // BuildCharacterSelectButton as a named constant so
        // VerifyFooterClearance (build-time overlap guard) can reference the
        // exact same number instead of a duplicated magic 56.
        private const float OdinFooterButtonHeight = 56f;
        // Minimum clearance (canvas units) required between the last
        // section's content (icon+label) and the footer button's top edge -
        // task spec: "설정 아이콘+라벨과 footer 버튼 사이 최소 12px 간격".
        private const float OdinFooterMinGap = 12f;
        // Matches CharacterFlowUiScaffold/VillageHubUiBuilder's
        // CanvasScaler.referenceResolution - VerifyFooterClearance works in
        // these reference-resolution canvas units rather than reading a live
        // RectTransform.rect, because batchmode scene builds run before any
        // GameView/screen exists and RectTransform.rect can't be trusted yet
        // (same reasoning LayoutOverlapGuard documents for CharacterCreate/
        // CharacterSelect).
        private const float ReferenceCanvasHeight = 720f;

        // 2026-09-15 (S2 fix): the previous flat 24-unit OdinContentMargin
        // put grid content inside the panel's own gold 9-slice border, so
        // column 1 icons touched the left frame and the column 4 label
        // ("캐릭터정보", the longest in MenuCatalog) clipped against the
        // right frame (orchestrator screenshot final2_1280_menu.png).
        // MenuPanelOdin's border must be converted from source-texture
        // pixels to canvas units the same way Image.Type.Sliced does at
        // runtime: Image.pixelsPerUnit = sprite.pixelsPerUnit /
        // canvas.referencePixelsPerUnit (see the *100 note in
        // ArtImportConfigurator.ConfigureUiFrames), so
        // borderCanvasUnits = borderPx / (spritePixelsPerUnit / 100).
        // Values below (border=84px, pixelsPerUnit=100*793/400) are the
        // exact ones HudArtImportConfigurator.ConfigureMenuPanelOdin imports
        // MenuPanelOdin.png with - kept in sync as named constants instead
        // of a duplicated magic number.
        private const float MenuPanelOdinLeftRightBorderPx = 84f;
        private const float MenuPanelOdinSpritePixelsPerUnit = 100f * 793f / 400f;
        private const float OdinContentMarginClearance = 14f;
        private static readonly float OdinContentMargin =
            MenuPanelOdinLeftRightBorderPx / (MenuPanelOdinSpritePixelsPerUnit / 100f) + OdinContentMarginClearance;
        // code-reviewer flagged a doc conflict: docs/REMEDIATION_PLAN.md line
        // 61 says "5열 아이콘 그리드" (5 columns), but the session's task
        // instructions explicitly specify "a 4-column grid of items (cell
        // width = (400 - 2*margin) / 4 ...)". Per this task's own conflict-
        // resolution rule (the task instructions are the latest, most specific
        // authority; REMEDIATION_PLAN.md may carry stale nuance), 4 is what's
        // implemented here - docs/DECISIONS.md's 2026-09-16 entry records this
        // as a known, deliberate conflict rather than an oversight.
        private const int OdinColumns = 4;

        internal static void Build(GameObject canvasGo, SimpleMessagePanel messagePanel)
        {
            Sprite openButtonSprite = VillageHubUiBuilder.LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MenuButtonGold.png");
            Sprite panelSprite = VillageHubUiBuilder.LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MenuPanelOdin.png");
            Sprite headerSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/MenuSectionHeader.png", "MenuSectionHeader");
            Sprite lockSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/MenuLockBadge.png", "MenuLockBadge");

            var openGo = new GameObject("MainMenuButton", typeof(Image), typeof(Button));
            openGo.transform.SetParent(canvasGo.transform, false);
            var openRect = openGo.GetComponent<RectTransform>();
            openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 1f);
            openRect.pivot = new Vector2(1f, 1f);
            openRect.sizeDelta = new Vector2(150f, 72f);
            openRect.anchoredPosition = new Vector2(-20f, -20f);
            var openImage = openGo.GetComponent<Image>();
            openImage.sprite = openButtonSprite;
            openImage.type = Image.Type.Sliced;
            AddButtonLabel(openGo, "메뉴", 25);

            // 2026-09-16 (F6.2 fix): full-screen dim backdrop + click-blocker,
            // toggled together with the panel (both live under one
            // "MenuOverlay" wrapper - see Configure below). Built BEFORE the
            // panel so it sits earlier in sibling order and renders behind it,
            // while still being the frontmost thing over the HUD/gameplay
            // behind it (raycastTarget=true stops clicks from leaking through
            // to the world/skill fan while the menu is open).
            var overlayGo = new GameObject("MenuOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(canvasGo.transform, false);
            var overlayRect = overlayGo.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var backdropGo = new GameObject("MenuBackdrop", typeof(Image), typeof(Button));
            backdropGo.transform.SetParent(overlayGo.transform, false);
            var backdropRect = backdropGo.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            var backdropImage = backdropGo.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.45f);
            backdropImage.raycastTarget = true;
            Button backdropButton = backdropGo.GetComponent<Button>();

            var panelGo = new GameObject("MainMenuPanel", typeof(Image));
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = new Vector2(-20f - OdinPanelWidth, OdinPanelBottomMargin);
            panelRect.offsetMax = new Vector2(-20f, -OdinPanelTopMargin);
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;

            var allButtons = new System.Collections.Generic.List<Button>();
            var allLabels = new System.Collections.Generic.List<string>();
            var allAvailable = new System.Collections.Generic.List<bool>();

            float contentWidth = OdinPanelWidth - 2f * OdinContentMargin;
            float cellWidth = contentWidth / OdinColumns;
            float cursorY = -OdinContentMargin;
            // Tracks the bottom edge (distance below panel top, negative) of
            // the last section's item row - i.e. where the grid content
            // actually ends, ignoring the trailing OdinSectionGap the loop
            // below adds after every section (including the last one, where
            // it's never used for anything). VerifyFooterClearance needs the
            // real content-end, not that unused trailing gap.
            float lastContentBottomY = cursorY;

            foreach (MenuSectionDefinition section in MenuCatalog.Sections)
            {
                BuildOdinSectionHeader(panelGo, headerSprite, section.Title, cursorY, contentWidth);
                cursorY -= OdinHeaderHeight + OdinHeaderToItemsGap;

                int rows = Mathf.CeilToInt(section.Items.Length / (float)OdinColumns);
                for (int r = 0; r < rows; r++)
                {
                    float rowY = cursorY - r * OdinItemRowHeight;
                    for (int c = 0; c < OdinColumns; c++)
                    {
                        int index = r * OdinColumns + c;
                        if (index >= section.Items.Length)
                        {
                            break;
                        }

                        MenuItemDefinition item = section.Items[index];
                        float cellCenterX = OdinContentMargin + c * cellWidth + cellWidth * 0.5f;
                        Button itemButton = BuildOdinMenuItem(panelGo, item, lockSprite, new Vector2(cellCenterX, rowY), cellWidth);
                        allButtons.Add(itemButton);
                        allLabels.Add(item.Label);
                        allAvailable.Add(item.IsAvailable);
                    }
                }

                lastContentBottomY = cursorY - rows * OdinItemRowHeight;
                cursorY = lastContentBottomY - OdinSectionGap;
            }

            VerifyFooterClearance(lastContentBottomY);

            // Character-flow slice, task requirement: one return path from the
            // village back to CharacterSelect, somewhere in the menu panel.
            // A standalone footer button (not a MenuCatalog grid item) wired
            // to load the scene directly, rather than routing through
            // MainMenuPanel.Select's generic "coming soon" placeholder - so
            // this doesn't touch MainMenuPanel.cs/MenuCatalog.cs or any of
            // the existing 8 menu items' behavior at all.
            //
            // 2026-09-15 (D1 fix): this used to be positioned purely by its
            // own anchoredPosition (from-bottom) with zero awareness of where
            // the grid content above ended (from-top) - the two coordinate
            // systems never got compared, so nothing caught them overlapping
            // (orchestrator screenshot flow_village_warrior_menu.png: this
            // button visually covered the "시스템" section's 설정 icon
            // label). VerifyFooterClearance above now throws at build time if
            // they would.
            BuildCharacterSelectButton(panelGo, openButtonSprite);

            var controller = canvasGo.AddComponent<MainMenuPanel>();
            controller.Configure(overlayGo, openGo.GetComponent<Button>(), allButtons.ToArray(), allLabels.ToArray(), allAvailable.ToArray(), messagePanel);
            backdropButton.onClick.AddListener(controller.Close);
            overlayGo.SetActive(false);
        }

        // Build-time overlap guard (D4 spec: "기존 VerifyNoOverlap류 빌드타임
        // 검증이 있으면 footer까지 포함하도록 확장" - none existed for this
        // panel yet, so this is the new one). Works in the same
        // reference-resolution canvas units every position/margin constant
        // above is defined in, not a live RectTransform.rect read - see
        // ReferenceCanvasHeight's doc comment for why.
        private static void VerifyFooterClearance(float lastContentBottomY)
        {
            float panelHeight = ReferenceCanvasHeight - OdinPanelTopMargin - OdinPanelBottomMargin;
            float contentBottomFromPanelBottom = panelHeight - (-lastContentBottomY);
            float footerTopFromPanelBottom = OdinContentMargin + OdinFooterButtonHeight;
            float clearance = contentBottomFromPanelBottom - footerTopFromPanelBottom;

            if (clearance < OdinFooterMinGap)
            {
                throw new System.Exception(
                    $"VillageHub menu: only {clearance:F1}px clearance between the last section's " +
                    $"content and the character-select footer button (need >= {OdinFooterMinGap}px). " +
                    "MenuCatalog grew, or OdinItemRowHeight/OdinHeaderHeight/OdinSectionGap shrank the " +
                    "available margin - reduce item count per section, shrink those constants further, " +
                    "or reduce OdinPanelTopMargin/OdinPanelBottomMargin to grow the panel.");
            }
        }

        private static void BuildCharacterSelectButton(GameObject panelGo, Sprite buttonSprite)
        {
            var buttonGo = new GameObject("CharacterSelectButton", typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panelGo.transform, false);
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(OdinPanelWidth - 2f * OdinContentMargin, OdinFooterButtonHeight);
            rect.anchoredPosition = new Vector2(0f, OdinContentMargin);
            var image = buttonGo.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            Button button = buttonGo.GetComponent<Button>();
            button.onClick.AddListener(() => SceneManager.LoadScene("CharacterSelect"));

            AddButtonLabel(buttonGo, "캐릭터 선택으로", 20);
        }

        private static void BuildOdinSectionHeader(GameObject panelGo, Sprite headerSprite, string title, float topY, float contentWidth)
        {
            var headerGo = new GameObject("Section_" + title, typeof(Image));
            headerGo.transform.SetParent(panelGo.transform, false);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.sizeDelta = new Vector2(contentWidth, OdinHeaderHeight);
            headerRect.anchoredPosition = new Vector2(OdinContentMargin, topY);
            var headerImage = headerGo.GetComponent<Image>();
            headerImage.sprite = headerSprite;
            headerImage.type = Image.Type.Sliced;
            headerImage.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(headerGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = VillageHubUiBuilder.LoadKoreanFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 20;
            text.text = title;
            text.raycastTarget = false;
        }

        // Locked items: gray-tinted icon + a MenuLockBadge overlay at the
        // icon's bottom-right, sized to 40% of the icon, and
        // Button.interactable=false (spec item 4). Available items keep the
        // icon at full color and stay interactable - MainMenuPanel wires their
        // click behavior.
        private static Button BuildOdinMenuItem(GameObject panelGo, MenuItemDefinition item, Sprite lockSprite, Vector2 anchoredPosition, float cellWidth)
        {
            // RectTransform is explicit here (unlike buttons elsewhere in this
            // codebase that get one implicitly via an Image's
            // [RequireComponent(typeof(RectTransform))]) - this item root has
            // no Image of its own (its Icon/Label children each have their
            // own), so Button/Selectable alone would leave it with a plain
            // Transform and crash the anchoredPosition/sizeDelta assignments
            // below.
            var itemGo = new GameObject("MenuItem_" + item.Id, typeof(RectTransform), typeof(Button));
            itemGo.transform.SetParent(panelGo.transform, false);
            var itemRect = itemGo.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(0f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(cellWidth, OdinItemRowHeight);
            itemRect.anchoredPosition = anchoredPosition;
            var button = itemGo.GetComponent<Button>();

            Sprite iconSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/MenuIconsSet.png", item.IconSpriteName);
            var iconGo = new GameObject("Icon", typeof(Image));
            iconGo.transform.SetParent(itemGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.sizeDelta = new Vector2(OdinIconSize, OdinIconSize);
            iconRect.anchoredPosition = new Vector2(0f, 0f);
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(Text));
            labelGo.transform.SetParent(itemGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            // 2026-09-16 (F6.5 fix): width explicitly cellWidth-4 (not a
            // stretch anchor spanning the full cell) so resizeTextForBestFit
            // has a known, slightly-inset box to shrink into instead of
            // clipping against the cell's exact edge - "캐릭터정보" (the
            // longest label in MenuCatalog) was overflowing its cell at the
            // previous fixed fontSize=14.
            //
            // 2026-09-15 (S2 fix): F6.5 above still clipped "캐릭터정보" in
            // the orchestrator's screenshot (final2_1280_menu.png) because
            // horizontalOverflow was left at its Text default
            // (HorizontalWrapMode.Overflow) - resizeTextForBestFit does not
            // shrink the font against a box whose horizontal overflow mode is
            // Overflow, it only clamps vertically. Explicit Wrap horizontal +
            // Truncate vertical gives BestFit an actual box to fit text into.
            // Rect narrowed to cellWidth-6/height 18 (was -4/24) per this
            // task's spec, min/max font size correspondingly reduced to keep
            // that inset box from clipping.
            labelRect.sizeDelta = new Vector2(cellWidth - 6f, 18f);
            labelRect.anchoredPosition = new Vector2(0f, -OdinIconSize - 4f);
            var label = labelGo.GetComponent<Text>();
            label.font = VillageHubUiBuilder.LoadKoreanFont();
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 13;
            label.text = item.Label;
            label.raycastTarget = false;

            if (!item.IsAvailable)
            {
                iconImage.color = new Color(0.45f, 0.45f, 0.5f, 1f);
                label.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                button.interactable = false;

                var lockGo = new GameObject("LockBadge", typeof(Image));
                lockGo.transform.SetParent(iconGo.transform, false);
                var lockRect = lockGo.GetComponent<RectTransform>();
                lockRect.anchorMin = new Vector2(1f, 0f);
                lockRect.anchorMax = new Vector2(1f, 0f);
                lockRect.pivot = new Vector2(1f, 0f);
                float lockSize = OdinIconSize * 0.4f;
                lockRect.sizeDelta = new Vector2(lockSize, lockSize);
                lockRect.anchoredPosition = Vector2.zero;
                var lockImage = lockGo.GetComponent<Image>();
                lockImage.sprite = lockSprite;
                lockImage.preserveAspect = true;
                lockImage.raycastTarget = false;
            }
            else
            {
                label.color = Color.white;
            }

            return button;
        }

        private static void AddButtonLabel(GameObject parent, string value, int fontSize)
        {
            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(parent.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = VillageHubUiBuilder.LoadKoreanFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = fontSize;
            text.text = value;
            text.raycastTarget = false;
        }
    }
}
