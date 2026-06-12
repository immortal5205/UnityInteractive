using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuoYan.Interactive
{
    public class InteractiveComponent : MonoBehaviour, IDraggable, IFocusable, ISelectable
    {
        public Type InteractableType => this.GetType();

        public virtual bool Enable { get; set; } = true;
        /// <summary>
        /// 当前是否正在拖拽
        /// </summary>
        public bool IsDragging => (UnityInteractive.Instance.CurrentDraggable as MonoBehaviour) == this;
        /// <summary>
        /// 当前是否获得焦点
        /// </summary>
        public bool IsFocused => (UnityInteractive.Instance.CurrentFocusable as MonoBehaviour) == this;
        /// <summary>
        /// 当前是否获得焦点或正在拖拽
        /// </summary>
        public bool IsFocusedOrDragging => IsFocused || IsDragging;
        /// <summary>
        /// 当前是否获得焦点且正在拖拽
        /// </summary>
        public bool IsFocusedAndDragging => IsFocused && IsDragging;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Enable)
            {
                UnityInteractive.Instance.SetCurrentDraggable(this);
                OnStartDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Enable)
            {
                OnUpdateDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Enable)
            {
                OnStopDrag(eventData);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Enable)
            {
                OnSelect(eventData);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Enable)
            {
                UnityInteractive.Instance.SetCurrentFocusable(this);
                OnFocus(eventData);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Enable)
            {
                OnLostFocus(eventData);
                UnityInteractive.Instance.SetCurrentFocusable(null);
            }
        }
        protected virtual void OnSelect(PointerEventData eventData) { }
        protected virtual void OnStartDrag(PointerEventData eventData) { }
        protected virtual void OnUpdateDrag(PointerEventData eventData) { }
        protected virtual void OnStopDrag(PointerEventData eventData) { }
        protected virtual void OnFocus(PointerEventData eventData) { }
        protected virtual void OnLostFocus(PointerEventData eventData) { }
    }
}
