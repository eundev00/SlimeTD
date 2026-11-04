using UnityEngine;
using UnityEngine.InputSystem;

namespace EpicToonFX
{
    public static class ETFXInput
    {
        public static bool KeyDown(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        public static bool KeyUp(Key key) => Keyboard.current != null && Keyboard.current[key].wasReleasedThisFrame;

        public static bool LeftDown => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool LeftHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool LeftUp => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        public static bool RightHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;

        public static Vector3 MousePosition => Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;

        // 구 Input의 Mouse X/Y 축은 픽셀 이동량 x 0.1이다
        public static Vector2 MouseAxis => Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.1f : Vector2.zero;

        // 구 Input의 Mouse ScrollWheel은 한 칸에 0.1, Input System은 한 칸에 120이다
        public static float ScrollWheel => Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 1200f : 0f;
    }
}
