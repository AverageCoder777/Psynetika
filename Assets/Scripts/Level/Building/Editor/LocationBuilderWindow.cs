using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
Утилита «Psynetika → Построение локации».

Собирает локацию из одного .aseprite со слоями: раскладывает слои по объектам сцены, строит под них
коллайдеры и подключает границы к CinemachineConfiner2D. Всё созданное живёт под одним корнем с
LocationBuildMarker — ручные объекты сцены не трогаются никогда.

Роль слоя берётся из двух мест. Правила конфига работают по маске имени (col_*, plat_*) — это
дёшево, пока художник держит конвенцию. Когда не держит, роль назначается прямо здесь: выбрали
слой в таблице, поставили «Статичная коллизия», увидели, какой слой физики и какой тег получит
объект. Ручное назначение сильнее любого правила и хранится в маркере, то есть в самой сцене.

Путь камеры (CameraPath) живёт отдельным объектом вне корня сборки: линию дизайнера нельзя терять
при каждой перестройке арта. Если коридор по пути построен, прямоугольник по габаритам уровня
утилита уже не строит.
*/
public class LocationBuilderWindow : EditorWindow
{
    private const string UndoLabel = "Построение локации";
    private const float LayerColumnWidth = 160f;
    private const float RuleColumnWidth = 90f;

    private static GUIStyle warnStyle;
    private static GUIStyle manualStyle;
    private static List<ActionOption> actionOptions;

    // Назначения правятся через SerializedObject самого окна: так [SerializeReference]-действие
    // рисуется штатным инспектором со всеми своими полями и выпадающим списком типов.
    [SerializeField] private List<LocationLayerOverride> overrides = new();

    private GameObject source;
    private LocationBuildConfig config;
    private CinemachineConfiner2D confiner;
    private CameraPath cameraPath;

    private readonly List<LocationBuildMarker> markers = new();
    private int markerIndex = -1;

    private readonly List<LocationLayerSource> layers = new();
    private string selectedLayer;

    private SerializedObject self;
    private LocationBuildReport report;
    private bool reportIsPreview;
    private Vector2 scroll;

    // Один вариант роли в выпадающем списке: тип действия и его имя из [AddTypeMenu].
    private class ActionOption
    {
        public Type type;
        public string label;
    }

    [MenuItem("Psynetika/Построение локации")]
    private static void Open()
    {
        LocationBuilderWindow window = GetWindow<LocationBuilderWindow>("Построение локации");
        window.minSize = new Vector2(460f, 560f);
    }

    // Слой без своего правила подсвечивается: чаще всего это опечатка в имени слоя.
    private static GUIStyle WarnStyle
    {
        get
        {
            if (warnStyle == null)
            {
                warnStyle = new GUIStyle(EditorStyles.label);
                warnStyle.normal.textColor = new Color(0.85f, 0.6f, 0.1f);
            }

            return warnStyle;
        }
    }

    // Слой с ручной ролью: видно, что тут решение человека, а не совпадение маски.
    private static GUIStyle ManualStyle
    {
        get
        {
            if (manualStyle == null)
            {
                manualStyle = new GUIStyle(EditorStyles.label);
                manualStyle.normal.textColor = new Color(0.45f, 0.8f, 1f);
            }

            return manualStyle;
        }
    }

    // Все наследники LocationLayerAction: новый класс действия появляется в списке ролей сам.
    private static List<ActionOption> ActionOptions
    {
        get
        {
            if (actionOptions != null)
            {
                return actionOptions;
            }

            actionOptions = new List<ActionOption>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<LocationLayerAction>())
            {
                if (type.IsAbstract || type.IsGenericType || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                AddTypeMenuAttribute menu = (AddTypeMenuAttribute)Attribute.GetCustomAttribute(type, typeof(AddTypeMenuAttribute));

                actionOptions.Add(new ActionOption
                {
                    type = type,
                    label = menu != null ? menu.GetTypeNameWithoutPath() : ObjectNames.NicifyVariableName(type.Name)
                });
            }

            actionOptions.Sort((left, right) => string.Compare(left.label, right.label, StringComparison.CurrentCulture));

            return actionOptions;
        }
    }

