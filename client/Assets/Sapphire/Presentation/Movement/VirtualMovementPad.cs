using UnityEngine;
using UnityEngine.EventSystems;
using Sapphire.Domain.Grid;

namespace Sapphire.Presentation.Movement
{
    /// <summary>
    /// Bottom-left touch/mouse virtual stick (2026-09-14 UI overhaul, see
    /// docs/DECISIONS.md). Press-and-drag inside the pad resolves to the
    /// dominant axis of the drag vector - this game only has 4-directional
    /// grid movement (see GridDirection), no diagonal, so there is no
    /// meaningful "angle" beyond up/down/left/right.
    ///
    /// PlayerInputReader polls <see cref="HeldDirection"/> exactly like it
    /// polls Keyboard.current, so touch/mouse and keyboard always drive the
    /// same TryBeginMove call - no separate input path (docs/planning/
    /// 05_AGENT_PLAYBOOK.md: "동일 명령의 키보드와 터치 구현을 별도로 만들지 않는다").
    /// </summary>
    public class VirtualMovementPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("드래그 거리가 패드 반지름의 이 비율(0~1)을 넘어야 방향으로 인정한다. " +
            "너무 작으면 살짝 스친 것만으로도 이동이 발생해 오조작이 된다.")]
        [SerializeField] private float deadzoneRatio = 0.25f;

        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform knob;

        private float radius;
        private Vector2 dragOriginScreenPos;

        /// <summary>Current held direction, or null when the pad isn't being dragged past the deadzone.</summary>
        public GridDirection? HeldDirection { get; private set; }

        private void Awake()
        {
            radius = background != null ? background.rect.width * 0.5f : 1f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Anchored to the pad's own center rather than the initial touch point,
            // so the stick always reads relative to a fixed origin (standard virtual
            // joystick behavior) regardless of where inside the pad the drag starts.
            dragOriginScreenPos = background != null
                ? RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, background.position)
                : eventData.position;

            UpdateFromDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateFromDrag(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            HeldDirection = null;

            if (knob != null)
            {
                knob.anchoredPosition = Vector2.zero;
            }
        }

        private void UpdateFromDrag(Vector2 screenPosition)
        {
            Vector2 delta = screenPosition - dragOriginScreenPos;
            float effectiveRadius = radius > 0f ? radius : 1f;

            if (knob != null)
            {
                knob.anchoredPosition = Vector2.ClampMagnitude(delta, effectiveRadius);
            }

            if (delta.magnitude < effectiveRadius * deadzoneRatio)
            {
                HeldDirection = null;
                return;
            }

            HeldDirection = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? (delta.x >= 0f ? GridDirection.Right : GridDirection.Left)
                : (delta.y >= 0f ? GridDirection.Up : GridDirection.Down);
        }
    }
}
