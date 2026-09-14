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
            "재상향(약 2.5배, 0.4s)함.")]
        [SerializeField] private float moveDuration = 0.4f;

        [Tooltip("한 칸 이동 완료 직후 다음 이동을 시작하기 전 두는 짧은 정지 간격. " +
            "연속 이동 시 '이동 -> 살짝 멈춤 -> 이동'의 칸 단위 리듬을 만들어 자유이동처럼 보이지 않게 한다. " +
            "2026-09-14 추가.")]
        [SerializeField] private float stepPause = 0.04f;

        private Coroutine activeMove;

        public void PlayMove(Transform target, WorldPoint from, WorldPoint to, Action onComplete)
        {
            if (activeMove != null)
            {
                StopCoroutine(activeMove);
            }

            activeMove = StartCoroutine(MoveRoutine(target, from, to, onComplete));
        }

        private IEnumerator MoveRoutine(Transform target, WorldPoint from, WorldPoint to, Action onComplete)
        {
            Vector3 start = new Vector3(from.X, from.Y, target.position.z);
            Vector3 end = new Vector3(to.X, to.Y, target.position.z);
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                target.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            target.position = end;

            if (stepPause > 0f)
            {
                // Domain stays IsMoving==true through this wait (onComplete, which calls
                // GridMover.CompleteMove(), fires only after it) - this is what blocks the
                // next TryBeginMove and creates the "step -> brief stop -> step" rhythm
                // instead of the moves chaining into continuous free-roam motion.
                yield return new WaitForSeconds(stepPause);
            }

            activeMove = null;
            onComplete?.Invoke();
        }
    }
}
