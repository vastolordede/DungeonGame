using UnityEngine;
using UnityEngine.InputSystem;

public class MouseController : MonoBehaviour
{
    void Start()
    {
        HideCursor();
    }

    void Update()
    {
        if (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed)
        {
            ShowCursor();
        }
        else
        {
            HideCursor();
        }
    }

    void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}