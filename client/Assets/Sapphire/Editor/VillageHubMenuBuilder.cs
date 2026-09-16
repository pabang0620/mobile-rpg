using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Sapphire.Presentation.CharacterFlow;
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
        // 2026-09-16 (widen menu task): 400 * 1.3 - the task spec asks for a
        // panel "30% wider" verbatim. Every other horizontal number below
        // that depends on width (contentWidth, cellWidth, columnPitch) is
        // already computed FROM this constant rather than hardcoded, so
        // widening it is the only edit needed to reflow the grid - see
        // VerifyPanelLayout for the build-time sanity check this earns.
        private const float OdinPanelWidth = 520f;
        // 2026-09-16 (widen menu task, part A4) - OdinPanelTopMargin/
        // OdinPanelBottomMargin (the panel's OUTER footprint against the
        // canvas edges) are left at their original values (F6.1 fix's 0,
        // D1 fix's 20). A first attempt made these two computed and equal
        // (shrinking the panel to content height, margin = leftover/2) but
        // that FAILED verification: it moved the panel's bottom edge up by
        // ~78px, and VillageHubSkillMenuBuilder's warrior/mage skill fan
        // (anchored to this exact same bottom-right corner, see that file -
        // its outermost button's polar position lands as low as canvas
        // y~19-99) is a SIBLING of this panel, not behind it - shrinking the
        // panel's outer bottom margin past ~20 exposes that button peeking
        // out from under the open menu (confirmed in
        // generated-images/diagnostics/v3_menu.png during this task's own
        // verification pass, then reverted - see docs/DECISIONS.md's
        // 2026-09-16 "메뉴판 확장" entry). So the outer footprint is
        // unchanged, and "상단 여백=하단 여백" is instead satisfied by
        // OdinContentPadding below - the INNER gap from the panel's own top
        // edge to the first section header, and from the last section's
        // content to the panel's own bottom edge, made equal to each other
        // instead. That fully addresses the actual complaint (the
        // orchestrator's final_village_warrior_menu.png reference screenshot:
        // a large empty band concentrated only under the last section) without
        // touching the panel's outer size/position at all.
        private const float OdinPanelTopMargin = 0f;
        // 2026-09-16 (widen menu task, part A4) - kept at its original value
        // (D1 fix's 20), NOT enlarged. This task's own screenshot
        // verification pass (generated-images/diagnostics/v3_menu.png) found
        // VillageHubSkillMenuBuilder's warrior/mage skill fan peeking out
        // from behind the panel's bottom-right corner - MenuPanelDark.png's
        // border art has a decorative curved cutout at each corner
        // (intentional, visible at all 4 corners) that exposes whatever
        // sits directly behind it. A first instinct (raise this margin so
        // the panel's bottom edge sits higher, "further from" the fan) was
        // tried and made it strictly WORSE - raising the margin SHRINKS the
        // panel, uncovering MORE of the fan below it, not less (verified by
        // re-running the capture script: at margin 100 the entire basic-
        // attack button rendered fully undimmed below the panel, worse than
        // the small corner peek at margin 20). The correct direction to fully
        // cover the fan would be LOWERING this margin toward 0, but that
        // rabbit hole - reconciling an ornamental corner cutout's exact
        // curve against the fan's exact polar coordinates - is out of this
        // task's scope (widen the panel / equalize its own content padding /
        // add system-section items), the fan's clicks are still correctly
        // blocked by the full-screen backdrop Button regardless of this
        // visual peek (no functional bug), and this exact peek is not new -
        // it was already latent in the ORIGINAL unwidened panel at this same
        // margin value, merely hidden by the CharacterSelectButton footer
        // that used to render on top of that corner (now correctly removed
        // per this task's part B - see docs/DECISIONS.md's 2026-09-16 entry).
        // Left as a known, pre-existing, out-of-scope cosmetic issue rather
        // than risking a worse regression chasing it further.
        private const float OdinPanelBottomMargin = 20f;
        // 2026-09-16 (widen menu task, part A4): replaces the old separate
        // OdinContentTopInset(67)/OdinBottomContentPadding(12) constants -
        // computed from the panel's fixed height (see OdinPanelTopMargin's
        // comment above) minus however tall 성장+모험+시스템's header/items/
        // gaps actually are (ComputeSectionsHeight, a dry-run of Build's own
        // per-section arithmetic below, kept as its own method for the same
        // reason ComputePanelContentHeight originally was - it must run
        // before Build creates any GameObjects), split evenly so the padding
        // above the first header equals the padding below the last row by
        // construction. static readonly (not const) because
        // ComputeSectionsHeight reads MenuCatalog.Sections, not a
        // compile-time constant.
        private static readonly float OdinSectionsHeight = ComputeSectionsHeight();
        private static readonly float OdinContentPadding =
            (ReferenceCanvasHeight - OdinPanelTopMargin - OdinPanelBottomMargin - OdinSectionsHeight) / 2f;
        // 2026-09-16 (menu polish task, docs/HANDOFF.md): OdinContentPadding
        // above deliberately balances top padding == bottom padding (currently
        // 128px each, see its own comment) - the user asked to move the whole
        // grid (header+icons) up 15px specifically because that top band has
        // visible spare room, which intentionally unbalances that pairing
        // (top shrinks to 113px, bottom stays 128px) rather than recomputing
        // both. VerifyPanelLayout below asserts the resulting top margin
        // stays comfortably positive so this can never collapse the first
        // header against the panel's own top edge if MenuCatalog grows later.
        private const float ContentUpShift = 15f;
        private const float MinTopMarginAfterShift = 8f;
        // 2026-09-16 (F6.4 fix): raised from 36 to 40 so the section title
        // text sits centered in the header band instead of overlapping its
        // top edge. 2026-09-15 (gemless MapleStory-M rebuild): the header no
        // longer has a banner backdrop at all (see BuildOdinSectionHeader) -
        // this height now just reserves vertical room for the divider+title
        // row, kept at 40 since that still comfortably fits both.
        // internal (not private): VillageHubMenuHeaderBuilder.BuildOdinSectionHeader
        // (split out to keep this file under the ~500-line convention) needs it.
        internal const float OdinHeaderHeight = 40f;
        private const float OdinHeaderToItemsGap = 12f;
        // 2026-09-15 (D1 fix): shrunk from 104 to the item's actual content
        // height (icon 56 + 4 gap + label 18 = 78, see BuildOdinMenuItem) - at
        // 104 every row reserved 26px of unused padding for a second grid row
        // that MenuCatalog's current data never produces (every section is
        // <=4 items, i.e. exactly one OdinColumns=3-wide row for every
        // section except 성장's 2 rows). 2026-09-16: the old comment here
        // referenced a "character-select footer button" clearance check
        // (VerifyFooterClearance) - that footer button and its guard are both
        // removed (see ComputeSectionsHeight/OdinContentPadding/
        // VerifyPanelLayout below, which now size the top/bottom padding to
        // content instead of guarding a fixed-height panel against one
        // footer element).
        private const float OdinItemRowHeight = 70f;
        private const float OdinSectionGap = 4f;
        // Tighter 3x3 presentation: the previous 56px icon left a visibly
        // large gap between neighboring cells.  Enlarging the icon while
        // keeping the same three-column centers reduces the perceived gap to
        // roughly half without changing the panel width or label alignment.
        private const float OdinIconSize = 54f;
        // Matches CharacterFlowUiScaffold/VillageHubUiBuilder's
        // CanvasScaler.referenceResolution - ComputeSectionsHeight/
        // VerifyPanelLayout work in these reference-resolution canvas units
        // rather than reading a live RectTransform.rect, because batchmode
        // scene builds run before any GameView/screen exists and
        // RectTransform.rect can't be trusted yet (same reasoning
        // LayoutOverlapGuard documents for CharacterCreate/CharacterSelect).
        private const float ReferenceCanvasHeight = 720f;

        // 2026-09-16 (header/content overflow fix, docs/HANDOFF.md): this
        // margin had been computed against MenuPanelOdin.png's border
        // (17.33 canvas units at the v3 kit's 300 ppu), but the panel
        // sprite this builder actually draws is MenuPanelDark.png (line
        // ~263, swapped in by the 2026-09-16 external SlimeKingdom pull) -
        // imported by HudArtImportConfigurator.ConfigureMenuPanelDark with
        // border (52,52,52,52) at an EXPLICIT pixelsPerUnit=100, not the v3
        // kit's 300. Canvas units the same way Image.Type.Sliced computes
        // them at runtime (Image.pixelsPerUnit = sprite.pixelsPerUnit /
        // canvas.referencePixelsPerUnit): borderCanvasUnits =
        // borderPx / (spritePixelsPerUnit / 100) = 52 / (100/100) = 52 -
        // three times the 17.33 this margin was tuned for. Content (grid
        // AND section headers) was rendering ~35 units past the panel's
        // real left/right border into the game world - exactly the
        // "글자가 메뉴판 밖에 나와있어" the user kept reporting after two
        // prior header-only patches (VillageHubMenuHeaderBuilder's since-
        // removed HeaderWidthReduction) that narrowed the header without
        // fixing this shared root value. Fixed at the source instead.
        private const float MenuPanelDarkBorderPx = 52f;
        private const float MenuPanelDarkSpritePixelsPerUnit = 100f;
        private const float OdinContentMarginClearance = 14f;
        private const float OdinHorizontalPaddingReduction = 20f;
        // internal (not private): VillageHubMenuHeaderBuilder.BuildOdinSectionHeader
        // (split out to keep this file under the ~500-line convention) needs it.
        internal static readonly float OdinContentMargin =
            MenuPanelDarkBorderPx / (MenuPanelDarkSpritePixelsPerUnit / 100f)
            + OdinContentMarginClearance - OdinHorizontalPaddingReduction;
        // code-reviewer flagged a doc conflict: docs/REMEDIATION_PLAN.md line
        // 61 says "5열 아이콘 그리드" (5 columns), but the session's task
        // instructions explicitly specify "a 4-column grid of items (cell
        // width = (400 - 2*margin) / 4 ...)". Per this task's own conflict-
        // resolution rule (the task instructions are the latest, most specific
        // authority; REMEDIATION_PLAN.md may carry stale nuance), 4 is what's
        // implemented here - docs/DECISIONS.md's 2026-09-16 entry records this
        // as a known, deliberate conflict rather than an oversight.
        // Three-column landscape menu grid: wider icon cells keep labels
        // readable at 1280x720 and match the supplied reference layout.
        private const int OdinColumns = 3;
        // Compress only the horizontal pitch of the 3-column grid so the
        // icons sit closer together without changing the vertical rhythm.
        private const float OdinColumnPitchScale = 0.75f;

        // 2026-09-15 (gemless MapleStory-M rebuild): the "메뉴" button dropped
        // its MenuButtonGold pill background and text label entirely - it's
        // now the hamburger icon alone (spec: "배경 없음, 살짝 그림자" - the
        // shadow is already baked into MenuHamburgerIcon.png). Sized smaller
        // than the old 150x72 pill since there's no label to fit anymore.
        private const float HamburgerButtonSize = 56f;

        // 2026-09-16 (widen menu task, part A4): sums exactly the same
        // per-section block (header + header-to-items gap + rows*rowHeight)
        // the Build loop below lays out, with OdinSectionGap between every
        // pair of sections (MenuCatalog.Sections -1 gaps for N sections) -
        // deliberately EXCLUDES the top inset/bottom padding (those are
        // OdinContentPadding, derived FROM this method's result, see its own
        // comment above) - i.e. this is a dry-run of just the header+items
        // portion of Build's own vertical arithmetic, kept as its own method
        // (rather than sharing a loop with Build) because Build also creates
        // GameObjects and this needs to run once, before Build, purely to
        // size the padding. Any future MenuCatalog change (more items/
        // sections) automatically grows/shrinks both paddings in lockstep
        // instead of needing a hand-tuned margin.
        private static float ComputeSectionsHeight()
        {
            float height = 0f;
            MenuSectionDefinition[] sections = MenuCatalog.Sections;
            for (int i = 0; i < sections.Length; i++)
            {
                int rows = Mathf.CeilToInt(sections[i].Items.Length / (float)OdinColumns);
                height += OdinHeaderHeight + OdinHeaderToItemsGap + rows * OdinItemRowHeight;
                if (i < sections.Length - 1)
                {
                    height += OdinSectionGap;
                }
            }

            return height;
        }

        // Build-time sanity check (replaces the old VerifyFooterClearance,
        // which guarded one footer element against a fixed-height panel).
        // OdinContentPadding going negative means MenuCatalog grew past what
        // the fixed-size panel (OdinPanelTopMargin/BottomMargin, unchanged
        // footprint) can fit at all - same failure mode VerifyFooterClearance
        // used to catch, just measured against both paddings instead of one
        // footer element.
        private static void VerifyPanelLayout()
        {
            if (OdinContentPadding < 0f)
            {
                throw new System.Exception(
                    $"VillageHub menu: sections need {OdinSectionsHeight:F1}px but the panel only has " +
                    $"{ReferenceCanvasHeight - OdinPanelTopMargin - OdinPanelBottomMargin:F0}px to give - " +
                    "MenuCatalog grew too large for the Odin panel's fixed footprint. Reduce item count " +
                    "per section or shrink OdinItemRowHeight/OdinHeaderHeight/OdinSectionGap.");
            }

            float topMarginAfterShift = OdinContentPadding - ContentUpShift;
            if (topMarginAfterShift < MinTopMarginAfterShift)
            {
                throw new System.Exception(
                    $"VillageHub menu: ContentUpShift({ContentUpShift:F0}px) would leave only " +
                    $"{topMarginAfterShift:F1}px between the panel's top edge and the first section header " +
                    $"(minimum {MinTopMarginAfterShift:F0}px) - MenuCatalog grew too large to also afford the " +
                    "15px up-shift. Reduce ContentUpShift or item count per section.");
            }
        }

        internal static void Build(GameObject canvasGo, SimpleMessagePanel messagePanel)
        {
            VerifyPanelLayout();

            Sprite hamburgerSprite = VillageHubUiBuilder.LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MenuHamburgerIcon.png");
            Sprite panelSprite = VillageHubUiBuilder.LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MenuPanelDark.png");
            Sprite dividerLeftSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/MenuSectionDivider.png", "MenuSectionDivider_Left");
            Sprite dividerRightSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/MenuSectionDivider.png", "MenuSectionDivider_Right");
            Sprite lockSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/MenuLockBadge.png", "MenuLockBadge");

            var openGo = new GameObject("MainMenuButton", typeof(Image), typeof(Button));
            openGo.transform.SetParent(canvasGo.transform, false);
            var openRect = openGo.GetComponent<RectTransform>();
            openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 1f);
            openRect.pivot = new Vector2(1f, 1f);
            openRect.sizeDelta = new Vector2(HamburgerButtonSize, HamburgerButtonSize);
            openRect.anchoredPosition = new Vector2(-20f, -20f);
            var openImage = openGo.GetComponent<Image>();
            openImage.sprite = hamburgerSprite;
            openImage.type = Image.Type.Simple;
            openImage.preserveAspect = true;

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
            backdropImage.color = new Color(0.015f, 0.025f, 0.06f, 0.62f);
            backdropImage.raycastTarget = true;
            Button backdropButton = backdropGo.GetComponent<Button>();

            var panelGo = new GameObject("MainMenuPanel", typeof(Image));
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = new Vector2(-OdinPanelWidth, OdinPanelBottomMargin);
            panelRect.offsetMax = new Vector2(0f, -OdinPanelTopMargin);
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;

            var allButtons = new System.Collections.Generic.List<Button>();
            var allLabels = new System.Collections.Generic.List<string>();
            var allAvailable = new System.Collections.Generic.List<bool>();
            var allIds = new System.Collections.Generic.List<string>();

            float contentWidth = OdinPanelWidth - 2f * OdinContentMargin;
            float cellWidth = contentWidth / OdinColumns;
            float cursorY = -(OdinContentPadding - ContentUpShift);

            foreach (MenuSectionDefinition section in MenuCatalog.Sections)
            {
                int rows = Mathf.CeilToInt(section.Items.Length / (float)OdinColumns);

                // 2026-09-16 (widen menu task, part A4/A5): the old code
                // force-jumped "시스템"'s cursorY to sit flush against the
                // panel's bottom edge (reserving room for the now-removed
                // footer button), which left a large dead gap between 모험's
                // last row and 시스템's header (see OdinContentPadding's doc
                // comment above for the exact number - the orchestrator's
                // final_village_warrior_menu.png reference screenshot showed
                // it). That jump is gone: 시스템 now flows straight through
                // from 모험 with the same OdinSectionGap every other section
                // transition already uses, collapsing that gap from ~180px
                // down to 4px - far more than the 20px tightening this task
                // separately asked for. Deliberately NOT stacking an
                // additional -20 on top of this (docs/DECISIONS.md 2026-09-16
                // entry): OdinSectionGap is already the same minimal 4px
                // every other section boundary uses, and subtracting 20 more
                // would push 시스템's header up into 모험's last icon row (the
                // "겹침 검증 통과" requirement this same task item ends with
                // would fail).
                VillageHubMenuHeaderBuilder.BuildOdinSectionHeader(panelGo, dividerLeftSprite, dividerRightSprite, section.Title, cursorY, contentWidth);
                cursorY -= OdinHeaderHeight + OdinHeaderToItemsGap;

                for (int r = 0; r < rows; r++)
                {
                    float rowY = cursorY - r * OdinItemRowHeight;
                    int rowStartIndex = r * OdinColumns;
                    int itemsInRow = Mathf.Min(OdinColumns, section.Items.Length - rowStartIndex);
                    float columnPitch = cellWidth * OdinColumnPitchScale;
                    float firstItemCenterX = OdinPanelWidth * 0.5f - (itemsInRow - 1) * columnPitch * 0.5f;
                    for (int c = 0; c < itemsInRow; c++)
                    {
                        int index = rowStartIndex + c;

                        MenuItemDefinition item = section.Items[index];
                        float cellCenterX = firstItemCenterX + c * columnPitch;
                        Button itemButton = BuildOdinMenuItem(panelGo, item, lockSprite, new Vector2(cellCenterX, rowY), cellWidth);
                        allButtons.Add(itemButton);
                        allLabels.Add(item.Label);
                        allAvailable.Add(item.IsAvailable);
                        allIds.Add(item.Id);
                    }
                }

                cursorY = cursorY - rows * OdinItemRowHeight - OdinSectionGap;
            }

            ConfirmDialog quitConfirmDialog = VillageHubMenuHeaderBuilder.BuildQuitConfirmDialog(canvasGo);

            var controller = canvasGo.AddComponent<MainMenuPanel>();
            controller.Configure(overlayGo, openGo.GetComponent<Button>(), allButtons.ToArray(), allLabels.ToArray(), allAvailable.ToArray(), allIds.ToArray(), messagePanel, quitConfirmDialog);
            backdropButton.onClick.AddListener(controller.Close);
            overlayGo.SetActive(false);
        }

        // Locked items: gray-tinted icon + a MenuLockBadge overlay at the
        // icon's bottom-right, sized to 40% of the icon, and
        // Button.interactable=false (spec item 4). Available items keep the
        // icon at full color and stay interactable - MainMenuPanel wires their
        // click behavior.
        private static Button BuildOdinMenuItem(GameObject panelGo, MenuItemDefinition item, Sprite lockSprite, Vector2 anchoredPosition, float cellWidth)
        {
            // 2026-09-16 (character_select click bug fix, docs/HANDOFF.md):
            // this root used to be typeof(RectTransform), typeof(Button) only
            // - no Graphic of its own, and both children below (Icon/Label)
            // explicitly set raycastTarget=false. UnityEngine.UI's
            // GraphicRaycaster only ever considers Graphic components with
            // raycastTarget=true as hit candidates; with none anywhere in
            // this item's hierarchy, a click here always fell through to
            // whatever Graphic sat behind it instead (panelGo's own
            // background Image, which has no click handler) - so EVERY item
            // in this grid was unclickable by mouse, not just the newly
            // added "character_select"/"quit" ids (those two just happened
            // to be the ones actually exercised after this session's menu
            // widen, which is how the bug surfaced). Fix: give the item root
            // its own full-cell Image as an invisible (alpha 0) raycast
            // target, so the whole cell - not just the icon/label glyphs -
            // is clickable and Button.onClick fires normally.
            var itemGo = new GameObject("MenuItem_" + item.Id, typeof(RectTransform), typeof(Image), typeof(Button));
            itemGo.transform.SetParent(panelGo.transform, false);
            var itemRect = itemGo.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(0f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(cellWidth, OdinItemRowHeight);
            itemRect.anchoredPosition = anchoredPosition;
            var itemHitArea = itemGo.GetComponent<Image>();
            itemHitArea.color = new Color(1f, 1f, 1f, 0f);
            itemHitArea.raycastTarget = true;
            var button = itemGo.GetComponent<Button>();

            // 2026-09-16 (widen menu task, part B): the 2 new system-section
            // items (character_select/quit) are sliced from a dedicated
            // MenuIconsSetExtra.png rather than the shared MenuIconsSetDark.png
            // every other item uses - see MenuItemDefinition.IconSheetFileName.
            string iconSheetFileName = string.IsNullOrEmpty(item.IconSheetFileName) ? "MenuIconsSetDark.png" : item.IconSheetFileName;
            Sprite iconSprite = VillageHubUiBuilder.LoadNamedSprite(SapphireSceneBuilder.UiArtDir + "/" + iconSheetFileName, item.IconSpriteName);
            var iconGo = new GameObject("Icon", typeof(Image));
            iconGo.transform.SetParent(itemGo.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.sizeDelta = new Vector2(OdinIconSize, OdinIconSize);
            float iconVerticalOffset = item.Id == "equipment" || item.Id == "map" ? -6f : 0f;
            iconRect.anchoredPosition = new Vector2(0f, iconVerticalOffset);
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
                iconImage.color = new Color(0.42f, 0.50f, 0.60f, 0.72f);
                label.color = new Color(0.58f, 0.65f, 0.72f, 0.82f);
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
                label.color = new Color(0.95f, 0.96f, 0.92f, 1f);
            }

            return button;
        }

    }
}
