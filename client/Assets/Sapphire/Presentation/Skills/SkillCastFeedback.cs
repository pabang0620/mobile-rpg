using UnityEngine;

namespace Sapphire.Presentation.Skills
{
    /// <summary>
    /// "Something happened" feedback for a skill cast. This used to be a
    /// brief sprite color flash, but the actual attack/skill motion frames
    /// added 2026-09-16 (see WarriorArtImportConfigurator's Attack row and
    /// SkillMotionPlayer) are the real cast feedback now, and the user
    /// explicitly asked for the color flash to go away once real motion
    /// existed ("기본공격하면 캐릭터 색상이 뭐 바뀌는게있는데 이건 좀
    /// 없애던가 해줘"). Left as a no-op rather than deleted - RadialSkillMenu/
    /// SapphireSceneBuilder/VillageHubSkillMenuBuilder all still construct
    /// and wire a `SkillCastFeedback` instance, and this keeps that plumbing
    /// compiling unchanged in case a non-flash "something happened" cue
    /// (e.g. a future hit-react tint on taking damage) reuses this hook.
    ///
    /// 2026-09-16 (earlier): removed the floating skill-name TextMesh label
    /// that used to spawn above the caster's head on every cast (user
    /// report: "스킬 쓸 때 위에 캐릭터 위에 텍스트 나오는거도 없애줘") -
    /// PlayCast's `skillName` parameter is unused by this class but kept so
    /// call sites (castFeedback?.PlayCast(skill.DisplayName)) don't change.
    /// </summary>
    public class SkillCastFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;

        public void PlayCast(string skillName)
        {
            // No-op (color flash disabled per user request, 2026-09-16) -
            // kept as a call site (and kept targetRenderer wired) so future
            // non-flash feedback can hook in here without touching
            // RadialSkillMenu/SapphireSceneBuilder again.
        }
    }
}
