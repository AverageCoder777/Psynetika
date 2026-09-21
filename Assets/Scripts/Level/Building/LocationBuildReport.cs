using System.Collections.Generic;
using UnityEngine;

// Строка предпросмотра: какой слой каким правилом обработан и что из этого вышло.
public class LocationBuildEntry
{
    public string layer;
    public string rule;
    public string result;
    public bool matched;
}

/*
Итог сборки или предпросмотра. Ничего не логирует сам: и окно утилиты, и будущие вызывающие
решают, что показать. Ошибка означает, что сборка не состоялась, предупреждение — что состоялась,
но к результату есть вопросы (пустой слой, ненайденный тег, отсутствие границ камеры).
*/
public class LocationBuildReport
{
    public List<LocationBuildEntry> Entries { get; } = new();
    public List<string> Warnings { get; } = new();

    // Сообщения «так и задумано»: сборка прошла штатно, но стоит знать, что именно она сделала.
    public List<string> Notes { get; } = new();
    public List<string> Errors { get; } = new();

    // Итоговый размер локации в юнитах — по нему сразу видно, сходится ли масштаб с ростом персонажа.
    public Vector2 LevelSize { get; set; }

    public float WorldScale { get; set; } = 1f;

    public bool HasErrors => Errors.Count > 0;

    public LocationBuildEntry AddEntry(string layer, string rule, bool matched)
    {
        LocationBuildEntry entry = new()
        {
            layer = layer,
            rule = rule,
            matched = matched,
            result = string.Empty
        };

        Entries.Add(entry);

        return entry;
    }

    public void Warn(string message)
    {
        Warnings.Add(message);
    }

    public void Info(string message)
    {
        Notes.Add(message);
    }

    public void Error(string message)
    {
        Errors.Add(message);
    }
}
