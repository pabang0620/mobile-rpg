using System.Collections;
using UnityEngine;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Movement
{
    /// <summary>
    /// Plays a short Windup -> Apex -> Recovery sprite sequence over the
    /// player's SpriteRenderer when a basic attack or skill is cast, sourced
    /// from the per-class *AttackGridSheet.png sheet (3 columns: Windup/Apex/
    /// Recovery, 4 rows: Down/Left/Right/Up - same 362px-cell grid as the
    /// walk sheet, see ArtImportConfigurator.ConfigureMageAttackSheet /
    /// WarriorArtImportConfigurator.ConfigureWarriorAttackSheet). This is the
    /// "SkillMotionPlayer" SkillCastFeedback's class doc already refers to -
    /// added 2026-09-16 so a cast has a real character motion (sword swing /
    /// staff raise) instead of only a VFX playing next to a static sprite.
    ///
    /// Movement input is NOT locked while this plays (not requested, and
    /// RadialSkillMenu already only casts the basic attack while the mover is
    /// not moving) - if the player starts moving mid-swing,
    /// DirectionalSpriteAnimator's own per-frame SetFacing/SetMoving calls
    /// simply take the SpriteRenderer back over, cutting this coroutine's
    /// remaining frames short. That's an acceptable rare edge case rather
    /// than a state machine worth adding for this slice.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkillMotionPlayer : MonoBehaviour
    {
        // Matches WarriorSkillVfxPlayer.PlayBasicAttack's own slash VFX
        // duration (0.35s) so the swing pose and the VFX land together.
        [SerializeField] private float motionDuration = 0.35f;

        [Header("Windup column (*AttackGridSheet.png)")]
        [SerializeField] private Sprite windupUp;
        [SerializeField] private Sprite windupDown;
        [SerializeField] private Sprite windupLeft;
        [SerializeField] private Sprite windupRight;

        [Header("Apex column (*AttackGridSheet.png)")]
        [SerializeField] private Sprite apexUp;
        [SerializeField] private Sprite apexDown;
        [SerializeField] private Sprite apexLeft;
        [SerializeField] private Sprite apexRight;

        [Header("Recovery column (*AttackGridSheet.png)")]
        [SerializeField] private Sprite recoveryUp;
        [SerializeField] private Sprite recoveryDown;
        [SerializeField] private Sprite recoveryLeft;
        [SerializeField] private Sprite recoveryRight;

        [SerializeField] private DirectionalSpriteAnimator spriteAnimator;

        private SpriteRenderer spriteRenderer;
        private Coroutine activeMotion;

        // Precomputed per-direction 3-frame sequences, built once in Awake
        // from the serialized single sprites above - same convention
        // DirectionalSpriteAnimator's own idleFrames*/movingFrames* use, so
        // PlayAttack never allocates.
        private Sprite[] framesUp;
        private Sprite[] framesDown;
        private Sprite[] framesLeft;
        private Sprite[] framesRight;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            framesUp = new[] { windupUp, apexUp, recoveryUp };
            framesDown = new[] { windupDown, apexDown, recoveryDown };
            framesLeft = new[] { windupLeft, apexLeft, recoveryLeft };
            framesRight = new[] { windupRight, apexRight, recoveryRight };
        }

        /// <summary>
        /// Starts (or restarts, if already mid-swing) the attack pose
        /// sequence facing the given direction. Used for both the basic
        /// attack and skill casts (RadialSkillMenu.CastBasicAttack/CastSkill)
        /// - the same three poses read as a generic "the character reacted"
        /// motion regardless of which skill triggered it.
        /// </summary>
        public void PlayAttack(GridDirection facing)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Sprite[] frames = FramesFor(facing);
            if (frames == null)
            {
                return;
            }

            if (activeMotion != null)
            {
                StopCoroutine(activeMotion);
            }

            activeMotion = StartCoroutine(PlayMotion(frames));
        }

        private IEnumerator PlayMotion(Sprite[] frames)
        {
            float frameDuration = motionDuration / frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    spriteRenderer.sprite = frames[i];
                }

                yield return new WaitForSeconds(frameDuration);
            }

            activeMotion = null;
            spriteAnimator?.ForceRefresh();
        }

        private Sprite[] FramesFor(GridDirection facing)
        {
            switch (facing)
            {
                case GridDirection.Up:
                    return framesUp;
                case GridDirection.Down:
                    return framesDown;
                case GridDirection.Left:
                    return framesLeft;
                case GridDirection.Right:
                    return framesRight;
                default:
                    return null;
            }
        }
    }
}
