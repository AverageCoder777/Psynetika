using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/*
Превью всей сцены в окне «Построение локации»: сцена целиком видна прямо в утилите, без
переключения в Scene view.

Картинку снимает отдельная скрытая камера (HideAndDontSave: в сцену не сохраняется, в Hierarchy
не видна, на main camera и Cinemachine не влияет) в RenderTexture, через штатный рендер пайплайна —
поэтому видно ровно то, что нарисует URP 2D, со светом и сортировкой. Перерисовка идёт только по
изменениям сцены, а не на каждый OnGUI: иначе окно рендерило бы уровень на каждое движение мыши.

Поверх картинки рисуется то, чего в рендере нет: коллайдеры собранной локации (слои разметки
спрайтов не имеют), рамка выделенного объекта с его коллайдерами и объект под курсором.
Клик по превью выделяет объект в сцене — дальше окно само подхватывает его слой.

Навигация как в Scene view: колесо — зум к точке под курсором, правая/средняя кнопка или Alt+левая —
сдвиг. Зум 1 — кадр целиком; он переживает правки сцены, сбрасывается только кнопкой «Вписать»
и сменой режима кадра.
*/
public class LocationScenePreview : IDisposable
{
    public enum FrameMode
    {
        // Кадр по всем объектам сцены.
        Scene = 0,

        // Кадр только по собранной локации: когда огромный фон растягивает габариты сцены.
        Location = 1
    }

    private const float FramePadding = 0.05f;
    private const int CircleSegments = 32;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 64f;
    private const float ZoomStep = 1.15f;

    private static readonly Color BackgroundColor = new(0.11f, 0.11f, 0.12f, 1f);
    private static readonly Color SelectionColor = new(0.24f, 0.48f, 0.9f, 1f);
    private static readonly Color SelectionColliderColor = new(0.45f, 0.8f, 1f, 1f);
    private static readonly Color LayerObjectColor = new(1f, 0.6f, 0.15f, 1f);
    private static readonly Color LocationColliderColor = new(0.3f, 0.95f, 0.4f, 0.45f);
    private static readonly Color HoverColor = new(1f, 1f, 1f, 0.6f);
    private static readonly Color LabelBackColor = new(0f, 0f, 0f, 0.65f);

    private readonly Action repaint;
    private readonly List<Renderer> renderers = new();
    private readonly List<Collider2D> locationColliders = new();
    private readonly List<Collider2D> overlapBuffer = new();
    private readonly List<Vector2> pathBuffer = new();
    private readonly Vector3[] boxBuffer = new Vector3[5];

    private Camera camera;
    private RenderTexture texture;
    private bool renderDirty = true;
    private bool cacheDirty = true;

    // Кадр в мире (габариты с отступом) и видимая область под аспект окна и зум, в юнитах.
    private Bounds frame;
    private Vector2 viewCenter;
    private Vector2 viewSize;
    private Rect viewRect;

    // Зум относительно «кадр целиком» и сдвиг центра вида от центра кадра, в юнитах.
    private float zoom = 1f;
    private Vector2 pan;
    private bool panning;

    private GameObject hovered;
    private Transform locationRoot;
    private FrameMode mode;

    public LocationScenePreview(Action repaint)
    {
        this.repaint = repaint;

        ObjectChangeEvents.changesPublished += OnChangesPublished;
        EditorApplication.hierarchyChanged += MarkDirty;
        Undo.undoRedoPerformed += MarkDirty;
    }

    public FrameMode Mode
    {
        get => mode;
        set
        {
            if (mode != value)
            {
                mode = value;
                ResetView();
            }
        }
    }

    public bool ShowLocationColliders { get; set; } = true;

    // Корень собранной локации: по нему кадр «Локация» и зелёный слой коллайдеров.
    public Transform LocationRoot
    {
        get => locationRoot;
        set
        {
            if (locationRoot != value)
            {
                locationRoot = value;
                MarkDirty();
            }
        }
    }

    // Объект слоя, выбранного в таблице окна: подсвечивается вторым цветом.
    public GameObject LayerObject { get; set; }

    // Габариты кадра в юнитах: по ним окно подбирает высоту превью.
    public Vector2 FrameSize
    {
        get
        {
            RefreshCaches();
            return frame.size;
        }
    }

    public float Zoom => zoom;

