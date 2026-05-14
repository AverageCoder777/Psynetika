using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AbilityDefinition))]
public class AbilityDefinitionEditor : Editor
{
    private SerializedProperty rootProperty;

    private void OnEnable()
    {
        rootProperty = serializedObject.FindProperty("root");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "root");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Ability Nodes", EditorStyles.boldLabel);

        DrawNodes();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("+ Add Node"))
        {
            ShowAddNodeMenu();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawNodes()
    {
        if (rootProperty == null)
        {
            EditorGUILayout.HelpBox("Поле root не найдено.", MessageType.Error);
            return;
        }

        for (int i = 0; i < rootProperty.arraySize; i++)
        {
            SerializedProperty element = rootProperty.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            string nodeName = element.managedReferenceValue != null
                ? element.managedReferenceValue.GetType().Name
                : "Null Node";

            EditorGUILayout.LabelField(nodeName, EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                rootProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(element, true);
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }
    }

    private void ShowAddNodeMenu()
    {
        GenericMenu menu = new GenericMenu();

        Type baseType = typeof(AbilityNode);

        Type[] nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(type =>
                baseType.IsAssignableFrom(type) &&
                !type.IsAbstract &&
                !type.IsInterface)
            .ToArray();

        foreach (Type type in nodeTypes)
        {
            menu.AddItem(new GUIContent(type.Name), false, () =>
            {
                AddNode(type);
            });
        }

        if (nodeTypes.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("No AbilityNode types found"));
        }

        menu.ShowAsContext();
    }

    private void AddNode(Type type)
    {
        serializedObject.Update();

        rootProperty.arraySize++;

        SerializedProperty element = rootProperty.GetArrayElementAtIndex(rootProperty.arraySize - 1);
        element.managedReferenceValue = Activator.CreateInstance(type);

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(target);
    }
}