using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace NuoYan.Interactive
{
    public static class EasyInput
    {
        /// <summary>新输入系统是否有可用设备</summary>
        private static bool HasNewInput => Mouse.current != null || Touchscreen.current != null;

        /// <summary>触摸屏设备存在且有活跃触摸</summary>
        private static bool HasActiveTouch()
        {
            if (Touchscreen.current == null) return false;
            foreach (var touch in Touchscreen.current.touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase != UnityEngine.InputSystem.TouchPhase.None)
                    return true;
            }
            return false;
        }

        public static bool PointerUp(int index, out int fingerId)
        {
            fingerId = 0;

            if (HasNewInput)
            {
                // 触摸屏存在且有活跃触摸 → 走触摸路径
                if (HasActiveTouch())
                {
                    var touches = Touchscreen.current.touches;
                    if (index < touches.Count)
                    {
                        var touch = touches[index];
                        fingerId = touch.touchId.ReadValue();
                        var phase = touch.phase.ReadValue();
                        return phase == UnityEngine.InputSystem.TouchPhase.Ended
                            || phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                    }
                    return false;
                }

                // 无活跃触摸 → 走鼠标路径
                if (Mouse.current != null)
                    return Mouse.current.leftButton.wasReleasedThisFrame;

                return false;
            }

            // Fallback: 旧输入系统
            return Input.GetMouseButtonUp(0);
        }

        public static bool PointerUp()
        {
            return PointerUp(0, out _);
        }

        public static bool PointerDown(int index, out int fingerId, out Vector2 pos)
        {
            fingerId = -1;
            pos = Vector2.zero;

            if (HasNewInput)
            {
                // 触摸屏存在且有活跃触摸 → 走触摸路径
                if (HasActiveTouch())
                {
                    var touches = Touchscreen.current.touches;
                    if (index < touches.Count)
                    {
                        var touch = touches[index];
                        fingerId = touch.touchId.ReadValue();
                        pos = touch.position.ReadValue();
                        return touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began;
                    }
                    return false;
                }

                // 无活跃触摸 → 走鼠标路径
                if (Mouse.current != null)
                {
                    fingerId = 0;
                    pos = Mouse.current.position.ReadValue();
                    return Mouse.current.leftButton.wasPressedThisFrame;
                }

                return false;
            }

            // Fallback: 旧输入系统
            fingerId = 0;
            pos = Input.mousePosition;
            return Input.GetMouseButtonDown(0);
        }

        public static bool PointerDown()
        {
            return PointerDown(0, out _, out _);
        }

        public static bool PointerMove(int index, out int fingerId, out Vector2 pos)
        {
            fingerId = -1;
            pos = Vector2.zero;

            if (HasNewInput)
            {
                // 触摸屏存在且有活跃触摸 → 走触摸路径
                if (HasActiveTouch())
                {
                    var touches = Touchscreen.current.touches;
                    if (index < touches.Count)
                    {
                        var touch = touches[index];
                        fingerId = touch.touchId.ReadValue();
                        pos = touch.position.ReadValue();
                        var phase = touch.phase.ReadValue();
                        return phase == UnityEngine.InputSystem.TouchPhase.Moved
                            || phase == UnityEngine.InputSystem.TouchPhase.Stationary;
                    }
                    return false;
                }

                // 无活跃触摸 → 走鼠标路径
                if (Mouse.current != null)
                {
                    fingerId = 0;
                    pos = Mouse.current.position.ReadValue();
                    return Mouse.current.leftButton.isPressed;
                }

                return false;
            }

            // Fallback: 旧输入系统
            fingerId = 0;
            pos = Input.mousePosition;
            return Input.GetMouseButton(0);
        }

        public static bool PointerMove()
        {
            return PointerMove(0, out _, out _);
        }

        private static System.Collections.Generic.List<RaycastResult> m_Results = new System.Collections.Generic.List<RaycastResult>();
        private static PointerEventData m_PointerEventData;

        public static bool TryGetCurrentPointRayCast(out GameObject go)
        {
            if (EventSystem.current == null)
            {
                go = null;
                return false;
            }

            if (EventSystem.current.IsPointerOverGameObject())
            {
                m_PointerEventData ??= new PointerEventData(EventSystem.current);
                m_PointerEventData.position = GetPointerPosition();

                m_Results.Clear();
                EventSystem.current.RaycastAll(m_PointerEventData, m_Results);

                if (m_Results.Count > 0)
                {
                    go = m_Results[0].gameObject;
                    return true;
                }
            }

            go = null;
            return false;
        }

        private static Vector2 GetPointerPosition()
        {
            if (HasNewInput)
            {
                if (HasActiveTouch()) return Touchscreen.current.primaryTouch.position.ReadValue();
                if (Mouse.current != null) return Mouse.current.position.ReadValue();
            }
            return Input.mousePosition;
        }
    }
}

