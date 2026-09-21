using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/*
Утилита «Psynetika → Шаблон уровня».

Шаблон — это обычная сцена, скопированная с рабочей (Level 1) и очищенная от контента конкретного
уровня. Именно копия, а не скрипт, который заново расставляет префабы: в Level 1 половина рабочей
обвязки живёт в оверрайдах инстансов, а не в самих префабах —
`Cameras` переопределяет TrackingTarget на игрока, размер линзы и весь тюнинг композера,
`UI` переопределяет gameplayUI, FinishPanel, Shop.player/inventory, аватары диалогов и раскладку.
Копирование сцены сохраняет это всё, расстановка префабов заново — нет.

Что считать контентом уровня, а что каркасом, утилита предлагает сама, но последнее слово за
галочками в списке: видно каждый корневой объект и причину, по которой он попал в ту или иную группу.
*/
public class LevelTemplateWindow : EditorWindow
{
    private const string TemplateFolder = "Assets/Scenes/Templates";
    private const string DefaultTemplatePath = TemplateFolder + "/Level Template.unity";
    private const string LevelFolder = "Assets/Scenes";

    private SceneAsset template;
    private string newLevelName = "New Level";
    private readonly List<RootEntry> roots = new();
    private Vector2 scroll;
    private string status;
    private MessageType statusType = MessageType.Info;

    private class RootEntry
    {
        public string name;
        public bool keep;
        public string reason;
    }

    [MenuItem("Psynetika/Шаблон уровня")]
    private static void Open()
    {
        LevelTemplateWindow window = GetWindow<LevelTemplateWindow>("Шаблон уровня");
        window.minSize = new Vector2(460f, 520f);
    }

    private void OnEnable()
    {
        if (template == null)
        {
            template = AssetDatabase.LoadAssetAtPath<SceneAsset>(DefaultTemplatePath);
        }

        ScanActiveScene();
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previous, Scene current)
    {
        ScanActiveScene();
        Repaint();
    }

