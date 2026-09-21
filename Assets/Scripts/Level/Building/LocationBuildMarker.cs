using System.Collections.Generic;
using UnityEngine;

/*
Метка сгенерированного корня локации.

Нужна, чтобы настройки сборки жили в сцене, а не в EditorPrefs: окно утилиты находит по маркеру
все локации активной сцены и подтягивает из него источник и конфиг. У всей команды состояние
одинаковое, потому что оно лежит в том же файле сцены.
*/
[DisallowMultipleComponent]
public class LocationBuildMarker : MonoBehaviour
{
    [Tooltip("Файл .aseprite, из которого собрана локация")]
    public GameObject source;

    [Tooltip("Конфиг правил, по которому шла сборка")]
    public LocationBuildConfig config;

    [Tooltip("Роли, назначенные слоям вручную в окне утилиты. Перекрывают правила конфига")]
    public List<LocationLayerOverride> overrides = new();

    [Tooltip("Путь камеры этой локации. Живёт вне корня сборки, чтобы пережить перестройку")]
    public CameraPath cameraPath;

    [Tooltip("Когда собрано в последний раз. Заполняется автоматически")]
    public string lastBuild;
}
