using UnityEditor;
using UnityEngine;

// Точки маршрута патруля перетаскиваются мышкой прямо в Scene View.
[CustomEditor(typeof(EnemyPatrolPath))]
public class EnemyPatrolPathEditor : Editor
{
    private const string UndoLabel = "Маршрут патруля";
    private static readonly Color PointColor = new Color(0.3f, 0.85f, 1f);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnemyPatrolPath path = (EnemyPatrolPath)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Точки двигаются мышкой в Scene View. Пока здесь есть хотя бы одна точка, в покое враг ходит " +
            "по маршруту вместо тактики Idle из конфига. Одна точка — дойти до неё и стоять.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Добавить точку"))
            {
                Undo.RecordObject(path, UndoLabel);
                Vector2 last = path.HasPoints ? path.points[path.points.Count - 1] : Vector2.zero;
                path.points.Add(last + Vector2.right * 2f);
                EditorUtility.SetDirty(path);
            }

            using (new EditorGUI.DisabledScope(!path.HasPoints))
            {
                if (GUILayout.Button("Удалить последнюю"))
                {
                    Undo.RecordObject(path, UndoLabel);
                    path.points.RemoveAt(path.points.Count - 1);
                    EditorUtility.SetDirty(path);
                }
            }
        }
    }

    private void OnSceneGUI()
    {
        EnemyPatrolPath path = (EnemyPatrolPath)target;
        if (!path.HasPoints) return;

        using (new Handles.DrawingScope(PointColor))
        {
            for (int i = 0; i < path.points.Count; i++)
            {
                Vector3 world = path.GetWorldPoint(i);
                float size = HandleUtility.GetHandleSize(world) * 0.08f;

                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.DotHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(path, UndoLabel);
                    path.SetWorldPoint(i, moved);
                    EditorUtility.SetDirty(path);
                }

                Handles.Label(world + Vector3.right * size * 2f, i.ToString());
            }
        }
    }
}
