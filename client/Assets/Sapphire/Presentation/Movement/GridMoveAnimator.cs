using System;
using System.Collections;
using UnityEngine;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Movement
{
    /// <summary>
    /// Tweens a transform's world position from A to B over a short, fixed
    /// duration. Pure presentation - no Domain dependency beyond the plain
    /// WorldPoint value type.
    /// </summary>
    public class GridMoveAnimator : MonoBehaviour
    {
        [Tooltip("docs/DECISIONS.md의 0.08s는 topdown-asset-mvp 참고치였고, 0.16s로 1차 상향했으나 " +
            "여전히 자유이동처럼 보인다는 2026-09-14 사용자 피드백으로 한 칸 이동이 눈에 확실히 보이도록 " +
            "재상향(약 2.5배, 0.4s)했으나, 전체적으로 더 빠르게 해달라는 후속 피드백으로 1.25배 단축(0.32s)함. " +
            "탭 이동과 연속 이동(꾹 누름) 모두에 적용되는 기본 속도 - 연속 이동만 더 빠르게 하던 이전 시도는 " +
            "2026-09-14 재원복되어 탭/연속 구분 없이 항상 이 값을 쓴다(질주 스킬로 부스트 중일 때만 예외).")]
        [SerializeField] private float moveDuration = 0.32f;

        [Tooltip("'질주' 공용 스킬(SkillCatalog의 skill.haste) 사용 중(IsSpeedBoosted=true) 10초간 적용되는 " +
            "스텝당 소요 시간. 한때 연속 이동(꾹 누름) 전용 속도로 쓰였던 값을 그대로 재사용 - " +
            "이제는 isContinuousHold 여부와 무관하게 부스트 상태에서만 적용된다. 2026-09-14 질주 스킬 추가.")]
        [SerializeField] private float boostedMoveDuration = 0.22f;

        [Tooltip("새로 눌러서 시작된 첫 스텝 완료 직후에만 두는 짧은 정지 간격(칸 단위 리듬을 살리기 위함). " +
            "같은 방향키를 계속 누르고 있어서 이어지는 스텝(isContinuousHold=true)에는 적용하지 않는다 - " +
            "그래야 길게 누르고 있는 동안 매 칸마다 끊기지 않고 매끄럽게 이어진다. 2026-09-14 추가/조정.")]
        [SerializeField] private float stepPause = 0.04f;

        private Coroutine activeMove;
        private Coroutine speedBoostRoutine;

        /// <summary>True while the "질주" skill's 10-second speed boost is active.</summary>
        public bool IsSpeedBoosted { get; private set; }

        /// <summary>
        /// Turns on the boosted move speed (boostedMoveDuration) for durationSeconds,
        /// then automatically reverts to the normal moveDuration. Re-casting while
        /// already boosted simply restarts the 10-second window.
        /// </summary>
        public void ActivateSpeedBoost(float durationSeconds)
        {
            if (speedBoostRoutine != null)
            {
                StopCoroutine(speedBoostRoutine);
            }

            IsSpeedBoosted = true;
            speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(durationSeconds));
        }

        private IEnumerator SpeedBoostRoutine(float durationSeconds)
        {
            yield return new WaitForSeconds(durationSeconds);
            IsSpeedBoosted = false;
            speedBoostRoutine = null;
        }

        public void PlayMove(Transform target, WorldPoint from, WorldPoint to, bool isContinuousHold, Action onComplete)
        {
            if (activeMove != null)
            {
                StopCoroutine(activeMove);
            }

            activeMove = StartCoroutine(MoveRoutine(target, from, to, isContinuousHold, onComplete));
        }

        private IEnumerator MoveRoutine(Transform target, WorldPoint from, WorldPoint to, bool isContinuousHold, Action onComplete)
        {
            Vector3 start = new Vector3(from.X, from.Y, target.position.z);
            Vector3 end = new Vector3(to.X, to.Y, target.position.z);
            float duration = IsSpeedBoosted ? boostedMoveDuration : moveDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            target.position = end;

            if (!isContinuousHold && stepPause > 0f)
            {
                // Domain stays IsMoving==true through this wait (onComplete, which calls
                // GridMover.CompleteMove(), fires only after it) - this is what blocks the
                // next TryBeginMove. Only a freshly-pressed step pauses here; a step that is
                // itself a continuation of a held key (isContinuousHold) skips this entirely
                // so held movement chains straight into the next step with no stutter.
                yield return new WaitForSeconds(stepPause);
            }

            activeMove = null;
            onComplete?.Invoke();
        }
    }
}
