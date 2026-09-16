using UnityEngine;
using UnityEngine.UI;
using Sapphire.Presentation.CharacterFlow;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Section header/divider + "게임 종료" confirm dialog helpers for the
    /// Odin-style menu panel. Split out of <see cref="VillageHubMenuBuilder"/>
    /// (2026-09-16, widen menu task) purely to keep that file under this
    /// codebase's ~500-line convention (rules/coding-style.md) - these three
    /// methods (BuildOdinSectionHeader/BuildDividerHalf/BuildQuitConfirmDialog)
    /// were the largest fully self-contained chunk that didn't require
    /// exposing a wide swath of VillageHubMenuBuilder's grid-layout constants,
    /// only <see cref="VillageHubMenuBuilder.OdinHeaderHeight"/> and
    /// <see cref="VillageHubMenuBuilder.OdinContentMargin"/> (both widened
    /// from private to internal for this).
    /// </summary>
    internal static class VillageHubMenuHeaderBuilder
    {
        // 2026-09-15 (gemless MapleStory-M rebuild): dropped the
        // MenuSectionHeader.png banner backdrop entirely - a section header
        // is now just a centered title with a short fading divider line on
        // each side (MenuSectionDivider_Left/_Right), no gold plate. Divider
        // width is a fixed on-screen size (not stretched - a fading line
        // would break if 9-sliced/stretched), chosen so both dividers plus a
        // generous center gap for the title fit inside contentWidth for
        // every current section title (성장/모험/시스템, all short).
        private const float DividerWidth = 70f;
        private const float DividerHeight = 7f;

        // 2026-09-16 (header/content overflow fix, docs/HANDOFF.md): this
        // used to shrink the header by an extra 40 units on top of
        // OdinContentMargin, as a band-aid for that margin being computed
        // against the wrong panel sprite's border (see
        // VillageHubMenuBuilder.OdinContentMargin's comment - it assumed
        // MenuPanelOdin.png's 17.33-unit border when the panel actually
        // drawn is MenuPanelDark.png's 52-unit one). Narrowing the header
        // alone patched the symptom there while leaving the icon grid
        // (which shares the same margin) under-inset by the same amount,
        // and still 6-20 units short of the real border either way - which
        // is why the user kept seeing header text outside the panel after
        // this "fix" shipped. Now that OdinContentMargin itself uses the
        // correct border, the header needs no separate reduction - it uses
        // the exact same contentWidth as the icon grid below it.
        internal static void BuildOdinSectionHeader(GameObject panelGo, Sprite dividerLeftSprite, Sprite dividerRightSprite, string title, float topY, float contentWidth)
        {
            float headerWidth = contentWidth;

            var headerGo = new GameObject("Section_" + title, typeof(RectTransform));
            headerGo.transform.SetParent(panelGo.transform, false);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.sizeDelta = new Vector2(headerWidth, VillageHubMenuBuilder.OdinHeaderHeight);
            headerRect.anchoredPosition = new Vector2(VillageHubMenuBuilder.OdinContentMargin, topY);

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(headerGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.25f);
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = VillageHubUiBuilder.LoadKoreanFont();
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.96f, 0.89f, 0.70f, 1f);
            text.fontSize = 18;
            text.text = title;
            text.raycastTarget = false;

            float halfLineWidth = headerWidth * 0.5f;
            BuildDividerHalf(headerGo, dividerLeftSprite, "UnderlineLeft", new Vector2(0f, 0f), halfLineWidth);
            BuildDividerHalf(headerGo, dividerRightSprite, "UnderlineRight", new Vector2(1f, 0f), halfLineWidth);
        }

        private static void BuildDividerHalf(GameObject headerGo, Sprite dividerSprite, string name, Vector2 anchor, float width)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(headerGo.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(width, DividerHeight);
            rect.anchoredPosition = new Vector2(0f, 3f);
            var image = go.GetComponent<Image>();
            image.sprite = dividerSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        // "게임 종료" reuses ConfirmDialog (Presentation/CharacterFlow) exactly
        // as CharacterSelectSceneBuilder.BuildConfirmDialog does for character
        // deletion - same panel/button art (MessagePanelFrameGold/
        // MenuButtonGold, both already loaded elsewhere in the VillageHub
        // scene) and the same CharacterFlowUiScaffold helpers, just a
        // dedicated instance parented under this scene's canvas since
        // VillageHub has no ConfirmDialog of its own yet.
        internal static ConfirmDialog BuildQuitConfirmDialog(GameObject canvasGo)
        {
            Sprite panelSprite = VillageHubUiBuilder.LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MessagePanelFrameGold.png");
            Sprite buttonSprite = VillageHubUiBuilder.LoadSingleSprite(SapphireSceneBuilder.UiArtDir + "/MenuButtonGold.png");
            Image panel = CharacterFlowUiScaffold.BuildSlicedPanel(canvasGo, panelSprite, "QuitConfirmDialog", Vector2.zero, new Vector2(500f, 260f));
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
