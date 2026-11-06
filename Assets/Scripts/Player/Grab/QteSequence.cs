using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
Настройки QTE захвата: одна случайная кнопка из набора и один таймер.
Каждое нажатие кнопки сокращает захват на одно нажатие из pressesToEscape; захват кончился раньше
timeLimit — игрок вырвался, не успел — персонаж совершает самоубийство.
*/
[Serializable]
public class QteSettings
{
    [Tooltip("Набор кнопок: на каждый захват случайно выбирается одна из них")]
    public List<Key> keyPool = new() { Key.Q, Key.E, Key.R, Key.F };

    [Tooltip("Сколько нажатий нужно, чтобы сбросить захват")]
    [Min(1)] public int pressesToEscape = 5;

    [Tooltip("Время на QTE, сек. Не вырвался — персонаж совершает самоубийство")]
    [Min(0.1f)] public float timeLimit = 2f;

    [Tooltip("Насколько нажатие другой кнопки из набора возвращает захват, нажатий. 0 = такие нажатия игнорируются")]
    [Min(0)] public int wrongKeyPenalty = 0;

    [Tooltip("Не успел — персонаж погибает (захват нейрончика). Выключено — захват просто кончается и игрок получает failDamage (паутина)")]
    public bool killOnFail = true;

    [Tooltip("Урон при провале QTE, если killOnFail выключен")]
    [Min(0)] public int failDamage = 0;
}

/*
Рантайм одного QTE: выбранная кнопка, сколько нажатий осталось и таймер.
Настройки общие (лежат в конфиге врага), поэтому состояние живёт здесь, а не в QteSettings.
*/
public class QteSequence
{
    public enum Status
    {
        Running = 0,
        Succeeded = 1,
        Failed = 2
    }

    private readonly List<Key> candidates = new();
    private QteSettings settings;

    public QteSettings Settings => settings;
    public Key TargetKey { get; private set; } = Key.Space;
    public int PressesLeft { get; private set; }
    public int PressesRequired => settings != null ? settings.pressesToEscape : 0;
    public float TimeLeft { get; private set; }
    public float TimeLimit => settings != null ? settings.timeLimit : 0f;
    public Status State { get; private set; } = Status.Failed;

    public void Begin(QteSettings qte)
    {
        settings = qte ?? new QteSettings();
        PressesLeft = Mathf.Max(1, settings.pressesToEscape);
        TimeLeft = settings.timeLimit;
        State = Status.Running;

        candidates.Clear();
        if (settings.keyPool != null)
        {
            foreach (Key key in settings.keyPool)
            {
                if (key != Key.None && !candidates.Contains(key)) candidates.Add(key);
            }
        }
        TargetKey = candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : Key.Space;
    }

    // Вызывать раз в кадр: таймер и нажатия с клавиатуры.
    public Status Tick(float deltaTime)
    {
        if (State != Status.Running) return State;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            HandlePresses(keyboard);
            if (State != Status.Running) return State;
        }

        TimeLeft -= deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            State = Status.Failed;
        }
        return State;
    }

    private void HandlePresses(Keyboard keyboard)
    {
        if (keyboard[TargetKey].wasPressedThisFrame)
        {
            Press(TargetKey);
            return;
        }

        if (settings.keyPool == null) return;
        foreach (Key key in settings.keyPool)
        {
            if (key != Key.None && keyboard[key].wasPressedThisFrame)
            {
                Press(key);
                return;
            }
        }
    }

    // Одно нажатие: нужная кнопка сокращает захват, другая кнопка из набора возвращает штраф.
    // Кнопки не из набора (случайно зажатое «влево») не считаются вовсе.
    public void Press(Key key)
    {
        if (State != Status.Running) return;

        if (key == TargetKey)
        {
            PressesLeft--;
            if (PressesLeft <= 0)
            {
                PressesLeft = 0;
                State = Status.Succeeded;
            }
        }
        else if (settings.keyPool != null && settings.keyPool.Contains(key))
        {
            PressesLeft = Mathf.Min(PressesRequired, PressesLeft + settings.wrongKeyPenalty);
        }
    }

    // Подпись по физической клавише, а не по раскладке: на русской раскладке displayName дал бы «Й» вместо Q.
    public static string KeyLabel(Key key) => key switch
    {
        Key.Space => "SPACE",
        Key.LeftShift or Key.RightShift => "SHIFT",
        Key.LeftCtrl or Key.RightCtrl => "CTRL",
        Key.Enter => "ENTER",
        Key.Tab => "TAB",
        >= Key.Digit1 and <= Key.Digit0 => key == Key.Digit0 ? "0" : ((int)(key - Key.Digit1) + 1).ToString(),
        _ => key.ToString().ToUpperInvariant()
    };
}
