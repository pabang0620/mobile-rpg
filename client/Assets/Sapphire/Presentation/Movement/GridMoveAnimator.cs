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
        [Tooltip("docs/DECISIONS.md의 0.08s는 topdown-asset-mvp 참고치였고 실제 플레이 체감상 너무 빨라 " +
            "2026-09-14 사용자 피드백으로 2배(0.16s)로 상향함.")]
        [SerializeField] private float moveDuration = 0.16f;

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
            activeMove = null;
            onComplete?.Invoke();
        }
    }
}
