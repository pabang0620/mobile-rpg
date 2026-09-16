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
            "2026-09-16: 방향전환 연타 시 반응이 늦다는 재피드백 - 주 원인은 입력 버퍼링 부재였지만(PlayerGridController/" +
            "GridMoveInputBuffer 참고), 이 값 자체도 태스크 지정 기준선(0.25s) 이상이라 완전 원복(0.16s 이하, " +
            "'자유이동처럼 보임'으로 이미 기각된 구간)은 피하면서 적당히 추가 단축(0.24s, 0.32의 약 0.75배). " +
            "2026-09-16 재조정: 이동속도가 너무 빠르다는 사용자 피드백으로 현재 속도의 70%로 하향 - " +
            "duration은 속도에 반비례하므로 0.24s / 0.7 ≈ 0.343s로 늘림. " +
            "탭 이동과 연속 이동(꾹 누름) 모두에 적용되는 기본 속도 - 연속 이동만 더 빠르게 하던 이전 시도는 " +
            "2026-09-14 재원복되어 탭/연속 구분 없이 항상 이 값을 쓴다(질주 스킬로 부스트 중일 때만 예외).")]
        [SerializeField] private float moveDuration = 0.343f;

        [Tooltip("새로 눌러서 시작된 첫 스텝 완료 직후에만 두는 짧은 정지 간격(칸 단위 리듬을 살리기 위함). " +
            "같은 방향키를 계속 누르고 있어서 이어지는 스텝(isContinuousHold=true)에는 적용하지 않는다 - " +
            "그래야 길게 누르고 있는 동안 매 칸마다 끊기지 않고 매끄럽게 이어진다. 2026-09-14 추가/조정. " +
            "2026-09-16: isContinuousHold를 이동 '시작 시점'에 캡처한 bool 대신 완료 시점에 다시 평가하는 " +
            "Func<bool>로 바꿈 - 이동 도중 키를 뗐는데도 시작 시점 값(연속유지=true)이 그대로 굳어 있어서 " +
            "stepPause 없이 즉시 mover가 풀리던 경합을 막기 위함(docs/HANDOFF.md 참고).")]
        [SerializeField] private float stepPause = 0.04f;

        private Coroutine activeMove;

        public void PlayMove(Transform target, WorldPoint from, WorldPoint to, Func<bool> isContinuousHold, Action onComplete)
        {
            if (activeMove != null)
            {
                StopCoroutine(activeMove);
            }

            activeMove = StartCoroutine(MoveRoutine(target, from, to, isContinuousHold, onComplete));
        }

        private IEnumerator MoveRoutine(Transform target, WorldPoint from, WorldPoint to, Func<bool> isContinuousHold, Action onComplete)
        {
            Vector3 start = new Vector3(from.X, from.Y, target.position.z);
            Vector3 end = new Vector3(to.X, to.Y, target.position.z);
            float duration = moveDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            target.position = end;

            // isContinuousHold is invoked HERE (move-completion time), not captured
            // as a plain bool back when PlayMove was called (move-start time). If the
            // key was released at any point during the tween, this now correctly
            // reports false and applies the settle pause below instead of freeing the
            // mover immediately with no cushion - see the field's tooltip above.
            if (!isContinuousHold() && stepPause > 0f)
            {
                // Domain stays IsMoving==true through this wait (onComplete, which calls
                // GridMover.CompleteMove(), fires only after it) - this is what blocks the
                // next TryBeginMove. Only a freshly-pressed step (or one released mid-flight)
                // pauses here; a step that is still an actively-held continuation skips this
                // entirely so held movement chains straight into the next step with no stutter.
                yield return new WaitForSeconds(stepPause);
            }

            activeMove = null;
            onComplete?.Invoke();
        }
    }
}
