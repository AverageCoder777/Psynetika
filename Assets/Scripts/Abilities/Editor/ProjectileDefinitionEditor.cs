using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProjectileDefinition))]
public class ProjectileDefinitionEditor : Editor
{
    private SerializedProperty onSpawnProperty;
    private SerializedProperty onHitProperty;
    private SerializedProperty onExpireProperty;

    private void OnEnable()
    {
        onSpawnProperty = serializedObject.FindProperty("onSpawn");
        onHitProperty = serializedObject.FindProperty("onHit");
        onExpireProperty = serializedObject.FindProperty("onExpire");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "onSpawn", "onHit", "onExpire", "m_Script");

        EditorGUILayout.Space(10);
        AbilityNodeListGUI.Draw(onSpawnProperty, "При спавне (onSpawn)");
        EditorGUILayout.Space(6);
        AbilityNodeListGUI.Draw(onHitProperty, "При попадании (onHit)");
        EditorGUILayout.Space(6);
        AbilityNodeListGUI.Draw(onExpireProperty, "По истечении времени (onExpire)");

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
