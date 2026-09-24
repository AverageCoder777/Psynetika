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
  значок посреди отрезка — переключить отрезок: кружок — гибкий (изгибается сглаживанием),
                           квадрат — строго прямой;
  панель подсказок       — в левом нижнем углу Scene View: список клавиш (сворачивается) и режим
                           «Новые отрезки» — каким будет отрезок, созданный Shift+кликом (он же —
                           режим отрезка, который новая точка разбила);
  подсказка у курсора    — пока зажат Shift или Ctrl, рядом с мышью написано, что сделает клик;
  оранжевые квадратики   — над и под каждой точкой: тянуть вверх/вниз, чтобы в этом месте
                           камера могла подняться выше (или опуститься ниже) линии;
  фиолетовый квадрат     — угол кадра камеры в точке: тянуть наружу — отдалить камеру, внутрь —
                           приблизить. Там, где зум не 1, пунктиром показан кадр и подпись «зум ×N».

Коридор пересобирается сразу после правки, если включено «Перестраивать сразу»: дизайнеру нужно
видеть настоящую фигуру конфайнера, а не только линию. Пересборка идёт мимо Undo — коридор целиком
производная от линии: отменяется сама линия, а коридор после отмены перестраивается заново.
*/
[CustomEditor(typeof(CameraPath))]
public class CameraPathEditor : Editor
{
    private const string AutoRebuildKey = "Psynetika.CameraPath.AutoRebuild";
    private const string NewSegmentStraightKey = "Psynetika.CameraPath.NewSegmentStraight";
    private const string ShowHintsKey = "Psynetika.CameraPath.ShowHints";
    private const float HintsPanelWidth = 340f;
    private const float HintRowHeight = 16f;
    private const string UndoLabel = "Путь камеры";
    private const float PickDistance = 18f;
    private const float SegmentPickRadius = 10f;
    private const float MinZoom = 0.2f;
    private const float MaxZoom = 5f;
    private const float ZoomStep = 0.05f;

    private static readonly Color LineColor = new(0.3f, 0.9f, 1f, 1f);
    private static readonly Color CorridorColor = new(0.3f, 0.9f, 1f, 0.2f);
    private static readonly Color RangeColor = new(1f, 0.6f, 0.2f, 1f);
    private static readonly Color ZoomColor = new(0.75f, 0.45f, 1f, 1f);
    private static readonly Color StraightColor = new(1f, 1f, 1f, 1f);
    private static readonly Color HoverColor = new(1f, 0.95f, 0.4f, 1f);
    private static readonly int ClickHash = "CameraPathClick".GetHashCode();
    private static readonly Color CursorHintBackColor = new(0f, 0f, 0f, 0.75f);

    private static readonly GUIContent[] SegmentModeLabels =
    {
        new("Гибкая", "Отрезок изгибается сглаживанием в плавную кривую"),
        new("Прямая", "Отрезок строго прямой, сглаживание его не трогает")
    };

    private bool dragged;
    private bool cursorHintShown;
    private int hoveredSegment = -1;

    private static bool AutoRebuild
    {
        get => EditorPrefs.GetBool(AutoRebuildKey, true);
        set => EditorPrefs.SetBool(AutoRebuildKey, value);
    }

    // Список клавиш на панели в Scene View: развёрнут, пока его не свернули.
    private static bool ShowHints
    {
        get => EditorPrefs.GetBool(ShowHintsKey, true);
        set => EditorPrefs.SetBool(ShowHintsKey, value);
    }

    // На Mac удаление — Cmd+клик: HandleClicks принимает и control, и command.
    private static string ModifierKey => Application.platform == RuntimePlatform.OSXEditor ? "Cmd" : "Ctrl";

    // Строки подсказок: клавиши и что они делают. Тот же список, что в шапке файла.
    private static string[,] HintRows => new[,]
    {
        { "Тянуть точку", "двигать линию" },
        { "Shift + клик", "добавить точку" },
        { ModifierKey + " + клик", "удалить точку" },
        { "Клик по значку", "кружок — гибкий отрезок, квадрат — прямой" },
        { "Оранжевые квадраты", "запас камеры вверх / вниз" },
        { "Фиолетовый квадрат", "зум: наружу — отдалить, внутрь — приблизить" },
        { ModifierKey + " + Z", "отменить правку" }
    };

