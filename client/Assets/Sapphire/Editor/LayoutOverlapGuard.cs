using UnityEngine;

namespace Sapphire.EditorTools
{
    /// <summary>
    /// Build-time layout guards for CharacterCreate/CharacterSelect (D4 spec:
    /// "레이아웃 수치는 빌더 상수로 모으고 빌드타임 겹침 검증... 추가:
    /// 생성/선택 화면 주요 요소끼리 겹치면 빌드 실패"). Works entirely off
    /// the same anchor/pivot/anchoredPosition/sizeDelta numbers the builders
    /// already compute - it does not read a live RectTransform.rect, because
    /// batchmode scene builds run before any GameView/screen exists and
    /// RectTransform.rect can't be trusted yet. Every Rect passed to
    /// VerifyNoOverlap/VerifyContained must come from ToCanvasRect below,
    /// using the SAME reference resolution (1280x720 - see
    /// CharacterFlowUiScaffold.BuildEventSystemAndCanvas's
    /// CanvasScaler.referenceResolution) every element in these two scenes is
    /// laid out against.
    ///
    /// Point-anchor elements only (anchorMin == anchorMax) - every element
    /// CharacterCreateSceneBuilder/CharacterSelectSceneBuilder check is
    /// anchored to a single point (card-center 0.5/0.5 or a corner), never a
    /// stretch anchor, so ToCanvasRect doesn't need to handle that case.
    /// VillageHubMenuBuilder's own panel-height check is deliberately NOT
    /// routed through this helper - its elements are children of the menu
    /// panel (not the canvas), a different coordinate frame, and per this
    /// codebase's locality-over-DRY convention (see rules/coding-style.md)
    /// it keeps its own small inline arithmetic check instead
    /// (VillageHubMenuBuilder.VerifyPanelLayout/ComputeSectionsHeight,
    /// 2026-09-16 - the panel's own top/bottom padding is now sized to
    /// content instead of the panel guarding one footer element that
    /// method's predecessor, VerifyFooterClearance, used to check).
    /// </summary>
    internal static class LayoutOverlapGuard
    {
        internal const float ReferenceWidth = 1280f;
        internal const float ReferenceHeight = 720f;

        /// <summary>
        /// Converts a point-anchored RectTransform's construction values into
        /// a canvas-space Rect (origin at canvas center, x right+/y up+),
        /// matching Unity's own anchor resolution formula for a canvas whose
        /// rect exactly equals (ReferenceWidth, ReferenceHeight).
        /// </summary>
        internal static Rect ToCanvasRect(Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            float anchorPointX = -ReferenceWidth / 2f + anchor.x * ReferenceWidth;
            float anchorPointY = -ReferenceHeight / 2f + anchor.y * ReferenceHeight;
            float xMin = anchorPointX + anchoredPosition.x - pivot.x * sizeDelta.x;
            float yMin = anchorPointY + anchoredPosition.y - pivot.y * sizeDelta.y;
            return new Rect(xMin, yMin, sizeDelta.x, sizeDelta.y);
        }

        /// <summary>Convenience overload for the common center-anchored (0.5,0.5) case every card/title/button in these two scenes uses.</summary>
        internal static Rect ToCenterAnchoredCanvasRect(Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            return ToCanvasRect(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta);
        }

        /// <summary>Throws if any two of the given named rects intersect.</summary>
        internal static void VerifyNoOverlap(params (string Name, Rect Rect)[] elements)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                for (int j = i + 1; j < elements.Length; j++)
                {
                    if (elements[i].Rect.Overlaps(elements[j].Rect))
                    {
                        throw new System.Exception(
                            $"Layout overlap detected: '{elements[i].Name}' {elements[i].Rect} overlaps " +
                            $"'{elements[j].Name}' {elements[j].Rect}");
                    }
                }
            }
        }

        /// <summary>Throws unless innerRect lies fully within outerRect (e.g. a pill button inset inside a card's border padding).</summary>
        internal static void VerifyContained(string innerName, Rect innerRect, string outerName, Rect outerRect)
        {
            if (innerRect.xMin < outerRect.xMin || innerRect.xMax > outerRect.xMax ||
                innerRect.yMin < outerRect.yMin || innerRect.yMax > outerRect.yMax)
            {
                throw new System.Exception(
                    $"Layout containment violated: '{innerName}' {innerRect} is not fully inside " +
                    $"'{outerName}' {outerRect}");
            }
        }
    }
}
