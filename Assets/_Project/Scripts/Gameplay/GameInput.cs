using UnityEngine;

public static class GameInput
{
    public const string GameplayHorizontalAxis = "Horizontal";
    public const string GameplayVerticalAxis = "Vertical";

    private static readonly KeyCode[] KeyboardConfirmKeys =
    {
        KeyCode.Space,
        KeyCode.Return,
        KeyCode.KeypadEnter
    };

    private static readonly KeyCode[] GamepadConfirmKeys =
    {
        KeyCode.JoystickButton0
    };

    private static readonly KeyCode[] AlternateConfirmKeys =
    {
        KeyCode.JoystickButton2,
        KeyCode.JoystickButton3
    };

    private static readonly KeyCode[] KeyboardActiveItemKeys =
    {
        KeyCode.Space
    };

    private static readonly KeyCode[] GamepadActiveItemKeys =
    {
        KeyCode.JoystickButton4
    };

    private static readonly KeyCode[] BackKeys =
    {
        KeyCode.Escape,
        KeyCode.JoystickButton1
    };

    private static readonly KeyCode[] PauseKeys =
    {
        KeyCode.Escape,
        KeyCode.JoystickButton7,
        KeyCode.JoystickButton6
    };

    private static readonly KeyCode[] UIUpKeys =
    {
        KeyCode.W,
        KeyCode.UpArrow
    };

    private static readonly KeyCode[] UIDownKeys =
    {
        KeyCode.S,
        KeyCode.DownArrow
    };

    private static readonly KeyCode[] UILeftKeys =
    {
        KeyCode.A,
        KeyCode.LeftArrow
    };

    private static readonly KeyCode[] UIRightKeys =
    {
        KeyCode.D,
        KeyCode.RightArrow
    };

    public static float GetHorizontalAxisRaw()
    {
        return Input.GetAxisRaw(GameplayHorizontalAxis);
    }

    public static float GetVerticalAxisRaw()
    {
        return Input.GetAxisRaw(GameplayVerticalAxis);
    }

    public static Vector2 ReadGameplayMoveInput()
    {
        return Vector2.ClampMagnitude(
            new Vector2(GetHorizontalAxisRaw(), GetVerticalAxisRaw()),
            1f);
    }

    public static bool IsConfirmPressed()
    {
        return IsKeyboardConfirmPressed() || IsGamepadConfirmPressed();
    }

    public static bool IsKeyboardConfirmPressed()
    {
        return IsAnyPressed(KeyboardConfirmKeys);
    }

    public static bool IsGamepadConfirmPressed()
    {
        return IsAnyPressed(GamepadConfirmKeys);
    }

    public static bool IsAlternateConfirmPressed()
    {
        return IsAnyPressed(AlternateConfirmKeys);
    }

    public static bool IsBackPressed()
    {
        return IsAnyPressed(BackKeys);
    }

    public static bool IsPausePressed()
    {
        return IsAnyPressed(PauseKeys);
    }

    public static bool IsContinuePressed()
    {
        return IsConfirmPressed() || IsAlternateConfirmPressed() || IsPausePressed();
    }

    public static bool IsActiveItemPressed()
    {
        return IsKeyboardActiveItemPressed() || IsGamepadActiveItemPressed();
    }

    public static bool IsKeyboardActiveItemPressed()
    {
        return IsAnyPressed(KeyboardActiveItemKeys);
    }

    public static bool IsGamepadActiveItemPressed()
    {
        return IsAnyPressed(GamepadActiveItemKeys);
    }

    public static bool IsUIUpPressed()
    {
        return IsAnyPressed(UIUpKeys);
    }

    public static bool IsUIDownPressed()
    {
        return IsAnyPressed(UIDownKeys);
    }

    public static bool IsUILeftPressed()
    {
        return IsAnyPressed(UILeftKeys);
    }

    public static bool IsUIRightPressed()
    {
        return IsAnyPressed(UIRightKeys);
    }

    public static bool IsAnyUIKeyboardDirectionHeld()
    {
        return IsAnyHeld(UIUpKeys) ||
               IsAnyHeld(UIDownKeys) ||
               IsAnyHeld(UILeftKeys) ||
               IsAnyHeld(UIRightKeys);
    }

    private static bool IsAnyPressed(KeyCode[] keys)
    {
        if (keys == null)
            return false;

        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
                return true;
        }

        return false;
    }

    private static bool IsAnyHeld(KeyCode[] keys)
    {
        if (keys == null)
            return false;

        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKey(keys[i]))
                return true;
        }

        return false;
    }
}
