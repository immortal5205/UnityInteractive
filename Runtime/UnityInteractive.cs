using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NuoYan.Interactive
{
    /// <summary>
    /// 基于 Unity 自身 EventSystem 的交互系统管理器。
    /// 维护全局当前拖拽/焦点/长按对象，每帧用它们匹配 IInteractCase（首个命中即停，LRU 前移）。
    /// 长按计时已下放到 InteractiveComponent 自身，本类仅跟踪 CurrentLongPress 引用。
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
                    m_Instance = FindFirstObjectByType<UnityInteractive>();
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
        public ILongPressHandler CurrentLongPress { get; private set; }

        [Tooltip("进入长按的阈值时间（秒），由 InteractiveComponent 读取")]
        public float LongPressThresholdTime = 1.5f;
        /// <summary>长按触发后 OnPress 的调用间隔（秒）。<=0 表示每帧调用。</summary>
        [Tooltip("长按触发后 OnPress 的调用间隔（秒）。<=0 表示每帧调用。")]
        public float LongPressInterval = 1f;

        private void InitOnce()
        {
            if (m_IsInitialized) return;
            m_IsInitialized = true;
            AllInteractCase = new Dictionary<Type, IInteractCase>();
            m_ActiveInteractCases = new LinkedList<IInteractCase>();

            // 收集后按 Order 排序，保证初始匹配优先级确定性
            var collected = new List<(IInteractCase Case, int Order)>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    // 跳过加载失败的程序集，避免一个坏程序集拖垮整个初始化
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract || !typeof(IInteractCase).IsAssignableFrom(type)) continue;

                    InteractCaseAttribute attribute = type.GetCustomAttribute<InteractCaseAttribute>();
                    if (attribute == null) continue;

                    IInteractCase interactCase = Activator.CreateInstance(type, attribute.InteractSubject, attribute.InteractTarget) as IInteractCase;
                    if (interactCase == null) continue;

                    AllInteractCase.Add(type, interactCase);
                    interactCase.Enable = attribute.EnableExecuteOnLoad;
                    interactCase.Order = attribute.Order;
                    collected.Add((interactCase, attribute.Order));
                }
            }

            foreach (var item in collected.OrderBy(x => x.Order))
            {
                m_ActiveInteractCases.AddLast(item.Case);
            }
        }

        void Update()
        {
            // 清除已销毁对象的悬挂引用
            if (!IsValid(CurrentDraggable)) SetCurrentDraggable(null);
            if (!IsValid(CurrentFocusable)) SetCurrentFocusable(null);
            if (!IsValid(CurrentLongPress)) SetCurrentLongPress(null);

            // 执行交互案例（命中首个即停止遍历）
            IInteractCase activeCase = null;
            var context = new InteractContext(CurrentFocusable, CurrentDraggable, CurrentLongPress);
            foreach (var item in m_ActiveInteractCases)
            {
                if (item.Enable && item.Execute(context))
                {
                    activeCase = item;
                    break;
                }
            }

            // LRU：把最近活跃案例前移，下次优先匹配
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

        private static bool IsValid(IDraggable obj) => obj == null || (obj as MonoBehaviour) != null;
        private static bool IsValid(IFocusable obj) => obj == null || (obj as MonoBehaviour) != null;
        private static bool IsValid(ILongPressHandler obj) => obj == null || (obj as MonoBehaviour) != null;

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
        public void DisableInteractCase<T>() where T : IInteractCase
        {
            if (AllInteractCase.TryGetValue(typeof(T), out var case_))
                case_.Enable = false;
        }

        /// <summary>
        /// 运行时启用交互案例
        /// </summary>
        public void EnableInteractCase<T>() where T : IInteractCase
        {
            if (AllInteractCase.TryGetValue(typeof(T), out var case_))
                case_.Enable = true;
        }

        /// <summary>
        /// 设置当前拖拽对象
        /// </summary>
        public void SetCurrentDraggable(IDraggable dragable) => CurrentDraggable = dragable;

        /// <summary>
        /// 设置当前焦点对象
        /// </summary>
        public void SetCurrentFocusable(IFocusable focusable) => CurrentFocusable = focusable;

        /// <summary>
        /// 设置当前长按对象
        /// </summary>
        public void SetCurrentLongPress(ILongPressHandler focusable) => CurrentLongPress = focusable;

        private void OnDestroy()
        {
            CurrentDraggable = null;
            CurrentFocusable = null;
            CurrentLongPress = null;
            CurrentInteractCase = null;
            AllInteractCase?.Clear();
            m_ActiveInteractCases?.Clear();
            m_Instance = null;
        }
    }
}