    private void OnGUI()
    {
        DrawTemplateSection();
        EditorGUILayout.Space();

        DrawCreateSection();
        EditorGUILayout.Space();

        DrawCaptureSection();

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, statusType);
        }
    }

    #region Разделы окна

    private void DrawTemplateSection()
    {
        EditorGUILayout.LabelField("Шаблон", EditorStyles.boldLabel);
        template = (SceneAsset)EditorGUILayout.ObjectField("Сцена-шаблон", template, typeof(SceneAsset), false);
    }

    private void DrawCreateSection()
    {
        EditorGUILayout.LabelField("Новый уровень", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            newLevelName = EditorGUILayout.TextField("Имя сцены", newLevelName);

            using (new EditorGUI.DisabledScope(template == null || string.IsNullOrWhiteSpace(newLevelName)))
            {
                if (GUILayout.Button("Создать", GUILayout.Width(110f), GUILayout.Height(20f)))
                {
                    CreateLevel();
                }
            }
        }

        if (template == null)
        {
            EditorGUILayout.HelpBox("Шаблона ещё нет — соберите его из текущей сцены ниже.", MessageType.Info);
        }
    }

    private void DrawCaptureSection()
    {
        EditorGUILayout.LabelField("Собрать шаблон из текущей сцены", EditorStyles.boldLabel);

        Scene scene = SceneManager.GetActiveScene();

        if (string.IsNullOrEmpty(scene.path))
        {
            EditorGUILayout.HelpBox("Активная сцена ещё ни разу не сохранена — шаблон снять не из чего.", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(scene.path, EditorStyles.miniLabel);

            if (GUILayout.Button("Пересканировать", GUILayout.Width(130f)))
            {
                ScanActiveScene();
            }
        }

        EditorGUILayout.LabelField(
            "Отмеченные объекты попадут в шаблон, остальные будут удалены из копии.",
            EditorStyles.wordWrappedMiniLabel);

        using (EditorGUILayout.ScrollViewScope scrollView = new(scroll, GUILayout.MinHeight(180f)))
        {
            scroll = scrollView.scrollPosition;

            foreach (RootEntry entry in roots)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    entry.keep = EditorGUILayout.Toggle(entry.keep, GUILayout.Width(18f));
                    EditorGUILayout.LabelField(entry.name, GUILayout.Width(200f));
                    EditorGUILayout.LabelField(entry.reason, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        EditorGUILayout.HelpBox(
            "Границы камеры в шаблоне останутся пустыми: конфайнеру их подставит «Построение локации» "
            + "при первой сборке уровня.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(roots.Count == 0))
        {
            if (GUILayout.Button("Сохранить как шаблон уровня", GUILayout.Height(28f)))
            {
                CaptureTemplate();
            }
        }
    }

    #endregion

    #region Разбор сцены

    private void ScanActiveScene()
    {
        roots.Clear();
        status = null;

        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
        {
            return;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            bool keep = ShouldKeep(root, out string reason);

            roots.Add(new RootEntry
            {
                name = root.name,
                keep = keep,
                reason = reason
            });
        }
    }

    /*
    Каркас уровня — то, без чего сцена вообще не играется: камера, игрок, UI, ввод, килл-зона.
    Всё остальное (арт, враги, NPC, триггеры, собираемое) — контент конкретного уровня.
    */
    private static bool ShouldKeep(GameObject root, out string reason)
    {
        if (root.GetComponentInChildren<LocationBuildMarker>(true) != null)
        {
            reason = "сгенерированная локация — шаблон получит пустой корень";
            return false;
        }

        StringBuilder found = new();

        Keep<Camera>(root, found, "камера");
        Keep<CinemachineBrain>(root, found, "Cinemachine Brain");
        Keep<CinemachineCamera>(root, found, "вирткамера");
        Keep<CinemachineConfiner2D>(root, found, "конфайнер");
        Keep<CinemachineTargetGroup>(root, found, "таргет-группа");
        Keep<Canvas>(root, found, "UI");
        Keep<EventSystem>(root, found, "ввод");
        Keep<PlayerController>(root, found, "игрок");
        Keep<DeadZone>(root, found, "килл-зона");

        if (found.Length > 0)
        {
            reason = "каркас: " + found;
            return true;
        }

        StringBuilder content = new();

        Keep<EnemyController>(root, content, "враг");
        Keep<DialogueTriggerZone>(root, content, "диалог-триггер");
        Keep<DialogueInteractable>(root, content, "диалог");
        Keep<NPCSpawnTrigger>(root, content, "спавн NPC");
        Keep<MovingPlatform>(root, content, "движущаяся платформа");
        Keep<PanelSwitch>(root, content, "панель");
        Keep<SpriteRenderer>(root, content, "арт");
        Keep<Collider2D>(root, content, "коллайдер");

        reason = content.Length > 0 ? "контент уровня: " + content : "контент уровня";

        return false;
    }

    private static void Keep<T>(GameObject root, StringBuilder into, string label) where T : Component
    {
        if (root.GetComponentInChildren<T>(true) == null)
        {
            return;
        }

        if (into.Length > 0)
        {
            into.Append(", ");
        }

        into.Append(label);
    }

    #endregion

    #region Действия

    private void CaptureTemplate()
    {
        Scene scene = SceneManager.GetActiveScene();
        string originalPath = scene.path;

        if (string.IsNullOrEmpty(originalPath))
        {
            SetStatus("Активная сцена не сохранена.", MessageType.Error);
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        // Индексы считаем до копирования: порядок корневых объектов в копии тот же.
        List<int> removeIndices = new();

        for (int i = 0; i < roots.Count; i++)
        {
            if (!roots[i].keep)
            {
                removeIndices.Add(i);
            }
        }

        if (removeIndices.Count == roots.Count)
        {
            SetStatus("Не отмечено ни одного объекта — шаблон вышел бы пустым.", MessageType.Error);
            return;
        }

        if (!Directory.Exists(TemplateFolder))
        {
            Directory.CreateDirectory(TemplateFolder);
            AssetDatabase.Refresh();
        }

        if (!EditorSceneManager.SaveScene(scene, DefaultTemplatePath, true))
        {
            SetStatus("Не удалось сохранить копию сцены в " + DefaultTemplatePath, MessageType.Error);
            return;
        }

        Scene copy = EditorSceneManager.OpenScene(DefaultTemplatePath, OpenSceneMode.Single);
        GameObject[] copyRoots = copy.GetRootGameObjects();

        foreach (int index in removeIndices)
        {
            if (index < copyRoots.Length && copyRoots[index] != null)
            {
                DestroyImmediate(copyRoots[index]);
            }
        }

        // Пустой корень под будущую локацию: окно сборки найдёт его по маркеру.
        GameObject locationRoot = new(LocationBuildRunner.DefaultRootName);
        locationRoot.AddComponent<LocationBuildMarker>();
        SceneManager.MoveGameObjectToScene(locationRoot, copy);

        EditorSceneManager.MarkSceneDirty(copy);
        EditorSceneManager.SaveScene(copy, DefaultTemplatePath);

        EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);

        template = AssetDatabase.LoadAssetAtPath<SceneAsset>(DefaultTemplatePath);
        ScanActiveScene();

        SetStatus(
            $"Шаблон сохранён: {DefaultTemplatePath}. Оставлено объектов: {roots.Count - removeIndices.Count}, удалено: {removeIndices.Count}.",
            MessageType.Info);
    }

    private void CreateLevel()
    {
        string templatePath = AssetDatabase.GetAssetPath(template);

        if (string.IsNullOrEmpty(templatePath))
        {
            SetStatus("Шаблон не найден в проекте.", MessageType.Error);
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string safeName = string.Join("_", newLevelName.Split(Path.GetInvalidFileNameChars()));
        string target = AssetDatabase.GenerateUniqueAssetPath($"{LevelFolder}/{safeName}.unity");

        if (!AssetDatabase.CopyAsset(templatePath, target))
        {
            SetStatus("Не удалось скопировать шаблон в " + target, MessageType.Error);
            return;
        }

        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(target, OpenSceneMode.Single);
        ScanActiveScene();

        GetWindow<LocationBuilderWindow>("Построение локации");

        SetStatus($"Создана сцена {target}. Осталось задать файл .aseprite и построить локацию.", MessageType.Info);
    }

    private void SetStatus(string message, MessageType type)
    {
        status = message;
        statusType = type;
    }

    #endregion
}
