using NUnit.Framework;
using Sapphire.Domain.Character;

namespace Sapphire.Domain.Tests
{
    /// <summary>
    /// Regression coverage for the 2026-09-16 character-floating-above-tile
    /// fix (see CharacterFootPivotCalculator's class doc / docs/HANDOFF.md).
    /// Mirrors VfxFramePivotCalculatorTests' structure/coverage but asserts
    /// foot (bottom-of-content), not center, pivoting.
    /// </summary>
    public class CharacterFootPivotCalculatorTests
    {
        [Test]
        public void FootPivot_AnchorsToBottomOfAlphaContent_NotVerticalCenter()
        {
            const int width = 10, height = 10;
            var alpha = new byte[width * height];
            // Content spans y:[2,7] (bottom-to-top), x:[3,6].
            for (int y = 2; y <= 7; y++)
                for (int x = 3; x <= 6; x++)
                    alpha[y * width + x] = 255;

            var pivot = CharacterFootPivotCalculator.ComputeFootPivot(alpha, width, 0, 0, width, height);

            Assert.AreEqual((3 + 6) / 2f / width, pivot.x, 0.001f);
            // Feet = lowest row of content (y=2), not the vertical center (4.5).
            Assert.AreEqual(2 / (float)height, pivot.y, 0.001f);
        }

        [Test]
        public void FeetFlushWithFrameBottom_ProducesZeroYPivot()
        {
            const int width = 10, height = 10;
            var alpha = new byte[width * height];
            for (int y = 0; y <= 5; y++)
                for (int x = 4; x <= 5; x++)
                    alpha[y * width + x] = 255;

            var pivot = CharacterFootPivotCalculator.ComputeFootPivot(alpha, width, 0, 0, width, height);

            Assert.AreEqual(0f, pivot.y, 0.001f);
        }

        [Test]
        public void EmptyFrame_FallsBackToBottomCenter()
        {
            const int width = 10, height = 10;
            var alpha = new byte[width * height];

            var pivot = CharacterFootPivotCalculator.ComputeFootPivot(alpha, width, 0, 0, width, height);

            Assert.AreEqual(0.5f, pivot.x, 0.001f);
            Assert.AreEqual(0f, pivot.y, 0.001f);
        }

        [Test]
        public void FrameOffsetWithinLargerTexture_IsRespected()
        {
            const int textureWidth = 20, textureHeight = 10;
            var alpha = new byte[textureWidth * textureHeight];
            // Content in a frame at x:[10,19] - a single lit pixel at (12, 3).
            alpha[3 * textureWidth + 12] = 255;
            // Unrelated content in a different frame at x:[0,9] that must be ignored.
            alpha[3 * textureWidth + 2] = 255;

            var pivot = CharacterFootPivotCalculator.ComputeFootPivot(
                alpha, textureWidth, frameLeft: 10, frameBottom: 0, frameWidth: 10, frameHeight: textureHeight);

            Assert.AreEqual((12 - 10) / 10f, pivot.x, 0.001f);
            Assert.AreEqual(3 / (float)textureHeight, pivot.y, 0.001f);
        }

        [Test]
        public void DisconnectedBleedBelowMainContent_StillCountsAsFoot()
        {
            // Regression guard documenting WHY the source PNGs were cleaned
            // (tools/art_qa/clean_character_sheets.py) rather than relied on alone:
            // this calculator has no connected-component filtering, so any
            // stray alpha left below the real feet (a future re-bled art
            // regeneration) would still be picked up as "the foot" here. The
            // calculator is only correct in combination with clean source
            // art - this test pins that contract down so a regression in
            // either half (dirty art OR a naive calculator) is caught by
            // reasoning about this test, not just passing it silently.
            const int width = 10, height = 10;
            var alpha = new byte[width * height];
            for (int y = 3; y <= 7; y++)
                for (int x = 3; x <= 6; x++)
                    alpha[y * width + x] = 255; // main character body/legs
            alpha[0 * width + 5] = 255; // disconnected 1px bleed blob at the very bottom

            var pivot = CharacterFootPivotCalculator.ComputeFootPivot(alpha, width, 0, 0, width, height);

            // Without connected-component filtering the bleed pixel wins,
            // demonstrating why the source art must be pre-cleaned.
            Assert.AreEqual(0f, pivot.y, 0.001f);
        }
    }
}