    // Вернуть вид к «кадр целиком».
    public void ResetView()
    {
        zoom = 1f;
        pan = Vector2.zero;
        MarkDirty();
    }

    public void MarkDirty()
    {
        renderDirty = true;
        cacheDirty = true;
        repaint?.Invoke();
    }

    public void Dispose()
    {
        ObjectChangeEvents.changesPublished -= OnChangesPublished;
        EditorApplication.hierarchyChanged -= MarkDirty;
        Undo.undoRedoPerformed -= MarkDirty;

        if (camera != null)
        {
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            camera = null;
        }

        if (texture != null)
        {
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
        }
    }

    public void Draw(Rect rect)
    {
        Event current = Event.current;

        // В Layout прямоугольник из GUILayout фиктивный (0,0,1,1) — не даём ему сбить кадр.
        if (current.type == EventType.Layout || rect.width < 2f || rect.height < 2f)
        {
            return;
        }

        RefreshCaches();

        if (rect.size != viewRect.size)
        {
            renderDirty = true;
        }

        viewRect = rect;
        UpdateView();

        HandleMouse(current, rect);

        if (current.type != EventType.Repaint)
        {
            return;
        }

        if (renderDirty || camera == null || texture == null || !texture.IsCreated())
        {
            Render();
        }

        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);

