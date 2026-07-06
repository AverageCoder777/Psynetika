using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Поля-ключи Blackboard: обычный текстфилд + кнопка-дропдаун со всеми ключами,
// которые уже встречаются в графе этого ассета, — вместо ручного набора по памяти.
[CustomPropertyDrawer(typeof(BlackboardKeyInputAttribute))]
[CustomPropertyDrawer(typeof(BlackboardKeyOutputAttribute))]
public class BlackboardKeyDrawer : PropertyDrawer
{
    private const float PickerWidth = 20f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            // Нельзя звать PropertyField на это же свойство — зациклит дровер; рисуем без атрибута.
            EditorGUI.LabelField(position, label.text, "Атрибут ключа применим только к string.");
            return;
        }

        label = EditorGUI.BeginProperty(position, label, property);

        Rect fieldRect = new Rect(position.x, position.y, position.width - PickerWidth - 2f, position.height);
        Rect pickerRect = new Rect(position.xMax - PickerWidth, position.y, PickerWidth, position.height);

        EditorGUI.BeginChangeCheck();
        string newValue = EditorGUI.TextField(fieldRect, label, property.stringValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = newValue;
        }

        if (GUI.Button(pickerRect, GUIContent.none, EditorStyles.popup))
        {
            List<string> keys = AbilityGraphUtility.CollectAllKeys(property.serializedObject.targetObject);
            GenericMenu menu = new GenericMenu();

            if (keys.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("В графе пока нет ключей"));
            }

            SerializedObject owner = property.serializedObject;
            string path = property.propertyPath;
            foreach (string key in keys)
            {
                menu.AddItem(new GUIContent(key), key == property.stringValue, () =>
                {
                    owner.Update();
                    owner.FindProperty(path).stringValue = key;
                    owner.ApplyModifiedProperties();
                });
            }

            menu.DropDown(pickerRect);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
