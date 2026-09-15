namespace Sapphire.Domain.Vfx
{
    /// <summary>
    /// Computes a sprite pivot from the bounding box of "visible" (alpha above
    /// threshold) pixels within a single atlas frame, instead of always using the
    /// frame rect's own geometric center/left-edge.
    ///
    /// 2026-09-15 skill-cast jitter bug (docs/HANDOFF.md): the mage/warrior VFX
    /// atlases (MageSkillVfxAtlas.png, WarriorSkillVfxAtlas.png, and the
    /// ManaShieldPadded/ThunderFieldPadded/WarriorGroundSlamPadded strips) are
    /// AI-generated per-frame, and the drawn effect is NOT consistently centered
    /// within its own cell from frame to frame (measured bbox-center drift up to
    /// ~24% of a cell's width/height across a row's 8 frames). SkillVfxImporter/
    /// WarriorSkillVfxImporter used to slice every frame with a fixed pivot
    /// (0.5,0.5 for centered effects, 0,0.5 for directional ones that grow from the
    /// actor) - since the caster-anchored transform.position never moves during a
    /// cast, a fixed pivot on inconsistently-centered content makes the rendered
    /// art visibly hop/shake around the caster as frames cycle, which reads as
    /// "the character is shaking" even though the character's own transform never
    /// moves (see SkillVfxPlayer.Play/WarriorSkillVfxPlayer.Play - neither ever
    /// touches the actor's own transform, only the spawned VFX GameObject's).
    ///
    /// Anchoring each frame's pivot to its own alpha content keeps the rendered
    /// art visually still against the fixed cast position instead.
    ///
    /// Pure/engine-free (plain byte alpha array, no Texture2D) so this is
    /// unit-testable without loading a texture asset - the Editor importers
    /// convert Texture2D.GetPixels32() alpha bytes into the flat array this takes.
    /// </summary>
    public static class VfxFramePivotCalculator
    {
        /// <param name="alpha">
        /// Row-major alpha bytes for the FULL texture, bottom-to-top (Unity's
        /// Texture2D.GetPixels32 convention - matches the "height - bottom" rect
        /// flip SkillVfxImporter/WarriorSkillVfxImporter already do).
        /// </param>
        /// <param name="textureWidth">Full texture row stride (pixels per row).</param>
        /// <param name="frameLeft">Frame rect's left edge, in texture pixels.</param>
        /// <param name="frameBottom">Frame rect's bottom edge, in texture pixels.</param>
        /// <param name="frameWidth">Frame rect width, in texture pixels.</param>
        /// <param name="frameHeight">Frame rect height, in texture pixels.</param>
        /// <param name="directional">
        /// True for rows that grow from the actor toward a facing direction
        /// (spear/slash/dash) - pivot.x anchors to the content's own LEFT edge
        /// (so Play()'s tile-count localScale stretch still starts exactly where
        /// the art starts) instead of its horizontal center.
        /// </param>
        /// <param name="alphaThreshold">Alpha (0-255) above which a pixel counts as content.</param>
        public static (float x, float y) ComputeContentPivot(
            byte[] alpha, int textureWidth,
            int frameLeft, int frameBottom, int frameWidth, int frameHeight,
            bool directional, byte alphaThreshold = 10)
        {
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (int y = 0; y < frameHeight; y++)
            {
                int rowBase = (frameBottom + y) * textureWidth + frameLeft;
                for (int x = 0; x < frameWidth; x++)
                {
                    if (alpha[rowBase + x] > alphaThreshold)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (minX > maxX)
            {
                // Fully transparent frame (e.g. a trailing empty tail frame) - fall
                // back to the old geometric default so it renders exactly where it
                // always did (there is no content to mis-align in the first place).
                return directional ? (0f, .5f) : (.5f, .5f);
            }

            float pivotX = directional ? minX / (float)frameWidth : (minX + maxX) / 2f / frameWidth;
            float pivotY = (minY + maxY) / 2f / frameHeight;
            return (pivotX, pivotY);
        }
    }
}