    private LocationBuildMarker CurrentMarker =>
        markerIndex >= 0 && markerIndex < markers.Count ? markers[markerIndex] : null;

    private void OnEnable()
    {
        self = new SerializedObject(this);
        RefreshScene();
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previous, Scene current)
    {
        RefreshScene();
        Repaint();
    }

    private void OnGUI()
    {
        DrawSceneSection();
        EditorGUILayout.Space();

        DrawSourceSection();
        EditorGUILayout.Space();

        DrawDiagnostics();
        EditorGUILayout.Space();

        DrawButtons();
        EditorGUILayout.Space();

        DrawCameraPathSection();
        EditorGUILayout.Space();

        DrawMessages();

        using (EditorGUILayout.ScrollViewScope scrollView = new(scroll))
        {
            scroll = scrollView.scrollPosition;

            DrawLayerTable();
            EditorGUILayout.Space();

            DrawSelectedLayer();
        }
    }

    #region Разделы окна

    private void DrawSceneSection()
    {
        EditorGUILayout.LabelField("Локация в сцене", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (markers.Count == 0)
            {
                EditorGUILayout.LabelField("Собранных локаций нет — будет создана новая");
            }
            else
            {
                string[] names = new string[markers.Count];

                for (int i = 0; i < markers.Count; i++)
                {
                    names[i] = markers[i] != null ? markers[i].name : "<удалена>";
                }

                int selected = EditorGUILayout.Popup(Mathf.Max(markerIndex, 0), names);

                if (selected != markerIndex)
                {
                    SelectMarker(selected);
                }
            }

            if (GUILayout.Button("Обновить", GUILayout.Width(90f)))
            {
                RefreshScene();
            }
        }
    }

    private void DrawSourceSection()
    {
        EditorGUILayout.LabelField("Источник", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        source = (GameObject)EditorGUILayout.ObjectField("Файл .aseprite", source, typeof(GameObject), false);
        config = (LocationBuildConfig)EditorGUILayout.ObjectField("Конфиг правил", config, typeof(LocationBuildConfig), false);
        confiner = (CinemachineConfiner2D)EditorGUILayout.ObjectField("Конфайнер камеры", confiner, typeof(CinemachineConfiner2D), true);

        if (EditorGUI.EndChangeCheck())
        {
            RefreshPreview();
        }
    }

    private void DrawDiagnostics()
    {
        if (source == null)
        {
            EditorGUILayout.HelpBox("Перетащите сюда .aseprite-файл уровня со слоями разметки.", MessageType.Info);
            return;
        }

        if (!AsepriteImportFixer.IsAsepriteAsset(source))
        {
            EditorGUILayout.HelpBox("Это не .aseprite-ассет. Нужен сам файл уровня, а не префаб или спрайт.", MessageType.Error);
            return;
        }

        if (AsepriteImportFixer.IsMergedIntoSingleLayer(source))
        {
            EditorGUILayout.HelpBox(
                "Файл импортируется в режиме Merge Frame: все слои сплющены в один спрайт, разбирать нечего.",
                MessageType.Warning);

            if (GUILayout.Button("Переключить на Individual Layers"))
            {
                SwitchImportMode();
            }
        }

        if (config == null)
        {
            EditorGUILayout.HelpBox("Не задан конфиг правил.", MessageType.Warning);

            if (GUILayout.Button("Создать конфиг по умолчанию"))
            {
                config = AsepriteImportFixer.CreateDefaultConfig();
                RefreshPreview();
            }
        }

        if (confiner == null)
        {
            EditorGUILayout.HelpBox(
                "CinemachineConfiner2D в сцене не найден: границы камеры будут построены, но не подключены.",
                MessageType.Info);
        }
    }

    private void DrawButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(source == null || config == null))
            {
                if (GUILayout.Button(CurrentMarker != null ? "Перестроить" : "Построить локацию", GUILayout.Height(28f)))
                {
                    Build();
                }
            }

