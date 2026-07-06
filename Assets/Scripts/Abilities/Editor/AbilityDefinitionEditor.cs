using System.Collections.Generic;
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

        DrawPropertiesExcluding(serializedObject, "root", "m_Script");

        EditorGUILayout.Space(10);
        AbilityNodeListGUI.Draw(rootProperty, "Ноды способности");

        serializedObject.ApplyModifiedProperties();

        DrawValidation();
    }

    private void DrawValidation()
    {
        List<string> problems = AbilityGraphUtility.Validate(target);
        if (problems.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space(6);
        foreach (string problem in problems)
        {
            EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }
    }
}
