namespace Sapphire.Domain.Character
{
    /// <summary>
    /// Computes a sprite pivot anchored to a character frame's own FEET (the
    /// bottom-most alpha content), instead of a hand-picked per-row constant.
    ///
    /// 2026-09-16 (character-floating-above-tile bug, docs/HANDOFF.md): the
    /// mage/warrior topdown grid sheets (MageTopdownGridSheet.png,
    /// WarriorTopdownGridSheet.png) used to ship one fixed pivot per
    /// direction (down/left/right/up), hand-picked from a single earlier PIL
    /// measurement. That measurement was corrupted for the "right" direction
    /// on both sheets by a cross-cell bleed artifact - the "up" row's own art
    /// was drawn slightly oversized and spilled a small disconnected blob
    /// into the very bottom edge of the "right" row's cell, which made the
    /// naive alpha bounding box's bottom edge look like the cell's own
    /// bottom edge (feet flush with the tile edge) instead of the
    /// character's real foot position, ~11-17% of a cell higher. Since the
    /// grid places a character's transform at the tile CENTER
    /// (Domain.Grid.GridWorldConversion.GridToWorld) and the sprite pivot is
    /// what gets placed at that transform, an under-measured foot pivot
    /// renders the character floating above the tile by exactly that gap -
    /// worst when facing right, matching the reported bug.
    ///
    /// The fix is two-part: the source PNGs had the bleed blobs stripped
    /// (see tools/art_qa/clean_character_sheets.py, kept for future art
    /// regenerations), and this calculator replaces the hand-picked
    /// per-row constants so every frame's pivot is computed straight from
    /// its own (now-clean) alpha content instead of drifting from it.
    ///
    /// Unlike <see cref="Vfx.VfxFramePivotCalculator"/> (which anchors VFX to
    /// their own content CENTER, since a spell effect has no fixed "ground"
    /// reference), a character's pivot must anchor to the FEET - the bottom
    /// of the alpha bounding box, not its vertical center - because that is
    /// the point that has to land on the tile the character stands on.
    /// pivot.x still anchors to the content's horizontal center, same as
    /// VfxFramePivotCalculator's non-directional case.
    ///
    /// Shared by both classes (see EditorTools.CharacterGridSheetImporter,
    /// which both ArtImportConfigurator and WarriorArtImportConfigurator
    /// call into) rather than each keeping its own copy - the mage and
    /// warrior sheets use the identical 3-column (idle/walkA/walkB) x
    /// 4-row (down/left/right/up) / 362px-cell layout, so the pivot
    /// algorithm has no reason to differ between them.
    ///
    /// Pure/engine-free (plain byte alpha array, no Texture2D) so this is
    /// unit-testable without loading a texture asset, same reasoning as
    /// VfxFramePivotCalculator.
    /// </summary>
    public static class CharacterFootPivotCalculator
    {
        /// <param name="alpha">
        /// Row-major alpha bytes for the FULL texture, bottom-to-top (Unity's
        /// Texture2D.GetPixels32 convention - matches the "height - bottom"
        /// rect flip the grid-slice builders already do).
        /// </param>
        /// <param name="textureWidth">Full texture row stride (pixels per row).</param>
        /// <param name="frameLeft">Frame rect's left edge, in texture pixels.</param>
        /// <param name="frameBottom">Frame rect's bottom edge, in texture pixels.</param>
        /// <param name="frameWidth">Frame rect width, in texture pixels.</param>
        /// <param name="frameHeight">Frame rect height, in texture pixels.</param>
        /// <param name="alphaThreshold">Alpha (0-255) above which a pixel counts as content.</param>
        public static (float x, float y) ComputeFootPivot(
            byte[] alpha, int textureWidth,
            int frameLeft, int frameBottom, int frameWidth, int frameHeight,
            byte alphaThreshold = 10)
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
                // Fully transparent frame - fall back to the bottom-center
                // default so it renders exactly where a plain Sprite/Multiple
                // slice with SpriteAlignment.Bottom would.
                return (0.5f, 0f);
            }

            float pivotX = (minX + maxX) / 2f / frameWidth;
            // y here is bottom-to-top (see <param name="alpha">), so the
            // feet - the LOWEST content row - is minY, not maxY.
            float pivotY = minY / (float)frameHeight;
            return (pivotX, pivotY);
        }
    }
}