            using (new EditorGUI.DisabledScope(CurrentMarker == null))
            {
                if (GUILayout.Button("Очистить", GUILayout.Height(28f), GUILayout.Width(110f)))
                {
                    Clear();
                }
            }
        }
    }

    private void DrawCameraPathSection()
    {
        EditorGUILayout.LabelField("Путь камеры", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        cameraPath = (CameraPath)EditorGUILayout.ObjectField("Путь", cameraPath, typeof(CameraPath), true);

        if (EditorGUI.EndChangeCheck())
        {
            PushToMarker();
        }

        if (cameraPath == null)
        {
            EditorGUILayout.HelpBox(
                "Пути нет: границы камеры — прямоугольник по габаритам уровня. " +
                "Создайте путь, чтобы провести линию, по которой камера должна ходить.",
                MessageType.Info);

            if (GUILayout.Button("Создать путь камеры"))
            {
                cameraPath = CameraPathEditor.Create(confiner);
                Selection.activeGameObject = cameraPath.gameObject;
                PushToMarker();
            }

            return;
        }

        EditorGUILayout.LabelField(
            "Линия",
            $"{(cameraPath.nodes != null ? cameraPath.nodes.Count : 0)} точек, " +
            $"коридор {cameraPath.ResolveWidth():0.##} юнитов" +
            (cameraPath.HasCorridor ? string.Empty : ", коридор не построен"));

        if (cameraPath.confiner == null)
        {
            EditorGUILayout.HelpBox("У пути не задан конфайнер — коридор не к чему подключить.", MessageType.Warning);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Править линию в сцене"))
            {
                Selection.activeGameObject = cameraPath.gameObject;

                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }

            if (GUILayout.Button(cameraPath.HasCorridor ? "Перестроить коридор" : "Построить коридор"))
            {
                CameraPathEditor.BuildWithUndo(cameraPath);
            }
        }
    }

    private void DrawMessages()
    {
        if (report == null)
        {
            return;
        }

        if (report.LevelSize != Vector2.zero)
        {
            // Размер в юнитах — самая быстрая проверка масштаба: сравните с ростом персонажа (~2.1 юнита).
            EditorGUILayout.LabelField(
                "Размер локации",
                $"{report.LevelSize.x:0.#} x {report.LevelSize.y:0.#} юнитов (масштаб {report.WorldScale:0.###})");
        }

        foreach (string error in report.Errors)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        foreach (string warning in report.Warnings)
        {
            EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }

        foreach (string note in report.Notes)
        {
            EditorGUILayout.HelpBox(note, MessageType.Info);
        }

        DrawOrphanOverrides();
    }

    // Назначения, чей слой исчез из файла: иначе они молча висят и путают при следующей сборке.
    private void DrawOrphanOverrides()
    {
        if (layers.Count == 0 || overrides.Count == 0)
        {
            return;
        }

        int orphans = 0;

        foreach (LocationLayerOverride item in overrides)
        {
            if (item != null && FindLayer(item.layer) == null)
            {
                orphans++;
            }
        }

        if (orphans == 0)
        {
            return;
        }

        EditorGUILayout.HelpBox($"Назначений для несуществующих слоёв: {orphans}.", MessageType.Warning);

        if (GUILayout.Button("Убрать лишние назначения"))
        {
            overrides.RemoveAll(item => item == null || FindLayer(item.layer) == null);
            self.Update();
            PushToMarker();
            RefreshPreview();
        }
    }

    private void DrawLayerTable()
    {
        if (report == null || report.Entries.Count == 0)
        {
            return;
        }

        EditorGUILayout.LabelField(
            reportIsPreview ? "Слои (предпросмотр)" : "Слои (результат сборки)",
            EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUILayout.LabelField("Слой", EditorStyles.miniBoldLabel, GUILayout.Width(LayerColumnWidth));
            EditorGUILayout.LabelField("Правило", EditorStyles.miniBoldLabel, GUILayout.Width(RuleColumnWidth));
            EditorGUILayout.LabelField("Что будет", EditorStyles.miniBoldLabel);
        }

        foreach (LocationBuildEntry entry in report.Entries)
        {
            DrawLayerRow(entry);
        }
    }

    private void DrawLayerRow(LocationBuildEntry entry)
    {
        Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
        bool manual = entry.rule == LocationBuildRunner.ManualRuleLabel;

        if (entry.layer == selectedLayer)
        {
            EditorGUI.DrawRect(row, new Color(0.24f, 0.48f, 0.9f, 0.25f));
        }

        Rect layerRect = new(row.x, row.y, LayerColumnWidth, row.height);
        Rect ruleRect = new(layerRect.xMax, row.y, RuleColumnWidth, row.height);
        Rect resultRect = new(ruleRect.xMax, row.y, row.width - LayerColumnWidth - RuleColumnWidth, row.height);

        GUIStyle layerStyle = manual ? ManualStyle : entry.matched ? EditorStyles.label : WarnStyle;

        EditorGUI.LabelField(layerRect, entry.layer, layerStyle);
        EditorGUI.LabelField(ruleRect, entry.rule, EditorStyles.miniLabel);
        EditorGUI.LabelField(resultRect, entry.result, EditorStyles.miniLabel);

        Event current = Event.current;

        if (current.type == EventType.MouseDown && row.Contains(current.mousePosition))
        {
            selectedLayer = entry.layer;
            current.Use();
            Repaint();
        }
    }

    /*
    Карточка выбранного слоя: что это за слой, как он выглядит и что с ним будет.
    Ровно то место, где «этот слой — земля» и «тег у него будет Floor» видно одновременно.
    */
    private void DrawSelectedLayer()
    {
        LocationLayerSource layer = FindLayer(selectedLayer);

        if (layer == null)
        {
            EditorGUILayout.HelpBox("Выберите слой в таблице, чтобы назначить ему роль.", MessageType.None);
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Rect preview = GUILayoutUtility.GetRect(72f, 72f, GUILayout.Width(72f), GUILayout.Height(72f));
                DrawSpritePreview(preview, layer.Sprite);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(layer.Name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Группа", string.IsNullOrEmpty(layer.GroupPath) ? "корень файла" : layer.GroupPath);
                    EditorGUILayout.LabelField("Порядок", layer.SortingOrder.ToString());

                    DrawRolePopup(layer.Name);
                }
            }

            int index = IndexOfOverride(layer.Name);

            if (index >= 0)
            {
                EditorGUILayout.Space();
                DrawActionFields(index);
            }
            else
            {
                LocationLayerAction action = config != null
                    ? config.ResolveAction(layer.Name, out _, out _)
                    : null;

                EditorGUILayout.HelpBox(
                    action != null
                        ? $"Роль берётся из правила конфига: {action.Describe()}."
                        : "Конфиг не задан, роль определить нечем.",
                    MessageType.None);
            }

            GameObject built = FindBuiltObject(layer.Name);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(built == null))
                {
                    if (GUILayout.Button("Выделить в сцене"))
                    {
                        Selection.activeGameObject = built;
                        EditorGUIUtility.PingObject(built);
                    }
                }

                using (new EditorGUI.DisabledScope(index < 0))
                {
                    if (GUILayout.Button("Вернуть правилу конфига"))
                    {
                        ApplyRole(layer.Name, null);
                    }
                }
            }
        }
    }

    private void DrawRolePopup(string layerName)
    {
        List<ActionOption> options = ActionOptions;
        string[] labels = new string[options.Count + 1];
        labels[0] = "Авто (правило конфига)";

        for (int i = 0; i < options.Count; i++)
        {
            labels[i + 1] = options[i].label;
        }

        LocationLayerAction manual = LocationLayerOverride.Find(overrides, layerName);
        int current = 0;

        if (manual != null)
        {
            current = options.FindIndex(option => option.type == manual.GetType()) + 1;
        }

        int picked = EditorGUILayout.Popup("Роль", current, labels);

        if (picked != current)
        {
            ApplyRole(layerName, picked == 0 ? null : options[picked - 1].type);
        }
    }

    // Поля действия рисуются без его собственного заголовка: тип уже выбран списком «Роль».
    private void DrawActionFields(int index)
    {
        self.Update();

        SerializedProperty action = self
            .FindProperty("overrides")
            .GetArrayElementAtIndex(index)
            .FindPropertyRelative("action");

        if (action == null)
        {
            return;
        }

        EditorGUI.BeginChangeCheck();

        SerializedProperty iterator = action.Copy();
        SerializedProperty end = action.GetEndProperty();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            EditorGUILayout.PropertyField(iterator, true);
            enterChildren = false;
        }

        if (EditorGUI.EndChangeCheck())
        {
            self.ApplyModifiedProperties();
            PushToMarker();
            RefreshPreview();
        }
    }

    // Превью слоя: спрайты уровня лежат в атласе, поэтому рисуем кусок атласа по его UV.
    private static void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        Rect textureRect = sprite.textureRect.width > 0f ? sprite.textureRect : sprite.rect;

        if (textureRect.width <= 0f || textureRect.height <= 0f)
        {
            return;
        }

        Rect uv = new(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height);

        float scale = Mathf.Min(rect.width / textureRect.width, rect.height / textureRect.height);
        float width = textureRect.width * scale;
        float height = textureRect.height * scale;

        Rect fitted = new(
            rect.x + (rect.width - width) * 0.5f,
            rect.y + (rect.height - height) * 0.5f,
            width,
            height);

        GUI.DrawTextureWithTexCoords(fitted, sprite.texture, uv, true);
    }

    #endregion

    #region Действия

    private void SwitchImportMode()
    {
        if (!AsepriteImportFixer.SwitchToIndividualLayers(source))
        {
            return;
        }

        // После реимпорта ссылка на model prefab остаётся валидной, но состав его детей меняется.
        RefreshPreview();
    }

    // Назначение роли слою: null возвращает слой под правила конфига.
    private void ApplyRole(string layerName, Type actionType)
    {
        int index = IndexOfOverride(layerName);

        if (actionType == null)
        {
            if (index >= 0)
            {
                overrides.RemoveAt(index);
            }
        }
        else
        {
            LocationLayerAction action = (LocationLayerAction)Activator.CreateInstance(actionType);

            if (index >= 0)
            {
                overrides[index].action = action;
            }
            else
            {
                overrides.Add(new LocationLayerOverride { layer = layerName, action = action });
            }
        }

        self.Update();
        PushToMarker();
        RefreshPreview();
    }

    private void Build()
    {
        // Коридор строим до сборки арта: тогда раннер видит, что границы камеры уже заданы,
        // и не подменяет их прямоугольником по габаритам уровня.
        if (cameraPath != null)
        {
            CameraPathEditor.BuildWithUndo(cameraPath);
        }

        LocationBuildMarker marker = CurrentMarker;
        Transform root;

        if (marker == null)
        {
            GameObject rootObject = new(LocationBuildRunner.DefaultRootName);
            Undo.RegisterCreatedObjectUndo(rootObject, UndoLabel);

            marker = Undo.AddComponent<LocationBuildMarker>(rootObject);
            root = rootObject.transform;
        }
        else
        {
            root = marker.transform;
            Undo.RecordObject(root, UndoLabel);
            ClearChildren(root);
        }

        if (confiner != null)
        {
            Undo.RecordObject(confiner, UndoLabel);
        }

        LocationBuildReport buildReport = LocationBuildRunner.Build(
            source,
            config,
            overrides,
            root,
            confiner,
            created => Undo.RegisterCreatedObjectUndo(created, UndoLabel));

        Undo.RecordObject(marker, UndoLabel);
        marker.source = source;
        marker.config = config;
        marker.overrides = LocationLayerOverride.Clone(overrides);
        marker.cameraPath = cameraPath;
        marker.lastBuild = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        EditorUtility.SetDirty(marker);

        if (confiner != null)
        {
            EditorUtility.SetDirty(confiner);
        }

        EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
        RefreshScene(marker);

        // RefreshScene пересчитывает предпросмотр — отчёт о самой сборке ставим после него.
        report = buildReport;
        reportIsPreview = false;

        Selection.activeGameObject = marker.gameObject;
    }

    private void Clear()
    {
        LocationBuildMarker marker = CurrentMarker;

        if (marker == null)
        {
            return;
        }

        ClearChildren(marker.transform);
        EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);

        RefreshPreview();
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
        }
    }

    private void RefreshScene(LocationBuildMarker preferred = null)
    {
        markers.Clear();
        markers.AddRange(FindObjectsByType<LocationBuildMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        if (markers.Count == 0)
        {
            markerIndex = -1;
        }
        else
        {
            int index = preferred != null ? markers.IndexOf(preferred) : 0;
            SelectMarker(Mathf.Max(index, 0));
        }

        if (confiner == null)
        {
            confiner = FindFirstObjectByType<CinemachineConfiner2D>(FindObjectsInactive.Include);
        }

        if (cameraPath == null)
        {
            cameraPath = FindFirstObjectByType<CameraPath>(FindObjectsInactive.Include);
        }
    }

    private void SelectMarker(int index)
    {
        markerIndex = index;

        LocationBuildMarker marker = CurrentMarker;

        if (marker == null)
        {
            return;
        }

        if (marker.source != null)
        {
            source = marker.source;
        }

        if (marker.config != null)
        {
            config = marker.config;
        }

        if (marker.cameraPath != null)
        {
            cameraPath = marker.cameraPath;
        }

        overrides = LocationLayerOverride.Clone(marker.overrides);
        self.Update();

        RefreshPreview();
    }

    // Назначения окна и маркера синхронны: сцена — единственное надёжное хранилище разметки.
    private void PushToMarker()
    {
        LocationBuildMarker marker = CurrentMarker;

        if (marker == null)
        {
            return;
        }

        Undo.RecordObject(marker, UndoLabel);
        marker.overrides = LocationLayerOverride.Clone(overrides);
        marker.cameraPath = cameraPath;
        EditorUtility.SetDirty(marker);
        EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
    }

    private void RefreshPreview()
    {
        layers.Clear();

        if (source == null || config == null)
        {
            report = null;
            return;
        }

        report = LocationBuildRunner.Preview(source, config, overrides);
        reportIsPreview = true;

        layers.AddRange(LocationBuildRunner.CollectLayers(source, new LocationBuildReport()));

        if (FindLayer(selectedLayer) == null)
        {
            selectedLayer = layers.Count > 0 ? layers[0].Name : null;
        }
    }

    private LocationLayerSource FindLayer(string layerName)
    {
        if (string.IsNullOrEmpty(layerName))
        {
            return null;
        }

        foreach (LocationLayerSource layer in layers)
        {
            if (layer.Name == layerName)
            {
                return layer;
            }
        }

        return null;
    }

    private int IndexOfOverride(string layerName)
    {
        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i] != null && overrides[i].layer == layerName)
            {
                return i;
            }
        }

        return -1;
    }

    // Объект слоя в уже собранной локации: по нему видно, что именно получилось.
    private GameObject FindBuiltObject(string layerName)
    {
        LocationBuildMarker marker = CurrentMarker;

        if (marker == null || string.IsNullOrEmpty(layerName))
        {
            return null;
        }

        foreach (Transform child in marker.GetComponentsInChildren<Transform>(true))
        {
            if (child != marker.transform && child.name == layerName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    #endregion
}