    // Режим отрезков, которые создаёт Shift+клик. Настройка редактора, а не данных пути.
    private static bool NewSegmentStraight
    {
        get => EditorPrefs.GetBool(NewSegmentStraightKey, false);
        set => EditorPrefs.SetBool(NewSegmentStraightKey, value);
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

        NewSegmentStraight = EditorGUILayout.Popup(
            new GUIContent("Новые отрезки", "Каким будет отрезок, созданный Shift+кликом в сцене"),
            NewSegmentStraight ? 1 : 0,
            SegmentModeLabels) == 1;

        DrawSegmentList(path);
        DrawZoomSummary(path);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Все отрезки гибкие"))
            {
                SetAllSegments(path, false);
            }

            if (GUILayout.Button("Все отрезки прямые"))
            {
                SetAllSegments(path, true);
            }
        }

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
            $"В сцене: перетаскивание — двигать точку, Shift+клик — добавить, {ModifierKey}+клик — удалить. " +
            "Значок посреди отрезка переключает его: кружок — гибкий, квадрат — прямой. " +
            "Режим новых отрезков и список клавиш — на панели в левом нижнем углу Scene View. " +
            "Оранжевые квадратики над и под точкой — насколько камере можно подняться выше " +
            "или опуститься ниже линии в этом месте. Фиолетовый квадрат в углу кадра точки — зум: " +
            "участок с зумом — это точки с одинаковым зумом на его концах, между точками он меняется плавно.",
            MessageType.None);

        // Правка полей в инспекторе (сглаживание, запас, ширина) — тот же повод перестроить коридор.
        if (changed)
        {
            Rebuild(path);
        }
    }

    // Отрезки пути с переключателем гибкий/прямой: то же, что клик по значку в сцене.
    private void DrawSegmentList(CameraPath path)
    {
        int segments = path.SegmentCount;

        if (segments == 0)
        {
            return;
        }

        EditorGUILayout.LabelField("Отрезки", EditorStyles.boldLabel);

        if (path.smoothing <= 0 || path.nodes.Count < 3)
        {
            EditorGUILayout.HelpBox(
                "Сглаживание 0 или в пути меньше трёх точек — сейчас все отрезки и так прямые, " +
                "режим начнёт влиять, когда появится изгиб.",
                MessageType.None);
        }

        for (int i = 0; i < segments; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{i} → {(i + 1) % path.nodes.Count}", GUILayout.Width(70f));

                bool straight = path.IsSegmentStraight(i);
                bool picked = GUILayout.Toolbar(straight ? 1 : 0, SegmentModeLabels, EditorStyles.miniButton) == 1;

                if (picked != straight)
                {
                    ToggleSegment(path, i);
                }
            }
        }
    }

    // Сводка по зуму и сброс: в списке точек зум есть у каждой, а здесь видно, задан ли он вообще.
    private void DrawZoomSummary(CameraPath path)
    {
        int zoomed = 0;

        foreach (CameraPathPoint node in path.nodes)
        {
            if (!Mathf.Approximately(node.Zoom, 1f))
            {
                zoomed++;
            }
        }

        EditorGUILayout.LabelField(
            "Зум",
            zoomed == 0
                ? "везде обычный — тяните фиолетовый квадрат у точки"
                : $"задан у {zoomed} из {path.nodes.Count} точек");

        if (zoomed > 0 && path.confiner != null && path.confiner.ComponentOwner is not CinemachineCamera)
        {
            EditorGUILayout.HelpBox(
                "Конфайнер висит не на CinemachineCamera — менять обзор в игре будет нечему.",
                MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(zoomed == 0))
        {
            if (GUILayout.Button("Сбросить зум у всех точек"))
            {
                Undo.RecordObject(path, UndoLabel);

                for (int i = 0; i < path.nodes.Count; i++)
                {
                    CameraPathPoint node = path.nodes[i];
                    node.zoom = 1f;
                    path.nodes[i] = node;
                }

                EditorUtility.SetDirty(path);
                Rebuild(path);
            }
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

        // Клики разбираются первыми: иначе нажатие успевает забрать ручка точки
        // (FreeMoveHandle трактует Ctrl+перетаскивание как привязку к сетке).
        HandleClicks(path, current);

        DrawCorridor(path);
        DrawSegmentIcons(path);
        DrawNodes(path);
        DrawHintsPanel();
        DrawCursorHint(path, current);

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
            DrawZoomHandle(path, i);
        }
    }

    // Значок режима посреди каждого отрезка: кружок — гибкий, квадрат — прямой. Клик — в HandleClicks.
    private void DrawSegmentIcons(CameraPath path)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        for (int i = 0; i < path.SegmentCount; i++)
        {
            bool straight = path.IsSegmentStraight(i);
            bool hot = i == hoveredSegment;
            Vector3 world = SegmentIconPosition(path, i);
            float size = HandleUtility.GetHandleSize(world) * (hot ? 0.08f : 0.06f);
            Handles.CapFunction cap = straight ? Handles.RectangleHandleCap : Handles.CircleHandleCap;

            using (new Handles.DrawingScope(hot ? HoverColor : straight ? StraightColor : LineColor))
            {
                cap(0, world, Quaternion.identity, size, EventType.Repaint);
            }
        }
    }

    private static Vector3 SegmentIconPosition(CameraPath path, int segment)
    {
        return path.transform.TransformPoint(path.EvaluateSegment(segment, 0.5f));
    }

    // Значок отрезка под курсором в пределах SegmentPickRadius пикселей, иначе -1.
    private static int FindSegmentIcon(CameraPath path, Vector2 mousePosition)
    {
        int nearest = -1;
        float bestDistance = SegmentPickRadius;

        for (int i = 0; i < path.SegmentCount; i++)
        {
            float distance = Vector2.Distance(HandleUtility.WorldToGUIPoint(SegmentIconPosition(path, i)), mousePosition);

            if (distance <= bestDistance)
            {
                bestDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }

    /*
    Все клики инструмента в одном контроле, который обрабатывается раньше ручек точек.

    Действие выполняется сразу на нажатии и по геометрии (значок, точка под курсором), а не по
    тому, чья ручка «ближе»: так клик не зависит от порядка, в котором Unity опрашивает ручки,
    инструмент Move/Rect и выделение объектов. В Layout контрол заявляет расстояние меньше нуля,
    пока клик предназначен ему (курсор над значком или зажат модификатор) — тогда ни ручка
    инструмента, ни выделение сцены на отпускании кнопки его не перехватят.
    */
    private void HandleClicks(CameraPath path, Event current)
    {
        int id = GUIUtility.GetControlID(ClickHash, FocusType.Passive);

        switch (current.GetTypeForControl(id))
        {
            case EventType.Layout:
                if (IsModifierClick(current) || FindSegmentIcon(path, current.mousePosition) >= 0)
                {
                    HandleUtility.AddControl(id, -1f);
                }

                break;

            case EventType.MouseMove:
            {
                int hovered = FindSegmentIcon(path, current.mousePosition);

                if (hovered != hoveredSegment)
                {
                    hoveredSegment = hovered;
                    HandleUtility.Repaint();
                }

                break;
            }

            case EventType.MouseDown:
                if (current.button == 0 && !current.alt && TryClick(path, current))
                {
                    GUIUtility.hotControl = id;
                    current.Use();
                }

                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    current.Use();
                }

                break;
        }
    }

    private static bool IsModifierClick(Event current)
    {
        return current.shift || current.control || current.command;
    }

    private bool TryClick(CameraPath path, Event current)
    {
        if (current.shift)
        {
            Insert(path, MouseOnPathPlane(path, current));
            return true;
        }

        if (current.control || current.command)
        {
            Remove(path, current);
            return true;
        }

        int segment = FindSegmentIcon(path, current.mousePosition);

        if (segment < 0)
        {
            return false;
        }

        ToggleSegment(path, segment);

        return true;
    }

    private void ToggleSegment(CameraPath path, int segment)
    {
        Undo.RecordObject(path, UndoLabel);
        SetSegment(path, segment, !path.IsSegmentStraight(segment));
        EditorUtility.SetDirty(path);
        Rebuild(path);

        // Сцена после клика сама не перерисовывается — без этого новый значок и форма линии
        // появлялись бы только после следующего движения камеры.
        SceneView.RepaintAll();
        Repaint();
    }

    /*
    Панель в углу Scene View: подсказки по клавишам и режим новых отрезков — там же, где ставятся
    точки, чтобы не держать шпаргалку в голове. Список сворачивается, режим отрезков виден всегда.
    */
    private static void DrawHintsPanel()
    {
        SceneView view = SceneView.currentDrawingSceneView;

        if (view == null)
        {
            return;
        }

        string[,] rows = HintRows;
        int rowCount = ShowHints ? rows.GetLength(0) : 0;
        float height = 70f + rowCount * HintRowHeight;

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10f, view.position.height - height - 44f, HintsPanelWidth, height), EditorStyles.helpBox);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Путь камеры", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(ShowHints ? "Скрыть клавиши" : "Клавиши", EditorStyles.miniButton))
            {
                ShowHints = !ShowHints;
            }
        }

        for (int i = 0; i < rowCount; i++)
        {
            using (new GUILayout.HorizontalScope(GUILayout.Height(HintRowHeight)))
            {
                GUILayout.Label(rows[i, 0], EditorStyles.miniBoldLabel, GUILayout.Width(110f));
                GUILayout.Label(rows[i, 1], EditorStyles.miniLabel);
            }
        }

        GUILayout.Label("Новые отрезки (Shift + клик)", EditorStyles.miniLabel);
        NewSegmentStraight = GUILayout.Toolbar(NewSegmentStraight ? 1 : 0, SegmentModeLabels, EditorStyles.miniButton) == 1;

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    /*
    Подсказка у курсора: пока зажат модификатор, рядом с мышью видно, что сделает клик —
    какую точку добавит или какую удалит. Модификатор сам событий не шлёт, поэтому
    Scene View перерисовывается на движение мыши и нажатия клавиш, пока подсказка нужна.
    */
    private void DrawCursorHint(CameraPath path, Event current)
    {
        bool modifier = current.shift || current.control || current.command;

        if (current.type == EventType.KeyDown || current.type == EventType.KeyUp
            || (current.type == EventType.MouseMove && (modifier || cursorHintShown)))
        {
            HandleUtility.Repaint();
        }

        if (current.type != EventType.Repaint)
        {
            return;
        }

        string text = null;

        if (current.shift)
        {
            text = $"+ точка, отрезок {(NewSegmentStraight ? "прямой" : "гибкий")}";
        }
        else if (current.control || current.command)
        {
            int nearest = FindNearestNode(path, current.mousePosition);

            text = path.nodes.Count <= 2
                ? "удалить нельзя: в пути минимум 2 точки"
                : nearest >= 0
                    ? $"− удалить точку {nearest}"
                    : "наведите на точку, чтобы удалить";
        }

        cursorHintShown = text != null;

        if (text == null)
        {
            return;
        }

        GUIContent content = new(text);
        Vector2 size = EditorStyles.whiteMiniLabel.CalcSize(content);
        Rect back = new(current.mousePosition.x + 16f, current.mousePosition.y + 16f, size.x + 8f, size.y + 4f);

        Handles.BeginGUI();
        EditorGUI.DrawRect(back, CursorHintBackColor);
        GUI.Label(new Rect(back.x + 4f, back.y + 2f, size.x, size.y), content, EditorStyles.whiteMiniLabel);
        Handles.EndGUI();
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

    /*
    Ручка зума: угол кадра камеры в этой точке. Кадр при зуме z — это обычный кадр, растянутый в z
    раз от точки, поэтому зум считается проекцией смещения ручки на диагональ обычного кадра.
    Шаг 0.05 — чтобы в данных оставались круглые числа, а не 1.4837.
    */
    private void DrawZoomHandle(CameraPath path, int index)
    {
        CameraPathPoint node = path.nodes[index];
        GetFrameHalfSize(path, out float halfWidth, out float halfHeight);

        float zoom = node.Zoom;
        Vector2 diagonal = new(halfWidth, halfHeight);
        Vector2 halfFrame = diagonal * zoom;
        Matrix4x4 matrix = path.transform.localToWorldMatrix;
        Vector3 corner = matrix.MultiplyPoint3x4(node.position + halfFrame);
        float size = HandleUtility.GetHandleSize(corner) * 0.04f;

        using (new Handles.DrawingScope(ZoomColor))
        {
            // Кадр — только там, где зум задан: рамки на каждой точке превратили бы сцену в кашу.
            if (!Mathf.Approximately(zoom, 1f))
            {
                DrawFrame(matrix, node.position, halfFrame);
                Handles.Label(corner + Vector3.right * size * 3f, $"зум ×{zoom:0.##}");
            }

            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.FreeMoveHandle(corner, size, Vector3.zero, Handles.DotHandleCap);

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Vector2 offset = (Vector2)path.transform.InverseTransformPoint(moved) - node.position;
            float picked = Vector2.Dot(offset, diagonal) / diagonal.sqrMagnitude;

            Undo.RecordObject(path, UndoLabel);
            node.zoom = Mathf.Clamp(Mathf.Round(picked / ZoomStep) * ZoomStep, MinZoom, MaxZoom);
            path.nodes[index] = node;
            EditorUtility.SetDirty(path);
            dragged = true;
        }
    }

    // Половины обычного кадра камеры; без камеры — ручная ширина коридора и 16:9.
    private static void GetFrameHalfSize(CameraPath path, out float halfWidth, out float halfHeight)
    {
        if (path.TryGetCameraSize(out halfWidth, out halfHeight))
        {
            return;
        }

        halfHeight = path.ResolveWidth() * 0.5f;
        halfWidth = halfHeight * 16f / 9f;
    }

    private static void DrawFrame(Matrix4x4 matrix, Vector2 center, Vector2 half)
    {
        Vector3 bottomLeft = matrix.MultiplyPoint3x4(center + new Vector2(-half.x, -half.y));
        Vector3 bottomRight = matrix.MultiplyPoint3x4(center + new Vector2(half.x, -half.y));
        Vector3 topRight = matrix.MultiplyPoint3x4(center + new Vector2(half.x, half.y));
        Vector3 topLeft = matrix.MultiplyPoint3x4(center + new Vector2(-half.x, half.y));

        Handles.DrawDottedLines(
            new[] { bottomLeft, bottomRight, bottomRight, topRight, topRight, topLeft, topLeft, bottomLeft },
            4f);
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

    private void Insert(CameraPath path, Vector3 world)
    {
        Vector3 local = path.transform.InverseTransformPoint(world);
        Vector2 position = new(local.x, local.y);

        Undo.RecordObject(path, UndoLabel);

        bool straight = NewSegmentStraight;

        if (path.nodes.Count < 2)
        {
            if (path.nodes.Count == 1)
            {
                SetSegment(path, 0, straight);
            }

            path.nodes.Add(new CameraPathPoint(position, straight: straight));
        }
        else
        {
            int index = FindInsertIndex(path, position);

            // Новая точка наследует запас соседей, чтобы коридор в этом месте не схлопнулся.
            CameraPathPoint left = path.nodes[Mathf.Clamp(index - 1, 0, path.nodes.Count - 1)];
            CameraPathPoint right = path.nodes[Mathf.Clamp(index, 0, path.nodes.Count - 1)];
            CameraPathPoint inserted = CameraPathPoint.Lerp(left, right, 0.5f);
            inserted.position = position;
            inserted.straight = straight;

            // Отрезок, который точка разбила или продолжила, получает выбранный режим целиком:
            // обе его половины — одного вида.
            if (index > 0)
            {
                SetSegment(path, index - 1, straight);
            }

            path.nodes.Insert(index, inserted);
        }

        EditorUtility.SetDirty(path);
        Rebuild(path);

        SceneView.RepaintAll();
        Repaint();
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

        int nearest = FindNearestNode(path, current.mousePosition);

        if (nearest < 0)
        {
            return;
        }

        Undo.RecordObject(path, UndoLabel);
        path.nodes.RemoveAt(nearest);
        EditorUtility.SetDirty(path);
        Rebuild(path);

        SceneView.RepaintAll();
        Repaint();
    }

    // Точка под курсором в пределах PickDistance пикселей, иначе -1.
    private static int FindNearestNode(CameraPath path, Vector2 mousePosition)
    {
        int nearest = -1;
        float bestDistance = PickDistance;

        for (int i = 0; i < path.nodes.Count; i++)
        {
            float distance = Vector2.Distance(HandleUtility.WorldToGUIPoint(path.GetWorldPoint(i)), mousePosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }

    // Режим отрезка хранится в его начальной точке.
    private static void SetSegment(CameraPath path, int segment, bool straight)
    {
        CameraPathPoint node = path.nodes[segment];
        node.straight = straight;
        path.nodes[segment] = node;
    }

    private static void SetAllSegments(CameraPath path, bool straight)
    {
        Undo.RecordObject(path, UndoLabel);

        for (int i = 0; i < path.nodes.Count; i++)
        {
            SetSegment(path, i, straight);
        }

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
