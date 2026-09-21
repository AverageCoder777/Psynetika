using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/*
Отрисовка строковых полей с именами из настроек проекта: тег, слой физики, sorting layer.

Значение остаётся строкой (см. NameDropdownAttributes), но выбирается из списка, поэтому в
разметке локации не появится тега, которого нет в проекте. Если в поле уже лежит имя, которого
в проекте нет (наследие, переименовали тег), оно не затирается: строка показывается в списке
с пометкой, чтобы стало видно проблему.
*/
public static class NameDropdownGUI
{
    public static void Draw(Rect position, SerializedProperty property, GUIContent label, string[] names, string emptyLabel)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        using (new EditorGUI.PropertyScope(position, label, property))
        {
            string current = property.stringValue ?? string.Empty;
            List<string> options = new() { emptyLabel };
            options.AddRange(names);

            int selected = string.IsNullOrEmpty(current) ? 0 : options.IndexOf(current);

            if (selected < 0)
            {
                // Имени нет в проекте: показываем его отдельным пунктом, а не подменяем молча.
                options.Add($"{current} — нет в проекте");
                selected = options.Count - 1;
            }

            int picked = EditorGUI.Popup(position, label.text, selected, options.ToArray());

            if (picked != selected)
            {
                property.stringValue = picked == 0 ? string.Empty : options[picked];
            }
        }
    }

    public static string[] SortingLayerNames()
    {
        SortingLayer[] layers = SortingLayer.layers;
        string[] names = new string[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            names[i] = layers[i].name;
        }

        return names;
    }
}

[CustomPropertyDrawer(typeof(TagNameAttribute))]
public class TagNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        NameDropdownGUI.Draw(position, property, label, InternalEditorUtility.tags, "(без тега)");
    }
}

[CustomPropertyDrawer(typeof(PhysicsLayerNameAttribute))]
public class PhysicsLayerNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        NameDropdownGUI.Draw(position, property, label, InternalEditorUtility.layers, "(Default)");
    }
}

[CustomPropertyDrawer(typeof(SortingLayerNameAttribute))]
public class SortingLayerNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        NameDropdownGUI.Draw(position, property, label, NameDropdownGUI.SortingLayerNames(), "(Default)");
    }
}
