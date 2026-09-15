using NUnit.Framework;
using Sapphire.Domain.Vfx;

namespace Sapphire.Domain.Tests
{
    /// <summary>
    /// Regression coverage for the 2026-09-15 skill-cast jitter fix (see
    /// VfxFramePivotCalculator's class doc / docs/HANDOFF.md) - asserts the
    /// pivot-from-content-bbox algorithm itself, since SkillVfxPlayer/
    /// WarriorSkillVfxPlayer are MonoBehaviours that start coroutines (not
    /// callable from an EditMode [Test] outside Play mode) and
    /// SkillVfxImporter/WarriorSkillVfxImporter are Editor asset importers with
    /// no dedicated test assembly - this pure Domain-layer calculator is what's
    /// actually unit-testable and is exactly the code that produces the
    /// per-frame pivot Unity renders each VFX frame at.
    /// </summary>
    public class VfxFramePivotCalculatorTests
    {
        [Test]
        public void CenteredPivot_MatchesOffCenterAlphaContent()
        {
            const int width = 10, height = 10;
            var alpha = new byte[width * height];
            for (int y = 2; y <= 5; y++)
                for (int x = 6; x <= 9; x++)
                    alpha[y * width + x] = 255;

            var pivot = VfxFramePivotCalculator.ComputeContentPivot(alpha, width, 0, 0, width, height, directional: false);

            Assert.AreEqual((6 + 9) / 2f / width, pivot.x, 0.001f);
            Assert.AreEqual((2 + 5) / 2f / height, pivot.y, 0.001f);
        }

        [Test]
        public void DirectionalPivot_UsesContentLeftEdge_NotCellEdge()
        {
            const int width = 10, height = 10;
            var alpha = new byte[width * height];
            for (int y = 4; y <= 5; y++)
                for (int x = 3; x <= 8; x++)
                    alpha[y * width + x] = 255;

            var pivot = VfxFramePivotCalculator.ComputeContentPivot(alpha, width, 0, 0, width, height, directional: true);

            Assert.AreEqual(3 / (float)width, pivot.x, 0.001f);
            Assert.AreEqual((4 + 5) / 2f / height, pivot.y, 0.001f);
        }

        [Test]
        public void EmptyFrame_FallsBackToGeometricCenter()
        {
            const int width = 10, height = 10;
            var alpha = new byte[width * height];

            var pivot = VfxFramePivotCalculator.ComputeContentPivot(alpha, width, 0, 0, width, height, directional: false);

            Assert.AreEqual(0.5f, pivot.x, 0.001f);
            Assert.AreEqual(0.5f, pivot.y, 0.001f);
        }

        [Test]
        public void EmptyDirectionalFrame_FallsBackToLeftEdgePivot()
        {
            const int width = 10, height = 10;
            var alpha = new byte[width * height];

            var pivot = VfxFramePivotCalculator.ComputeContentPivot(alpha, width, 0, 0, width, height, directional: true);

            Assert.AreEqual(0f, pivot.x, 0.001f);
            Assert.AreEqual(0.5f, pivot.y, 0.001f);
        }

        [Test]
        public void ShiftedContentBetweenFrames_ProducesDifferentPivots()
        {
            // Regression guard for the actual reported bug: two same-size cells
            // whose drawn content sits in different places (as real AI-generated
            // atlas frames do) must get different pivots, or the rendered art
            // would render offset from the fixed actor position on one of them -
            // visible as the effect (and by extension the character it overlaps)
            // hopping/shaking as the animation cycles between such frames.
            const int width = 20, height = 20;
            var centered = new byte[width * height];
            var shiftedLeft = new byte[width * height];
            for (int y = 8; y <= 11; y++)
            {
                for (int x = 8; x <= 11; x++) centered[y * width + x] = 255;
                for (int x = 0; x <= 3; x++) shiftedLeft[y * width + x] = 255;
            }

            var centeredPivot = VfxFramePivotCalculator.ComputeContentPivot(centered, width, 0, 0, width, height, directional: false);
            var shiftedPivot = VfxFramePivotCalculator.ComputeContentPivot(shiftedLeft, width, 0, 0, width, height, directional: false);

            Assert.AreNotEqual(centeredPivot.x, shiftedPivot.x);
        }

        [Test]
        public void FrameOffsetWithinLargerTexture_IsRespected()
        {
            // The texture-wide alpha array covers more than one frame (as it does
            // for real 8-frame-per-row atlases) - content outside the requested
            // frame rect must not affect the computed pivot.
            const int textureWidth = 20, textureHeight = 10;
            var alpha = new byte[textureWidth * textureHeight];
            // Content in a frame at x:[10,19], all of y - a single lit pixel at (12, 3).
            alpha[3 * textureWidth + 12] = 255;
            // Unrelated content in a different frame at x:[0,9] that must be ignored.
            alpha[3 * textureWidth + 2] = 255;

            var pivot = VfxFramePivotCalculator.ComputeContentPivot(alpha, textureWidth, frameLeft: 10, frameBottom: 0, frameWidth: 10, frameHeight: textureHeight, directional: false);

            Assert.AreEqual((12 - 10) / 10f, pivot.x, 0.001f);
            Assert.AreEqual(3 / (float)textureHeight, pivot.y, 0.001f);
        }
    }
}
