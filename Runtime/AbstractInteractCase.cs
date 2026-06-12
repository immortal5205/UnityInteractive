using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NuoYan.Interactive
{
    /// <summary>
    /// 交互情景标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InteractCaseAttribute : Attribute
    {
        public Type InteractSubject;
        public Type InteractTarget;
        public bool EnableExecuteOnLoad;

        public InteractCaseAttribute(Type subject, Type target, bool enableExecuteOnLoad = true)
        {
            InteractSubject = subject;
            InteractTarget = target;
            this.EnableExecuteOnLoad = enableExecuteOnLoad;
        }
    }

    /// <summary>
    /// 拖拽物体聚焦目标交互情景
    /// </summary>
    public abstract class DragSubjectFocusTargetInteractCase : IInteractCase
    {
        public Type Subject { get; set; }
        public Type Target { get; set; }
        public bool Enable { get; set; }

        private bool m_IsEnter = false;
        private bool m_IsExit = true;
        private int m_LastSubjectInstanceId = 0;
        private int m_LastTargetInstanceId = 0;

        public DragSubjectFocusTargetInteractCase(Type subject, Type target)
        {
            Subject = subject;
            Target = target;
        }

        protected abstract void OnExecute(IDraggable subject, IFocusable target);

        public bool Execute(IFocusable focusable, IDraggable dragable)
        {
            if (focusable == null || dragable == null
                || !Target.IsAssignableFrom(focusable.InteractableType)
                || !Subject.IsAssignableFrom(dragable.InteractableType))
            {
                if (m_IsEnter)
                {
                    m_IsEnter = false;
                    OnExit();
                    m_IsExit = true;
                    m_LastSubjectInstanceId = 0;
                    m_LastTargetInstanceId = 0;
                }
                return false;
            }

            int currentSubjectId = (dragable as MonoBehaviour)?.GetInstanceID() ?? 0;
            int currentTargetId = (focusable as MonoBehaviour)?.GetInstanceID() ?? 0;

            bool subjectChanged = currentSubjectId != m_LastSubjectInstanceId;
            bool targetChanged = currentTargetId != m_LastTargetInstanceId;
            bool anyObjectChanged = subjectChanged || targetChanged;

            if (m_IsEnter && anyObjectChanged)
            {
                m_IsEnter = false;
                OnExit();
                m_IsExit = true;
            }

            if (m_IsExit)
            {
                m_IsExit = false;
                OnEnter(dragable, focusable);
                m_IsEnter = true;

                m_LastSubjectInstanceId = currentSubjectId;
                m_LastTargetInstanceId = currentTargetId;
            }

            OnExecute(dragable, focusable);
            return true;
        }

        protected virtual void OnEnter(IDraggable subject, IFocusable target)
        {
        }

        protected virtual void OnExit()
        {
        }

        protected virtual bool EndDrag => EasyInput.PointerUp();
    }
}
