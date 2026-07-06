using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Отрисовка списка [SerializeReference] AbilityNode в инспекторе:
// заголовок с именем и summary, сворачивание, перемещение, дублирование,
// удаление и категоризированное меню добавления.
public static class AbilityNodeListGUI
{
    public static void Draw(SerializedProperty listProperty, string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (listProperty == null || !listProperty.isArray)
        {
            EditorGUILayout.HelpBox("Свойство списка нод не найдено.", MessageType.Error);
            return;
        }

        for (int i = 0; i < listProperty.arraySize; i++)
        {
            DrawElement(listProperty, i);
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("+ Добавить ноду"))
        {
            ShowAddNodeMenu(listProperty);
        }
    }

    private static void DrawElement(SerializedProperty listProperty, int index)
    {
        SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
        object node = element.managedReferenceValue;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        string displayName = node != null
            ? AbilityGraphUtility.GetDisplayName(node.GetType())
            : "Пустая нода";
        // EditorGUILayout.Foldout забирает всю ширину строки и ломает горизонтальную
        // раскладку, поэтому фолдаут — через Toggle со стилем foldout.
        element.isExpanded = GUILayout.Toggle(element.isExpanded, displayName, EditorStyles.foldout, GUILayout.ExpandWidth(false));

        string summary = AbilityGraphUtility.GetSummary(node as AbilityNode);
        GUIStyle summaryStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
        GUILayout.Label(summary ?? string.Empty, summaryStyle, GUILayout.ExpandWidth(true));

        using (new EditorGUI.DisabledScope(index == 0))
        {
            if (GUILayout.Button("▲", GUILayout.Width(22)))
            {
                listProperty.MoveArrayElement(index, index - 1);
            }
        }

        using (new EditorGUI.DisabledScope(index == listProperty.arraySize - 1))
        {
            if (GUILayout.Button("▼", GUILayout.Width(22)))
            {
                listProperty.MoveArrayElement(index, index + 1);
            }
        }

        if (GUILayout.Button("⧉", GUILayout.Width(22)))
        {
            element.DuplicateCommand();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        if (GUILayout.Button("✕", GUILayout.Width(22)))
        {
            listProperty.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.EndHorizontal();

        if (element.isExpanded && node != null)
        {
            EditorGUI.indentLevel++;
            DrawChildren(element);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    // Рисуем поля ноды напрямую, без popup-строки SubclassSelector —
    // тип уже показан в заголовке элемента.
    private static void DrawChildren(SerializedProperty element)
    {
        SerializedProperty iterator = element.Copy();
        SerializedProperty end = element.GetEndProperty();

        if (!iterator.NextVisible(true))
        {
            return;
        }

        while (!SerializedProperty.EqualContents(iterator, end))
        {
            EditorGUILayout.PropertyField(iterator, true);
            if (!iterator.NextVisible(false))
            {
                break;
            }
        }
    }

    private static void ShowAddNodeMenu(SerializedProperty listProperty)
    {
        GenericMenu menu = new GenericMenu();
        SerializedObject owner = listProperty.serializedObject;
        string listPath = listProperty.propertyPath;

        foreach (Type type in AbilityGraphUtility.GetNodeTypes())
        {
            Type nodeType = type;
            menu.AddItem(new GUIContent(AbilityGraphUtility.GetMenuPath(type)), false, () =>
            {
                owner.Update();
                SerializedProperty list = owner.FindProperty(listPath);
                list.arraySize++;
                SerializedProperty added = list.GetArrayElementAtIndex(list.arraySize - 1);
                added.managedReferenceValue = Activator.CreateInstance(nodeType);
                added.isExpanded = true;
                owner.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }
}
