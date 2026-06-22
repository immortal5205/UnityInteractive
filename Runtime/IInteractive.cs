using System;
using UnityEngine.EventSystems;

namespace NuoYan.Interactive
{
    /// <summary>
    /// 使用Unity自身的EventSystem来处理交互事件
    /// </summary>
    public interface IInteractive : IEventSystemHandler
    {
        public Type InteractableType { get; }
        public bool EnableInteractive { get; set; }
        public string Name { get => this.GetType().Name; }
    }
    public interface IDraggable : IInteractive, IBeginDragHandler, IDragHandler, IEndDragHandler { }
    public interface IFocusable : IInteractive, IPointerEnterHandler, IPointerExitHandler { }
    public interface ISelectable : IInteractive, IPointerClickHandler { }
    public interface ILongPressHandler : IInteractive, IPointerDownHandler, IPointerUpHandler
    {
        void OnBeginLongPress(PointerEventData eventData);
        void OnLongPress(PointerEventData eventData);
        void OnEndLongPress(PointerEventData eventData);
    }
    public struct InteractContext
    {
        public IFocusable Focusable;
        public IDraggable Draggable;
        public ILongPressHandler LongPressHandler;
        public InteractContext(IFocusable focusable, IDraggable dragable, ILongPressHandler longPressHandler)
        {
            this.Focusable = focusable;
            this.Draggable = dragable;
            this.LongPressHandler = longPressHandler;
        }
    }
    /// <summary>
    /// 交互情景接口
    /// </summary>

    public interface IInteractCase
    {
        public Type Subject { get; }
        public Type Target { get; }
        public bool Enable { get; set; }
        /// <summary>初始匹配优先级（来自 InteractCaseAttribute.Order），供调试/Editor 查看。</summary>
        public int Order { get; set; }
        /// <summary>
        /// 执行交互情景
        /// </summary>
        /// <param name="focusable">当前聚焦对象</param>
        /// <param name="dragable">当前拖拽对象</param>
        /// <returns>是否命中该交互情景</returns>
        public bool Execute(InteractContext context);
    }
}
