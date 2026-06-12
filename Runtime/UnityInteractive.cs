using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NuoYan.Interactive
{
    /// <summary>
    /// 基于Unity自身的EventSystem交互系统
    /// </summary>
    public sealed class UnityInteractive : MonoBehaviour
    {
        private static UnityInteractive m_Instance;
        private bool m_IsInitialized;
        public static UnityInteractive Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = FindObjectOfType<UnityInteractive>();
                    if (m_Instance == null)
                    {
                        GameObject go = new GameObject("[UnityInteractive]");
                        m_Instance = go.AddComponent<UnityInteractive>();
                        DontDestroyOnLoad(go);
                    }
                    m_Instance.InitOnce();
                }
                return m_Instance;
            }
        }
        public Dictionary<Type, IInteractCase> AllInteractCase { get; private set; }
        private LinkedList<IInteractCase> m_ActiveInteractCases;
        public IInteractCase CurrentInteractCase { get; private set; }
        public IDraggable CurrentDraggable { get; private set; }
        public IFocusable CurrentFocusable { get; private set; }

        private void InitOnce()
        {
            if (m_IsInitialized) return;
            m_IsInitialized = true;
            AllInteractCase = new Dictionary<Type, IInteractCase>();
            m_ActiveInteractCases = new LinkedList<IInteractCase>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                List<Type> types = assembly.GetTypes()
                    .Where(type => typeof(IInteractCase).IsAssignableFrom(type) && !type.IsAbstract)
                    .ToList();

                foreach (var type in types)
                {
                    InteractCaseAttribute attribute = type.GetCustomAttribute<InteractCaseAttribute>();
                    if (attribute == null) continue;

                    IInteractCase interactCase = Activator.CreateInstance(type, attribute.InteractSubject, attribute.InteractTarget) as IInteractCase; //通过反射获得交互案例实例，通过构造函数注入交互对象
                    if (interactCase == null) continue;

                    AllInteractCase.Add(type, interactCase);
                    interactCase.Enable = attribute.EnableExecuteOnLoad;
                    m_ActiveInteractCases.AddLast(interactCase);
                }
            }
        }

        void Update()
        {
            // 清除已销毁对象的悬挂引用
            if (!IsValid(CurrentDraggable)) SetCurrentDraggable(null);
            if (!IsValid(CurrentFocusable)) SetCurrentFocusable(null);
            // 执行交互案例（命中首个即停止遍历）
            IInteractCase activeCase = null;
            foreach (var item in m_ActiveInteractCases)
            {
                if (item.Enable && item.Execute(CurrentFocusable, CurrentDraggable))
                {
                    activeCase = item;
                    break;
                }
            }

            // 更新最近活跃的交互案例
            if (activeCase != null && activeCase != CurrentInteractCase)
            {
                CurrentInteractCase = activeCase;
                m_ActiveInteractCases.Remove(CurrentInteractCase);
                m_ActiveInteractCases.AddFirst(CurrentInteractCase);
            }

            // 交互案例处理完后清理拖拽状态
            if (CurrentDraggable != null && EasyInput.PointerUp())
            {
                SetCurrentDraggable(null);
            }
        }

        private static bool IsValid(IDraggable obj)
        {
            return obj == null || (obj as MonoBehaviour) != null;
        }

        private static bool IsValid(IFocusable obj)
        {
            return obj == null || (obj as MonoBehaviour) != null;
        }

        /// <summary>
        /// 运行时注册交互案例
        /// </summary>
        public void RegisterInteractCase(IInteractCase interactCase, bool enable = true)
        {
            var type = interactCase.GetType();
            if (AllInteractCase.ContainsKey(type)) return;
            AllInteractCase.Add(type, interactCase);
            interactCase.Enable = enable;
            m_ActiveInteractCases.AddLast(interactCase);
        }

        /// <summary>
        /// 运行时注销交互案例
        /// </summary>
        public void UnregisterInteractCase<T>() where T : IInteractCase
        {
            var type = typeof(T);
            if (AllInteractCase.TryGetValue(type, out var case_))
            {
                m_ActiveInteractCases.Remove(case_);
                AllInteractCase.Remove(type);
            }
        }
        /// <summary>
        /// 运行时禁用交互案例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void DisableInteractCase<T>() where T : IInteractCase
        {
            var type = typeof(T);
            if (AllInteractCase.TryGetValue(type, out var case_))
            {
                case_.Enable = false;
            }
        }

        /// <summary>
        /// 运行时启用交互案例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void EnableInteractCase<T>() where T : IInteractCase
        {
            var type = typeof(T);
            if (AllInteractCase.TryGetValue(type, out var case_))
            {
                case_.Enable = true;
            }
        }
        /// <summary>
        /// 设置当前拖拽对象
        /// </summary>
        /// <param name="dragable"></param>
        public void SetCurrentDraggable(IDraggable dragable)
        {
            CurrentDraggable = dragable;
        }
        /// <summary>
        /// 设置当前焦点对象
        /// </summary>
        /// <param name="focusable"></param>
        public void SetCurrentFocusable(IFocusable focusable)
        {
            CurrentFocusable = focusable;
        }

        private void OnDestroy()
        {
            CurrentDraggable = null;
            CurrentFocusable = null;
            CurrentInteractCase = null;
            AllInteractCase?.Clear();
            m_ActiveInteractCases?.Clear();
            m_Instance = null;
        }
    }
}
