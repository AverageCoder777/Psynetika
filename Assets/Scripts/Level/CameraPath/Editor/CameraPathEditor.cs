using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/*
Редактор пути камеры: линия рисуется прямо в сцене, по ней собирается коридор.

Горячие клавиши в Scene View, пока объект пути выделен:
  перетаскивание точки   — двигать линию;
  Shift + клик           — добавить точку (в ближайший отрезок либо продолжением за конец);
  Ctrl  + клик           — удалить точку под курсором;
  оранжевые квадратики   — над и под каждой точкой: тянуть вверх/вниз, чтобы в этом месте
                           камера могла подняться выше (или опуститься ниже) линии.

Коридор пересобирается сразу после правки, если включено «Перестраивать сразу»: дизайнеру нужно
видеть настоящую фигуру конфайнера, а не только линию. Пересборка идёт мимо Undo — коридор целиком
производная от линии: отменяется сама линия, а коридор после отмены перестраивается заново.
*/
[CustomEditor(typeof(CameraPath))]
public class CameraPathEditor : Editor
{
    private const string AutoRebuildKey = "Psynetika.CameraPath.AutoRebuild";
    private const string UndoLabel = "Путь камеры";
    private const float PickDistance = 18f;

    private static readonly Color LineColor = new(0.3f, 0.9f, 1f, 1f);
    private static readonly Color CorridorColor = new(0.3f, 0.9f, 1f, 0.2f);
    private static readonly Color RangeColor = new(1f, 0.6f, 0.2f, 1f);

    private bool dragged;

    private static bool AutoRebuild
    {
        get => EditorPrefs.GetBool(AutoRebuildKey, true);
        set => EditorPrefs.SetBool(AutoRebuildKey, value);
    }

    [MenuItem("Psynetika/Путь камеры")]
    private static void CreateFromMenu()
    {
        Selection.activeGameObject = Create(null).gameObject;
    }

    /*
    Новый путь в активной сцене. Линию кладём по центру видимой области Scene View и во всю её
    ширину: дизайнеру остаётся потянуть концы, а не искать две точки в нуле координат.
    */
    public static CameraPath Create(CinemachineConfiner2D confiner)
    {
        GameObject created = new("Путь камеры");
        Undo.RegisterCreatedObjectUndo(created, UndoLabel);

        CameraPath path = Undo.AddComponent<CameraPath>(created);
        SceneView view = SceneView.lastActiveSceneView;

        Vector3 center = view != null ? view.pivot : Vector3.zero;
        float half = view != null ? Mathf.Max(view.size, 5f) : 10f;

        created.transform.position = new Vector3(center.x, center.y, 0f);
        path.nodes = new List<CameraPathPoint>
        {
            new(new Vector2(-half, 0f)),
            new(new Vector2(half, 0f))
        };
        path.confiner = confiner != null
            ? confiner
            : FindFirstObjectByType<CinemachineConfiner2D>(FindObjectsInactive.Include);

        EditorSceneManager.MarkSceneDirty(created.scene);

        return path;
    }

    public static void BuildWithUndo(CameraPath path)
    {
        if (path == null)
        {
            return;
        }

        if (path.confiner != null)
        {
            Undo.RecordObject(path.confiner, UndoLabel);
        }

        path.Build(
            created => Undo.RegisterCreatedObjectUndo(created, UndoLabel),
            destroying => Undo.DestroyObjectImmediate(destroying));

        if (path.confiner != null)
        {
            EditorUtility.SetDirty(path.confiner);
        }

        EditorSceneManager.MarkSceneDirty(path.gameObject.scene);
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();

        CameraPath path = (CameraPath)target;
        bool fromCamera = path.widthFromCamera && path.TryGetCameraSize(out _, out _);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Коридор", EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "Ширина",
            fromCamera
                ? $"{path.ResolveWidth(Vector2.right):0.##} по горизонтали, {path.ResolveWidth(Vector2.up):0.##} по вертикали (из обзора камеры)"
                : $"{path.ResolveWidth():0.##} юнитов (вручную)");

        if (path.widthFromCamera && !fromCamera)
        {
            EditorGUILayout.HelpBox(
                "Обзор камеры прочитать не удалось: у конфайнера нет виртуальной камеры или её размер нулевой. " +
                "Взята ручная ширина.",
                MessageType.Info);
        }

        if (path.confiner == null)
        {
            EditorGUILayout.HelpBox("Конфайнер не задан — построенный коридор не к чему подключить.", MessageType.Warning);

            if (GUILayout.Button("Найти конфайнер в сцене"))
            {
                Undo.RecordObject(path, UndoLabel);
                path.confiner = FindFirstObjectByType<CinemachineConfiner2D>(FindObjectsInactive.Include);
                EditorUtility.SetDirty(path);
            }
        }

