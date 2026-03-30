using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CursorInputGate : BaseInput
{
    [SerializeField] private bool pointerInputEnabled = true;
    [SerializeField] private bool touchInputEnabled = true;

    public bool PointerInputEnabled
    {
        get => pointerInputEnabled;
        set => pointerInputEnabled = value;
    }

    public bool TouchInputEnabled
    {
        get => touchInputEnabled;
        set => touchInputEnabled = value;
    }

    public override bool mousePresent => pointerInputEnabled && base.mousePresent;

    public override Vector2 mousePosition
    {
        get
        {
            if (pointerInputEnabled)
                return base.mousePosition;

            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
    }

    public override Vector2 mouseScrollDelta => pointerInputEnabled ? base.mouseScrollDelta : Vector2.zero;
    public override bool GetMouseButtonDown(int button) => pointerInputEnabled && base.GetMouseButtonDown(button);
    public override bool GetMouseButtonUp(int button) => pointerInputEnabled && base.GetMouseButtonUp(button);
    public override bool GetMouseButton(int button) => pointerInputEnabled && base.GetMouseButton(button);
    public override bool touchSupported => touchInputEnabled && base.touchSupported;
    public override int touchCount => touchInputEnabled ? base.touchCount : 0;

    public override Touch GetTouch(int index)
    {
        return touchInputEnabled ? base.GetTouch(index) : default(Touch);
    }
}
