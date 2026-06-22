using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuoYan.Interactive
{
    /// <summary>
    /// 交互组件基类。基于 Unity EventSystem 接收事件，转成受保护的虚方法供子类重写。
    /// 拖拽(Drag) / 长按(Press) 互斥
    /// </summary>
    public class InteractiveComponent : MonoBehaviour, IDraggable, IFocusable, ISelectable, ILongPressHandler
    {
        public const string Drag = "Drag";//拖拽
        public const string Focus = "Focus";//焦点
        public const string Press = "Press";//长按

        [SerializeField] private bool m_EnableInteractive = true;
        [SerializeField] private bool m_EnableDrag = true;
        [SerializeField] private bool m_EnableLongPress = true;
        public Type InteractableType => this.GetType();
        /// <summary>
        /// 是否启用自身的交互逻
        /// </summary>
        /// <value></value>
        public virtual bool EnableInteractive { get => m_EnableInteractive; set => m_EnableInteractive = value; }
        public virtual bool EnableDrag { get => m_EnableDrag; set => m_EnableDrag = value; }
        public virtual bool EnableLongPress { get => m_EnableLongPress; set => m_EnableLongPress = value; }


        // 交互事件全部在主线程派发，无需并发容器
        private readonly Dictionary<string, bool> m_InteractState = new Dictionary<string, bool>();

        // 长按计时（自包含，不依赖 UnityInteractive 的 Update）
        private bool m_Pressing;
        private float m_PressTime;
        private bool m_LongPressFired;
        private float m_NextPressTime; // 下次 OnPress 触发时间（按 LongPressInterval 节流）
        private bool m_SuppressNextClick; // 长按结束后抑制紧随的 click
        private Coroutine m_PressCoroutine; // 仅按下时运行，无按下时零 Update 开销
        private PointerEventData m_PressEventData;
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (EnableInteractive && EnableDrag && !GetInteractState(Press))
            {
                CancelLongPress(); // 按下未到阈值即开始拖拽，取消长按计时
                UnityInteractive.Instance.SetCurrentDraggable(this);
                SetState(Drag, true);
                OnStartDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (EnableInteractive && EnableDrag && !GetInteractState(Press))
            {
                OnUpdateDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (EnableInteractive && EnableDrag && !GetInteractState(Press))
            {
                SetState(Drag, false);
                OnStopDrag(eventData);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 长按刚结束，抑制随之而来的 click，避免与长按重复触发
            if (m_SuppressNextClick)
            {
                m_SuppressNextClick = false;
                return;
            }
            if (EnableInteractive)
            {
                OnSelect(eventData);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EnableInteractive)
            {
                UnityInteractive.Instance.SetCurrentFocusable(this);
                SetState(Focus, true);
                OnFocus(eventData);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (EnableInteractive)
            {
                OnLostFocus(eventData);
                SetState(Focus, false);
                UnityInteractive.Instance.SetCurrentFocusable(null);
            }
        }

        protected virtual void OnSelect(PointerEventData eventData) { }
        protected virtual void OnStartDrag(PointerEventData eventData) { }
        protected virtual void OnUpdateDrag(PointerEventData eventData) { }
        protected virtual void OnStopDrag(PointerEventData eventData) { }
        protected virtual void OnFocus(PointerEventData eventData) { }
        protected virtual void OnLostFocus(PointerEventData eventData) { }
        protected virtual void OnStartPress(PointerEventData eventData) { }
        protected virtual void OnPress(PointerEventData eventData) { }
        protected virtual void OnStopPress(PointerEventData eventData) { }

        /// <summary>
        /// 获取交互状态。所有 key 都为 true 才返回 true。Drag 与 Press 互斥。
        /// </summary>
        /// <param name="keys">"Drag","Focus","Press"</param>
        public bool GetInteractState(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!m_InteractState.TryGetValue(key, out var value) || !value)
                    return false;
            }
            return true;
        }

        private void SetState(string key, bool value)
        {
            m_InteractState[key] = value;
        }

        #region 长按（自包含计时）

        public void OnBeginLongPress(PointerEventData eventData)
        {
            if (EnableInteractive && EnableLongPress && !GetInteractState(Drag))
            {
                SetState(Press, true);
                m_LongPressFired = true;
                OnStartPress(eventData);
            }
        }

        public void OnLongPress(PointerEventData eventData)
        {
            if (EnableInteractive && EnableLongPress && !GetInteractState(Drag))
            {
                OnPress(eventData);
            }
        }

        public void OnEndLongPress(PointerEventData eventData)
        {
            if (EnableInteractive && EnableLongPress && !GetInteractState(Drag))
            {
                OnStopPress(eventData);
                SetState(Press, false);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!EnableInteractive) return;
            m_Pressing = true;
            m_PressTime = 0f;
            m_LongPressFired = false;
            m_SuppressNextClick = false;
            m_PressEventData = eventData;
            UnityInteractive.Instance.SetCurrentLongPress(this);
            if (m_PressCoroutine == null)
            {
                m_PressCoroutine = StartCoroutine(LongPressRoutine());
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!m_Pressing) return;
            StopLongPressRoutine();
            m_PressEventData = eventData;
            if (m_LongPressFired)
            {
                OnEndLongPress(m_PressEventData);
                m_SuppressNextClick = true; // 抑制紧随的 click
            }
            m_Pressing = false;
            m_LongPressFired = false;
            m_PressTime = 0f;
            UnityInteractive.Instance.SetCurrentLongPress(null);
        }

        /// <summary>
        /// 长按计时协程：仅在被按下期间运行，无按下时本组件零 Update 开销。
        /// 用协程替代 Update，避免大量 InteractiveComponent 实例每帧空跑。
        /// </summary>
        private IEnumerator LongPressRoutine()
        {
            while (m_Pressing)
            {
                yield return null;
                if (!m_Pressing) break;
                if (GetInteractState(Drag)) continue; // 拖拽中不累加（Drag 与 Press 互斥）
                m_PressTime += Time.deltaTime;
                if (!m_LongPressFired && m_PressTime >= UnityInteractive.Instance.LongPressThresholdTime)
                {
                    OnBeginLongPress(m_PressEventData);
                    // 仅 guard 通过（m_LongPressFired 已置 true）才首次 OnPress + 节流
                    if (m_LongPressFired)
                    {
                        OnLongPress(m_PressEventData);
                        m_NextPressTime = m_PressTime + UnityInteractive.Instance.LongPressInterval;
                    }
                }
                else if (m_LongPressFired)
                {
                    // LongPressInterval <= 0 → 每帧调用；否则按间隔节流
                    if (UnityInteractive.Instance.LongPressInterval <= 0f || m_PressTime >= m_NextPressTime)
                    {
                        OnLongPress(m_PressEventData);
                        if (UnityInteractive.Instance.LongPressInterval > 0f)
                            m_NextPressTime = m_PressTime + UnityInteractive.Instance.LongPressInterval;
                    }
                }
            }
            m_PressCoroutine = null;
        }

        private void StopLongPressRoutine()
        {
            if (m_PressCoroutine != null)
            {
                StopCoroutine(m_PressCoroutine);
                m_PressCoroutine = null;
            }
        }

        private void CancelLongPress()
        {
            StopLongPressRoutine();
            if (m_LongPressFired)
            {
                OnEndLongPress(m_PressEventData);
            }
            m_Pressing = false;
            m_LongPressFired = false;
            m_PressTime = 0f;
        }

        private void OnDisable()
        {
            // 组件禁用/销毁时清理自身状态，避免悬挂回调
            StopLongPressRoutine();
            if (m_Pressing && m_LongPressFired)
            {
                OnEndLongPress(m_PressEventData);
            }
            m_Pressing = false;
            m_LongPressFired = false;
            m_PressTime = 0f;
            m_InteractState[Drag] = false;
            m_InteractState[Press] = false;
            m_InteractState[Focus] = false;
            // 全局悬挂引用由 UnityInteractive.Update 的 IsValid 检查兜底
        }
        #endregion
    }
}
