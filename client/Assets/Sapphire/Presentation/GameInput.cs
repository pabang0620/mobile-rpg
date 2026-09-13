using UnityEngine;
using UnityEngine.EventSystems;

namespace Sapphire
{
    /// <summary>Pointer ownership prevents a second finger stealing the movement stick.</summary>
    public sealed class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        public Vector2 Value { get; private set; }
        int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData e) { if (pointer != int.MinValue) return; pointer = e.pointerId; OnDrag(e); }
        public void OnDrag(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out local);
            Value = Vector2.ClampMagnitude(local / 49f, 1);
            if (Knob) Knob.anchoredPosition = Value * 39;
        }
        public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) ResetInput(); }
        public void ResetInput() { pointer = int.MinValue; Value = Vector2.zero; if (Knob) Knob.anchoredPosition = Vector2.zero; }
        void OnDisable() { ResetInput(); }
    }

    public sealed class HoldCommand : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e) { Held = true; }
        public void OnPointerUp(PointerEventData e) { Held = false; }
        void OnDisable() { Held = false; }
        public void ResetInput() { Held = false; }
    }

    public sealed class GameInput : MonoBehaviour
    {
        public GamePresenter Presenter;
        public VirtualStick Stick;
        public HoldCommand AttackHold;
        GameApp app;
        void Start() { app = GetComponent<GameApp>(); }
        void Update()
        {
            if (app == null || Presenter == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Presenter.ToggleMenu();
            if (app.Mode == "Title" || app.Mode == "Result" || app.Paused || app.Dead) { app.Move(0,0); return; }
            Vector2 value = Stick != null ? Stick.Value : Vector2.zero;
            Vector2 keys = new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),
                (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
            if (keys.sqrMagnitude > .01f) value = keys.normalized;
            app.Move(value.x, value.y);
            if (Input.GetKey(KeyCode.J) || (AttackHold != null && AttackHold.Held)) app.Attack();
            if (Input.GetKeyDown(KeyCode.Alpha1)) app.Cast(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) app.Cast(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) app.Cast(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) app.Cast(3);
            if (Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift)) app.Dodge();
            if (Input.GetKeyDown(KeyCode.Q)) app.UsePotion(true);
            if (Input.GetKeyDown(KeyCode.F)) app.UsePotion(false);
            if (Input.GetKeyDown(KeyCode.T)) app.ToggleAuto();
            if (Input.GetKeyDown(KeyCode.E)) app.Interact();
        }
        public void ResetInput() { if(Stick) Stick.ResetInput(); if(AttackHold) AttackHold.ResetInput(); if(app != null) app.Move(0,0); }
        void OnApplicationFocus(bool focus) { if(!focus) { ResetInput(); if(Presenter) Presenter.PauseOnFocusLoss(); } }
    }
}
