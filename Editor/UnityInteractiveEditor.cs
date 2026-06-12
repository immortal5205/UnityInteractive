#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NuoYan.Interactive
{
    [CustomEditor(typeof(UnityInteractive))]
    public class UnityInteractiveEditor : Editor
    {
        private UnityInteractive m_Interactive;
        private Vector2 m_ScrollPos;

        private void OnEnable()
        {
            m_Interactive = target as UnityInteractive;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (m_Interactive != null && EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Space(10);

            DrawRuntimeState();
            EditorGUILayout.Space(10);
            DrawInteractCaseList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuntimeState()
        {
            EditorGUILayout.LabelField("运行时交互状态", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            DrawStateField("拖拽对象", m_Interactive.CurrentDraggable);
            DrawStateField("焦点对象", m_Interactive.CurrentFocusable);
            DrawStateField("活跃交互", m_Interactive.CurrentInteractCase);

            EditorGUI.indentLevel--;
        }

        private void DrawStateField(string label, object obj)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(rect, label, obj == null ? "无" : obj.GetType().Name);
        }

        private void DrawInteractCaseList()
        {
            EditorGUILayout.LabelField("交互情景列表", EditorStyles.boldLabel);

            if (m_Interactive.AllInteractCase == null || m_Interactive.AllInteractCase.Count == 0)
            {
                EditorGUILayout.HelpBox("没有已注册的交互情景", MessageType.Info);
                return;
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.MaxHeight(300));
            EditorGUI.indentLevel++;

            foreach (var kvp in m_Interactive.AllInteractCase)
            {
                var interactCase = kvp.Value;
                bool isCurrent = interactCase == m_Interactive.CurrentInteractCase;

                DrawInteractCase(interactCase, isCurrent);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndScrollView();
        }

        private void DrawInteractCase(IInteractCase interactCase, bool isCurrent)
        {
            var bgColor = GUI.backgroundColor;
            if (isCurrent) GUI.backgroundColor = Color.green;

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    // 启用开关
                    bool newEnable = EditorGUILayout.Toggle(interactCase.Enable, GUILayout.Width(16));
                    if (newEnable != interactCase.Enable)
                    {
                        interactCase.Enable = newEnable;
                        EditorUtility.SetDirty(target);
                    }

                    // 情景名
                    EditorGUILayout.LabelField(interactCase.GetType().Name, EditorStyles.boldLabel);

                    if (isCurrent) EditorGUILayout.LabelField("活跃中", GUILayout.Width(45));
                }
                EditorGUILayout.EndHorizontal();

                // 第二行：Subject → Target
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"主体: {interactCase.Subject?.Name ?? "any"}  →  目标: {interactCase.Target?.Name ?? "any"}");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = bgColor;
        }
    }
}
#endif