        GUI.BeginClip(rect);
        DrawOverlay();
        GUI.EndClip();
    }

    #region Рендер

    private void Render()
    {
        EnsureCamera();
        EnsureTexture();
        ConfigureCamera();

        RenderTexture previous = RenderTexture.active;

        try
        {
            RenderPipeline.StandardRequest request = new() { destination = texture };

            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            else
            {
                camera.targetTexture = texture;
                camera.Render();
                camera.targetTexture = null;
            }
        }
        finally
        {
            RenderTexture.active = previous;
        }

        renderDirty = false;
    }

    private void EnsureCamera()
    {
        if (camera != null)
        {
            return;
        }

        GameObject cameraObject = EditorUtility.CreateGameObjectWithHideFlags(
            "LocationPreviewCamera",
            HideFlags.HideAndDontSave,
            typeof(Camera));

        camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.nearClipPlane = 0.01f;

        int uiLayer = LayerMask.NameToLayer("UI");
        camera.cullingMask = uiLayer >= 0 ? ~(1 << uiLayer) : ~0;
    }

    private void EnsureTexture()
    {
        float scale = EditorGUIUtility.pixelsPerPoint;
        int width = Mathf.Max(1, Mathf.RoundToInt(viewRect.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(viewRect.height * scale));

        if (texture != null && texture.width == width && texture.height == height && texture.IsCreated())
        {
            return;
        }

        if (texture != null)
        {
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }

        texture = new RenderTexture(width, height, 24)
        {
            name = "LocationScenePreview",
            hideFlags = HideFlags.HideAndDontSave
        };

        texture.Create();
    }

    /*
    Свойства камеры присваиваются, только если реально изменились: скрытая камера — тоже объект
    сцены, и лишние присваивания могли бы порождать события изменений, а те — новую перерисовку.
    */
    private void ConfigureCamera()
    {
        Vector3 position = new(viewCenter.x, viewCenter.y, frame.min.z - 10f);
        float orthographicSize = viewSize.y * 0.5f;
        float farClip = frame.size.z + 20f;
        float aspect = viewRect.width / viewRect.height;

        // Фон как у игровой камеры: превью выглядит так же, как уровень в игре.
        Camera main = Camera.main;
        Color background = main != null ? main.backgroundColor : BackgroundColor;

        if (camera.transform.position != position)
        {
            camera.transform.position = position;
        }

        if (!Mathf.Approximately(camera.orthographicSize, orthographicSize))
        {
            camera.orthographicSize = orthographicSize;
        }

        if (!Mathf.Approximately(camera.farClipPlane, farClip))
        {
            camera.farClipPlane = farClip;
        }

        if (camera.backgroundColor != background)
        {
            camera.backgroundColor = background;
        }

        camera.aspect = aspect;
    }

    private void OnChangesPublished(ref ObjectChangeEventStream stream)
    {
        MarkDirty();
    }

    #endregion

    #region Кадр

    // Списки объектов и габариты кадра пересчитываются только после изменений сцены.
    private void RefreshCaches()
    {
        if (!cacheDirty)
        {
            return;
        }

        cacheDirty = false;
        renderers.Clear();
        locationColliders.Clear();

        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (IsSceneObject(renderer) && renderer.enabled && renderer.bounds.size.sqrMagnitude > 0f)
            {
                renderers.Add(renderer);
            }
        }

        if (locationRoot != null)
        {
            foreach (Collider2D collider in locationRoot.GetComponentsInChildren<Collider2D>())
            {
                if (collider.enabled)
                {
                    locationColliders.Add(collider);
                }
            }
        }

        bool found = mode == FrameMode.Location && locationRoot != null
            ? TryGetObjectBounds(locationRoot.gameObject, out frame)
            : TryGetSceneBounds(out frame);

        // Локация ещё не собрана — кадрируем всю сцену, чтобы превью не было пустым.
        if (!found)
        {
            found = TryGetSceneBounds(out frame);
        }

        if (!found)
        {
            frame = new Bounds(Vector3.zero, new Vector3(20f, 10f, 1f));
        }

        Vector3 size = frame.size;
        frame.Expand(new Vector3(size.x * FramePadding * 2f, size.y * FramePadding * 2f, 0f));
        frame.size = Vector3.Max(frame.size, new Vector3(1f, 1f, frame.size.z));
    }

    // Коллайдеры тоже входят в кадр: триггеры и коллизия без спрайтов иначе оказались бы за краем.
    private bool TryGetSceneBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer renderer in renderers)
        {
            Encapsulate(ref bounds, ref found, renderer.bounds);
        }

        foreach (Collider2D collider in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude))
        {
            if (IsSceneObject(collider) && collider.enabled)
            {
                Encapsulate(ref bounds, ref found, collider.bounds);
            }
        }

        return found;
    }

    private static bool TryGetObjectBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            if (renderer.enabled && renderer.bounds.size.sqrMagnitude > 0f)
            {
                Encapsulate(ref bounds, ref found, renderer.bounds);
            }
        }

        foreach (Collider2D collider in target.GetComponentsInChildren<Collider2D>())
        {
            if (collider.enabled)
            {
                Encapsulate(ref bounds, ref found, collider.bounds);
            }
        }

        return found;
    }

    private static void Encapsulate(ref Bounds bounds, ref bool found, Bounds other)
    {
        if (found)
        {
            bounds.Encapsulate(other);
        }
        else
        {
            bounds = other;
            found = true;
        }
    }

    // Видимая область под аспект окна: при зуме 1 кадр целиком влезает, лишнее место — по краям.
    private void UpdateView()
    {
        float aspect = viewRect.width / viewRect.height;
        float height = Mathf.Max(frame.size.y, frame.size.x / aspect) / zoom;
        viewSize = new Vector2(height * aspect, height);
        viewCenter = (Vector2)frame.center + pan;
    }

    // Координаты внутри GUI.BeginClip(viewRect): начало — левый верхний угол превью.
    private Vector2 WorldToGui(Vector2 world)
    {
        return new Vector2(
            (world.x - viewCenter.x) / viewSize.x * viewRect.width + viewRect.width * 0.5f,
            viewRect.height * 0.5f - (world.y - viewCenter.y) / viewSize.y * viewRect.height);
    }

    private Vector2 GuiToWorld(Vector2 local)
    {
        return new Vector2(
            viewCenter.x + (local.x - viewRect.width * 0.5f) / viewRect.width * viewSize.x,
            viewCenter.y + (viewRect.height * 0.5f - local.y) / viewRect.height * viewSize.y);
    }

    private static bool IsSceneObject(Component component)
    {
        return component != null
               && component.gameObject.scene.IsValid()
               && (component.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0;
    }

    #endregion

    #region Мышь

    private void HandleMouse(Event current, Rect rect)
    {
        switch (current.type)
        {
            case EventType.ScrollWheel when rect.Contains(current.mousePosition):
                ZoomAt(current.mousePosition - rect.position, current.delta.y);
                current.Use();
                break;

            case EventType.MouseDown when rect.Contains(current.mousePosition) && IsPanButton(current):
                panning = true;
                current.Use();
                break;

            case EventType.MouseDrag when panning:
                pan -= new Vector2(
                    current.delta.x / viewRect.width * viewSize.x,
                    -current.delta.y / viewRect.height * viewSize.y);
                UpdateView();
                renderDirty = true;
                current.Use();
                repaint?.Invoke();
                break;

            case EventType.MouseUp when panning:
                panning = false;
                current.Use();
                break;

            case EventType.MouseMove:
            {
                GameObject over = rect.Contains(current.mousePosition)
                    ? Pick(GuiToWorld(current.mousePosition - rect.position))
                    : null;

                if (over != hovered)
                {
                    hovered = over;
                    repaint?.Invoke();
                }

                break;
            }

            case EventType.MouseLeaveWindow:
                if (hovered != null)
                {
                    hovered = null;
                    repaint?.Invoke();
                }

                break;

            case EventType.MouseDown when current.button == 0 && rect.Contains(current.mousePosition):
            {
                GameObject picked = Pick(GuiToWorld(current.mousePosition - rect.position));
                Selection.activeGameObject = picked;

                if (picked != null && current.clickCount == 2 && SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }

                current.Use();
                break;
            }
        }
    }

    private static bool IsPanButton(Event current)
    {
        return current.button == 1 || current.button == 2 || (current.button == 0 && current.alt);
    }

    // Зум к точке под курсором: мировая точка под мышью остаётся на месте.
    private void ZoomAt(Vector2 local, float wheelDelta)
    {
        Vector2 anchor = GuiToWorld(local);
        float factor = wheelDelta > 0f ? 1f / ZoomStep : ZoomStep;

        zoom = Mathf.Clamp(zoom * factor, MinZoom, MaxZoom);
        UpdateView();

        pan += anchor - GuiToWorld(local);
        UpdateView();

        renderDirty = true;
        repaint?.Invoke();
    }

    /*
    Объект под точкой: коллайдеры по точной форме, рендереры по габаритам. Из всех кандидатов
    берётся самый маленький — иначе фон на весь уровень перебивал бы любой клик.
    */
    private GameObject Pick(Vector2 world)
    {
        GameObject best = null;
        float bestArea = float.MaxValue;

        Physics2D.SyncTransforms();
        overlapBuffer.Clear();
        Physics2D.OverlapPoint(world, ContactFilter2D.noFilter, overlapBuffer);

        foreach (Collider2D collider in overlapBuffer)
        {
            if (IsSceneObject(collider))
            {
                Consider(collider.gameObject, collider.bounds, ref best, ref bestArea);
            }
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;

            if (world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y)
            {
                Consider(renderer.gameObject, bounds, ref best, ref bestArea);
            }
        }

        return best;
    }

    private static void Consider(GameObject candidate, Bounds bounds, ref GameObject best, ref float bestArea)
    {
        float area = bounds.size.x * bounds.size.y;

        if (area < bestArea)
        {
            best = candidate;
            bestArea = area;
        }
    }

    #endregion

    #region Оверлей

    private void DrawOverlay()
    {
        if (ShowLocationColliders)
        {
            foreach (Collider2D collider in locationColliders)
            {
                DrawCollider(collider, LocationColliderColor, 1.5f);
            }
        }

        if (hovered != null)
        {
            DrawObjectBounds(hovered, HoverColor, 1.5f);
        }

        if (LayerObject != null && !Selection.Contains(LayerObject))
        {
            DrawObjectColliders(LayerObject, LayerObjectColor);
            DrawObjectBounds(LayerObject, LayerObjectColor, 2f);
        }

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null || !selected.scene.IsValid())
            {
                continue;
            }

            DrawObjectColliders(selected, SelectionColliderColor);
            DrawObjectBounds(selected, SelectionColor, 2.5f);
        }

        DrawHoverLabel();
    }

    private void DrawObjectColliders(GameObject target, Color color)
    {
        foreach (Collider2D collider in target.GetComponentsInChildren<Collider2D>())
        {
            DrawCollider(collider, color, 1.5f);
        }
    }

    // Рамка по габаритам объекта; у пустого объекта — крестик в его позиции.
    private void DrawObjectBounds(GameObject target, Color color, float width)
    {
        if (TryGetObjectBounds(target, out Bounds bounds))
        {
            DrawWorldRect(bounds.min, bounds.max, color, width);
            return;
        }

        Vector2 center = WorldToGui(target.transform.position);
        Handles.color = color;
        Handles.DrawAAPolyLine(width, center + new Vector2(-6f, 0f), center + new Vector2(6f, 0f));
        Handles.DrawAAPolyLine(width, center + new Vector2(0f, -6f), center + new Vector2(0f, 6f));
    }

    private void DrawCollider(Collider2D collider, Color color, float width)
    {
        if (collider == null || !collider.enabled)
        {
            return;
        }

        // Боксы, слитые в CompositeCollider2D, рисует сам composite — своей итоговой фигурой.
        if (collider.composite != null && collider.compositeOperation != Collider2D.CompositeOperation.None)
        {
            return;
        }

        Transform transform = collider.transform;

        switch (collider)
        {
            case BoxCollider2D box:
            {
                Vector2 half = box.size * 0.5f;
                Vector2 offset = box.offset;

                boxBuffer[0] = WorldToGui(transform.TransformPoint(offset + new Vector2(-half.x, -half.y)));
                boxBuffer[1] = WorldToGui(transform.TransformPoint(offset + new Vector2(half.x, -half.y)));
                boxBuffer[2] = WorldToGui(transform.TransformPoint(offset + new Vector2(half.x, half.y)));
                boxBuffer[3] = WorldToGui(transform.TransformPoint(offset + new Vector2(-half.x, half.y)));
                boxBuffer[4] = boxBuffer[0];

                Handles.color = color;
                Handles.DrawAAPolyLine(width, boxBuffer);
                break;
            }

            case PolygonCollider2D polygon:
                for (int i = 0; i < polygon.pathCount; i++)
                {
                    polygon.GetPath(i, pathBuffer);
                    DrawLocalPath(transform, polygon.offset, true, color, width);
                }

                break;

            case CompositeCollider2D composite:
                for (int i = 0; i < composite.pathCount; i++)
                {
                    composite.GetPath(i, pathBuffer);
                    DrawLocalPath(transform, Vector2.zero, true, color, width);
                }

                break;

            case EdgeCollider2D edge:
                edge.GetPoints(pathBuffer);
                DrawLocalPath(transform, edge.offset, false, color, width);
                break;

            case CircleCollider2D circle:
            {
                Vector3 scale = transform.lossyScale;
                float radius = circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                Vector2 center = transform.TransformPoint(circle.offset);
                Vector3[] points = new Vector3[CircleSegments + 1];

                for (int i = 0; i <= CircleSegments; i++)
                {
                    float angle = i * Mathf.PI * 2f / CircleSegments;
                    points[i] = WorldToGui(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }

                Handles.color = color;
                Handles.DrawAAPolyLine(width, points);
                break;
            }

            default:
                DrawWorldRect(collider.bounds.min, collider.bounds.max, color, width);
                break;
        }
    }

    // Путь из pathBuffer в локальных координатах коллайдера.
    private void DrawLocalPath(Transform transform, Vector2 offset, bool closed, Color color, float width)
    {
        if (pathBuffer.Count < 2)
        {
            return;
        }

        Vector3[] points = new Vector3[pathBuffer.Count + (closed ? 1 : 0)];

        for (int i = 0; i < pathBuffer.Count; i++)
        {
            points[i] = WorldToGui(transform.TransformPoint(pathBuffer[i] + offset));
        }

        if (closed)
        {
            points[^1] = points[0];
        }

        Handles.color = color;
        Handles.DrawAAPolyLine(width, points);
    }

    private void DrawWorldRect(Vector2 min, Vector2 max, Color color, float width)
    {
        boxBuffer[0] = WorldToGui(new Vector2(min.x, min.y));
        boxBuffer[1] = WorldToGui(new Vector2(max.x, min.y));
        boxBuffer[2] = WorldToGui(new Vector2(max.x, max.y));
        boxBuffer[3] = WorldToGui(new Vector2(min.x, max.y));
        boxBuffer[4] = boxBuffer[0];

        Handles.color = color;
        Handles.DrawAAPolyLine(width, boxBuffer);
    }

    private void DrawHoverLabel()
    {
        if (hovered == null)
        {
            return;
        }

        GUIContent content = new(hovered.name);
        Vector2 size = EditorStyles.whiteMiniLabel.CalcSize(content);
        Rect back = new(4f, viewRect.height - size.y - 8f, size.x + 8f, size.y + 4f);

        EditorGUI.DrawRect(back, LabelBackColor);
        GUI.Label(new Rect(back.x + 4f, back.y + 2f, size.x, size.y), content, EditorStyles.whiteMiniLabel);
    }

    #endregion
}