        AutoRebuild = EditorGUILayout.Toggle(
            new GUIContent("Перестраивать сразу", "Пересобирать коридор после каждой правки линии"),
            AutoRebuild);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(path.HasCorridor ? "Перестроить коридор" : "Построить коридор", GUILayout.Height(26f)))
            {
                BuildWithUndo(path);
            }

            using (new EditorGUI.DisabledScope(!path.HasCorridor))
            {
                if (GUILayout.Button("Убрать коридор", GUILayout.Height(26f), GUILayout.Width(130f)))
                {
                    path.ClearCorridor(destroying => Undo.DestroyObjectImmediate(destroying));
                    EditorSceneManager.MarkSceneDirty(path.gameObject.scene);
                }
            }
        }

        EditorGUILayout.HelpBox(
            "В сцене: перетаскивание — двигать точку, Shift+клик — добавить, Ctrl+клик — удалить. " +
            "Оранжевые квадратики над и под точкой — насколько камере можно подняться выше " +
            "или опуститься ниже линии в этом месте.",
            MessageType.None);

        // Правка полей в инспекторе (сглаживание, запас, ширина) — тот же повод перестроить коридор.
        if (changed)
        {
            Rebuild(path);
        }
    }

    private void OnSceneGUI()
    {
        CameraPath path = (CameraPath)target;

        if (path.nodes == null)
        {
            return;
        }

        Event current = Event.current;

        DrawCorridor(path);
        DrawNodes(path);
        HandleShortcuts(path, current);

        if (current.type == EventType.MouseUp && dragged)
        {
            dragged = false;
            Rebuild(path);
        }
    }

    #region Отрисовка

    // Превью строится той же геометрией, что и коллайдер: в сцене видна ровно будущая фигура.
    private static void DrawCorridor(CameraPath path)
    {
        List<CameraPathPoint> samples = path.Sample();

        if (samples.Count < 2)
        {
            return;
        }

        float[] widths = CameraCorridorFactory.SegmentWidths(samples, path.ResolveWidth, path.closed);

        using (new Handles.DrawingScope(CorridorColor, path.transform.localToWorldMatrix))
        {
            for (int i = 0; i < widths.Length; i++)
            {
                if (widths[i] > 0f)
                {
                    DrawPolygon(CameraCorridorFactory.SegmentPolygon(samples[i], samples[(i + 1) % samples.Count], widths[i]));
                }
            }

            CameraCorridorFactory.GetJointRange(samples.Count, path.closed, out int first, out int last);

            for (int i = first; i <= last; i++)
            {
                float side = CameraCorridorFactory.JointWidth(widths, i);

                if (side > 0f)
                {
                    DrawPolygon(CameraCorridorFactory.JointPolygon(samples[i], side));
                }
            }

            // Сама линия поверх коридора: кривая, если включено сглаживание.
            Vector3[] line = new Vector3[samples.Count + (path.closed ? 1 : 0)];

            for (int i = 0; i < line.Length; i++)
            {
                line[i] = samples[i % samples.Count].position;
            }

            Handles.color = LineColor;
            Handles.DrawAAPolyLine(3f, line);
        }
    }

    private static void DrawPolygon(Vector2[] polygon)
    {
        Vector3[] vertices = new Vector3[polygon.Length];

        for (int i = 0; i < polygon.Length; i++)
        {
            vertices[i] = polygon[i];
        }

        Handles.DrawAAConvexPolygon(vertices);
    }

    private void DrawNodes(CameraPath path)
    {
        for (int i = 0; i < path.nodes.Count; i++)
        {
            Vector3 world = path.GetWorldPoint(i);
            float size = HandleUtility.GetHandleSize(world) * 0.05f;

            using (new Handles.DrawingScope(LineColor))
            {
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.DotHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(path, UndoLabel);
                    path.SetWorldPoint(i, new Vector3(moved.x, moved.y, world.z));
                    EditorUtility.SetDirty(path);
                    dragged = true;
                }

                Handles.Label(world + Vector3.right * size * 3f, i.ToString());
            }

            DrawRangeHandles(path, i, size);
        }
    }

    /*
    Ручки запаса: квадратик над точкой — верхний край коридора в этом месте, под точкой — нижний.
    Отсчёт от края полосы шириной в кадр, поэтому «0» — это край по умолчанию, и тянуть ручку
    вверх значит ровно «разрешить камере подняться выше».
    */
    private void DrawRangeHandles(CameraPath path, int index, float size)
    {
        CameraPathPoint node = path.nodes[index];
        float half = path.ResolveWidth(NodeTangent(path, index)) * 0.5f;
        Matrix4x4 matrix = path.transform.localToWorldMatrix;

        Vector3 top = matrix.MultiplyPoint3x4(node.position + Vector2.up * (half + node.up));
        Vector3 bottom = matrix.MultiplyPoint3x4(node.position - Vector2.up * (half + node.down));
        Vector3 up = matrix.MultiplyVector(Vector3.up).normalized;

        using (new Handles.DrawingScope(RangeColor))
        {
            Handles.DrawDottedLine(top, bottom, 4f);

            EditorGUI.BeginChangeCheck();
            Vector3 newTop = Handles.Slider(top, up, size * 1.2f, Handles.CubeHandleCap, 0f);
            Vector3 newBottom = Handles.Slider(bottom, -up, size * 1.2f, Handles.CubeHandleCap, 0f);

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(path, UndoLabel);

            Vector2 localTop = path.transform.InverseTransformPoint(newTop);
            Vector2 localBottom = path.transform.InverseTransformPoint(newBottom);

            node.up = Mathf.Max(0f, localTop.y - node.position.y - half);
            node.down = Mathf.Max(0f, node.position.y - localBottom.y - half);
            path.nodes[index] = node;

            EditorUtility.SetDirty(path);
            dragged = true;
        }
    }

    // Направление пути в узле: по соседям, чтобы ручки стояли на краю той полосы, что видна в сцене.
    private static Vector2 NodeTangent(CameraPath path, int index)
    {
        int count = path.nodes.Count;
        int previous = path.closed ? (index - 1 + count) % count : Mathf.Max(index - 1, 0);
        int next = path.closed ? (index + 1) % count : Mathf.Min(index + 1, count - 1);

        Vector2 tangent = path.nodes[next].position - path.nodes[previous].position;

        return tangent.sqrMagnitude > Mathf.Epsilon ? tangent : Vector2.right;
    }

    #endregion

    #region Правка точек

    private void HandleShortcuts(CameraPath path, Event current)
    {
        if (current.type != EventType.MouseDown || current.button != 0)
        {
            return;
        }

        if (current.shift)
        {
            Insert(path, MouseOnPathPlane(path, current));
            current.Use();

            return;
        }

        if (current.control || current.command)
        {
            Remove(path, current);
            current.Use();
        }
    }

    private void Insert(CameraPath path, Vector3 world)
    {
        Vector3 local = path.transform.InverseTransformPoint(world);
        Vector2 position = new(local.x, local.y);

        Undo.RecordObject(path, UndoLabel);

        if (path.nodes.Count < 2)
        {
            path.nodes.Add(new CameraPathPoint(position));
        }
        else
        {
            int index = FindInsertIndex(path, position);

            // Новая точка наследует запас соседей, чтобы коридор в этом месте не схлопнулся.
            CameraPathPoint left = path.nodes[Mathf.Clamp(index - 1, 0, path.nodes.Count - 1)];
            CameraPathPoint right = path.nodes[Mathf.Clamp(index, 0, path.nodes.Count - 1)];
            CameraPathPoint inserted = CameraPathPoint.Lerp(left, right, 0.5f);
            inserted.position = position;

            path.nodes.Insert(index, inserted);
        }

        EditorUtility.SetDirty(path);
        Rebuild(path);
    }

    // Точка садится в ближайший отрезок, а за концами незамкнутого пути — продолжает линию.
    private static int FindInsertIndex(CameraPath path, Vector2 point)
    {
        int count = path.nodes.Count;
        int segments = path.closed ? count : count - 1;
        int bestSegment = 0;
        float bestDistance = float.MaxValue;
        float bestOffset = 0f;

        for (int i = 0; i < segments; i++)
        {
            Vector2 from = path.nodes[i].position;
            Vector2 to = path.nodes[(i + 1) % count].position;
            Vector2 delta = to - from;
            float lengthSqr = delta.sqrMagnitude;
            float offset = lengthSqr <= Mathf.Epsilon ? 0f : Vector2.Dot(point - from, delta) / lengthSqr;
            float distance = Vector2.Distance(point, from + delta * Mathf.Clamp01(offset));

            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestSegment = i;
            bestOffset = offset;
        }

        if (path.closed)
        {
            return bestSegment + 1;
        }

        if (bestSegment == 0 && bestOffset < 0f)
        {
            return 0;
        }

        return bestSegment == segments - 1 && bestOffset > 1f ? count : bestSegment + 1;
    }

    private void Remove(CameraPath path, Event current)
    {
        if (path.nodes.Count <= 2)
        {
            return;
        }

        int nearest = -1;
        float bestDistance = PickDistance;

        for (int i = 0; i < path.nodes.Count; i++)
        {
            float distance = Vector2.Distance(
                HandleUtility.WorldToGUIPoint(path.GetWorldPoint(i)),
                current.mousePosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = i;
            }
        }

        if (nearest < 0)
        {
            return;
        }

        Undo.RecordObject(path, UndoLabel);
        path.nodes.RemoveAt(nearest);
        EditorUtility.SetDirty(path);
        Rebuild(path);
    }

    private static void Rebuild(CameraPath path)
    {
        if (!AutoRebuild || !path.HasCorridor)
        {
            return;
        }

        path.Build();
        EditorSceneManager.MarkSceneDirty(path.gameObject.scene);
    }

    // Плоскость пути: клик мышью попадает ровно туда, где лежат точки, а не в нулевой Z.
    private static Vector3 MouseOnPathPlane(CameraPath path, Event current)
    {
        Plane plane = new(path.transform.forward, path.transform.position);
        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);

        return plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : path.transform.position;
    }

    #endregion
}
