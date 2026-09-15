using UnityEditor;
using UnityEngine;
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
        private const float OdinPanelTopMargin = 24f;
        private const float OdinPanelBottomMargin = 24f;
        private const float OdinContentMargin = 24f;
        private const float OdinHeaderHeight = 36f;
        private const float OdinHeaderToItemsGap = 12f;
        private const float OdinItemRowHeight = 104f;
        private const float OdinSectionGap = 22f;
        private const float OdinIconSize = 56f;
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

            var panelGo = new GameObject("MainMenuPanel", typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
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

                cursorY -= rows * OdinItemRowHeight + OdinSectionGap;
            }

            var controller = canvasGo.AddComponent<MainMenuPanel>();
            controller.Configure(panelGo, openGo.GetComponent<Button>(), allButtons.ToArray(), allLabels.ToArray(), allAvailable.ToArray(), messagePanel);
            panelGo.SetActive(false);
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
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(0f, 24f);
            labelRect.anchoredPosition = new Vector2(0f, -OdinIconSize - 4f);
            var label = labelGo.GetComponent<Text>();
            label.font = VillageHubUiBuilder.LoadKoreanFont();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 14;
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
