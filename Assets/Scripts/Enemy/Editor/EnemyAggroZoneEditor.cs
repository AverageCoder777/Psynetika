using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

// Зона агро растягивается за края и перетаскивается за центр прямо в Scene View.
[CustomEditor(typeof(EnemyAggroZone))]
public class EnemyAggroZoneEditor : Editor
{
    private const string UndoLabel = "Зона агро";
    private static readonly Color ZoneColor = new Color(1f, 0.45f, 0.2f);
    private static readonly Color FillColor = new Color(1f, 0.45f, 0.2f, 0.06f);

    private readonly BoxBoundsHandle boundsHandle = new()
    {
        axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Края зоны тянутся мышкой в Scene View, квадрат в центре двигает всю зону. Игрок внутри — враг " +
            "его замечает; из зоны враг не выходит. Зона удара остаётся прежней.",
            MessageType.Info);
    }

    private void OnSceneGUI()
    {
        EnemyAggroZone zone = (EnemyAggroZone)target;
        Rect rect = zone.WorldRect;
        Vector2 origin = zone.Origin;

        if (Event.current.type == EventType.Repaint)
        {
            Handles.DrawSolidRectangleWithOutline(rect, FillColor, Color.clear);
        }

        boundsHandle.center = rect.center;
        boundsHandle.size = rect.size;
        boundsHandle.handleColor = ZoneColor;
        boundsHandle.wireframeColor = ZoneColor;

        EditorGUI.BeginChangeCheck();
        boundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(zone, UndoLabel);
            zone.center = (Vector2)boundsHandle.center - origin;
            zone.size = boundsHandle.size;
            EditorUtility.SetDirty(zone);
        }

        using (new Handles.DrawingScope(ZoneColor))
        {
            Vector3 centerWorld = rect.center;
            float size = HandleUtility.GetHandleSize(centerWorld) * 0.08f;

            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.FreeMoveHandle(centerWorld, size, Vector3.zero, Handles.RectangleHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(zone, UndoLabel);
                zone.center = (Vector2)moved - origin;
                EditorUtility.SetDirty(zone);
            }
        }
    }
}
