# Анализ StateMovMachine и проблемы attackRequested

## 📋 Резюме
Найдена **критичная проблема** в `HittingState.cs`: флаг `attackRequested` не сбрасывается при входе в состояние, что приводит к потере нажатий комбо-атак.

---

## 1️⃣ Порядок вызовов методов StateMovMachine

### 📍 Где находится StateMovMachine?
- **Файл:** `Assets/Scripts/Player/States/StateMovingMachine.cs`
- **Класс:** `StateMovMachine` (очень простой менеджер состояний)

### 📍 Методы StateMovMachine
```csharp
public class StateMovMachine
{
    public State CurrentPlayerState { get; set; }
    
    public void Initialize(State startingState)       // Инициализация
    public void ChangeState(State newState)           // Переход между состояниями
}
```

### 🔄 Вызовы в Player.Update() (каждый кадр):
```
1. PlayerSM.CurrentPlayerState.HandleInput()         ← Читает входные данные
   ↓
2. PlayerSM.CurrentPlayerState.LogicUpdate()         ← Логика состояния
   ↓
3. CharacterSM.CurrentPlayerState.LogicUpdate()      ← Логика персонажа (Satan/Dog)
```

### ⚙️ Вызовы в Player.FixedUpdate() (физический кадр):
```
1. PlayerSM.CurrentPlayerState.PhysicsUpdate()       ← Физические расчёты
   ↓
2. CharacterSM.CurrentPlayerState.PhysicsUpdate()    ← Физика персонажа
```

### 📊 Диаграмма вызовов:
```
Update():
  ├─ HandleInput()    [Читает нажатие клавиш]
  ├─ LogicUpdate()    [Проверяет условия для переходов]
  └─ LogicUpdate()    [Персонаж логика]
     
FixedUpdate():
  ├─ PhysicsUpdate()  [Rigidbody, анимация]
  └─ PhysicsUpdate()  [Персонаж физика]
```

---

## 2️⃣ Как вызывается StateMovMachine?

### 📌 Инициализация (Awake в Player.cs):
```csharp
PlayerSM = new StateMovMachine();           // Создание менеджера
IdleState = new IdleState(this, PlayerSM);  // Создание состояний
HittingState = new HittingState(this, PlayerSM);
// ... другие состояния ...
PlayerSM.Initialize(IdleState);             // Установка начального состояния
```

### 📌 Основной цикл:
```csharp
// Player.cs
void Update()
{
    PlayerSM.CurrentPlayerState.HandleInput();      // ← StateMovMachine используется здесь
    PlayerSM.CurrentPlayerState.LogicUpdate();
    CharacterSM.CurrentPlayerState.LogicUpdate();
}

void FixedUpdate()
{
    PlayerSM.CurrentPlayerState.PhysicsUpdate();
    CharacterSM.CurrentPlayerState.PhysicsUpdate();
}
```

### 📌 Переходы между состояниями:
```csharp
// Внутри любого состояния:
stateMachine.ChangeState(player.HittingState);  // ← Вызов StateMovMachine.ChangeState()
// Это приведёт к: Exit() текущего → Enter() нового
```

---

## 3️⃣ ПРОБЛЕМА: attackRequested остается false 🔴

### 🎯 Механика комбо (как должна работать):
```
Кадр N:
  └─ Игрок в IdleState нажимает Attack
     └─ IdleState.HandleInput(): hit = WasPressedThisFrame() → true
     └─ IdleState.LogicUpdate(): ChangeState(HittingState)
     └─ StateMovMachine.ChangeState():
        ├─ IdleState.Exit()
        └─ HittingState.Enter()    ← ПРОБЛЕМА ЗДЕСЬ!

Кадр N+1:
  └─ HittingState.HandleInput(): 
     └─ attackRequested = WasPressedThisFrame() → FALSE ❌
```

### ❌ Корневая причина (КРИТИЧНАЯ):

**HittingState.Enter() НЕ сбрасывает attackRequested:**

```csharp
// Текущий код (НЕПРАВИЛЬНЫЙ):
public override void Enter()
{
    base.Enter();
    // ... много кода ...
    jumpRequested = false;         // ✓ Сбрасывается корректно
    // attackRequested НЕ сбрасывается! ❌ ← ПРОБЛЕМА
    player.Rb.linearVelocity = new Vector2(0f, player.Rb.linearVelocity.y);
}
```

### 🔍 Цепочка событий, где нажатие теряется:

```
Событие 1: Нажатие Attack в кадре N
  ├─ InputSystem обрабатывает нажатие
  ├─ IdleState.HandleInput() читает: WasPressedThisFrame() → TRUE
  ├─ IdleState переходит на HittingState
  └─ attackRequested в памяти осталась со старого значения

Событие 2: Кадр N+1 - проверка комбо
  ├─ HittingState.HandleInput() вызывает WasPressedThisFrame()
  ├─ InputSystem уже обработал это нажатие в кадре N
  ├─ WasPressedThisFrame() → FALSE ❌
  └─ attackRequested = false (хотя комбо должна была сработать)

Результат: Комбо не срабатывает ❌
```

### ⚠️ Почему это происходит?

`WasPressedThisFrame()` возвращает `true` только в **одном кадре**, когда InputSystem впервые обработал нажатие. Если переход состояния произошёл после этого момента, то в следующем кадре `WasPressedThisFrame()` вернёт `false`.

---

## 4️⃣ РЕШЕНИЕ 

### ✅ Решение 1: Сбросить attackRequested в Enter() (ОБЯЗАТЕЛЬНО)

**Внесено в файл:** `Assets/Scripts/Player/States/Ground States/HittingState.cs`

```csharp
public override void Enter()
{
    base.Enter();
    if (Time.time - lastHitTime > comboResetTime)
    {
        comboCount = 0;
    }
    shooted = false;
    playerIsSatan = player.GetCurrentCharState() == player.SatanState;
    hittingSpeed = player.GetHittingSpeed();
    hitDistance = player.GetHitDistance();
    comboCount++;
    if (comboCount > 2) comboCount = 1;
    // ... код инициализации ...
    lastHitTime = Time.time;
    player.LastState = this;
    jumpRequested = false;
    attackRequested = false;  // ✅ ИСПРАВЛЕНИЕ: сбросить флаг
    player.Rb.linearVelocity = new Vector2(0f, player.Rb.linearVelocity.y);
}
```

**Что это делает:**
- Гарантирует, что `attackRequested` начинает с `false` при входе
- Избегает наследования значения из предыдущего состояния
- Позволяет новому нажатию Attack быть обнаруженным корректно

### 💡 Решение 2: Рассмотреть IsPressed() вместо WasPressedThisFrame()

**Текущий код:**
```csharp
attackRequested = player.PlayerInput.actions["Attack"].WasPressedThisFrame();
```

**Альтернатива:**
```csharp
attackRequested = player.PlayerInput.actions["Attack"].IsPressed();
```

**Различие:**
| Метод | Описание | Поведение |
|-------|---------|----------|
| `WasPressedThisFrame()` | Проверяет нажатие ТОЛЬКО в текущий кадр | true → false (один кадр) |
| `IsPressed()` | Проверяет текущее состояние кнопки | true → true → ... → false |

**Когда использовать:**
- `WasPressedThisFrame()` - для одноразовых действий (прыжок)
- `IsPressed()` - для действий "пока нажато" (комбо, держание)

### ✨ Дополнительно: Визуализация проблемы

```
ОШИБОЧНЫЙ СЦЕНАРИЙ:
─────────────────────
Кадр 5: Нажатие Attack
  Player.Update():
    IdleState.HandleInput(): attackRequested = true
    IdleState.LogicUpdate(): ChangeState(HittingState)
      → HittingState.Enter() (attackRequested НЕ сбрасывается!)

Кадр 6: Попытка комбо
  Player.Update():
    HittingState.HandleInput(): 
      WasPressedThisFrame() → FALSE (уже обработано) ❌
      attackRequested = false
    HittingState.LogicUpdate():
      if (hitCompleted && attackRequested) → НИКОГДА
      → Переход на IdleState

РЕЗУЛЬТАТ: Комбо потеряна ❌


ИСПРАВЛЕННЫЙ СЦЕНАРИЙ:
──────────────────────
Кадр 5: Нажатие Attack
  HittingState.Enter():
    attackRequested = false  // ✅ ИСПРАВЛЕНИЕ
    
Кадр 6: Повторное нажатие Attack
  Player.Update():
    HittingState.HandleInput(): 
      WasPressedThisFrame() → TRUE (новое нажатие) ✅
      attackRequested = true
    HittingState.LogicUpdate():
      if (hitCompleted && attackRequested) → TRUE
        → RestartAttack()  // Комбо продолжается! ✅
```

---

## 5️⃣ Проверка и тестирование

### 🧪 Как проверить исправление:

1. **Запустить игру**
2. **Попробовать комбо:**
   - Нажать Attack однажды
   - Подождать завершения первого удара
   - Нажать Attack снова
   - **Ожидаемый результат:** Комбо продолжается (второй удар)

3. **Проверить логи:**
   - Должна быть строка `Sostoyanie attackRequested: true` в LogicUpdate
   - Должна быть строка `RestartAttack()` или вызов комбо второго удара

### 📝 Debug код:
```csharp
// Уже имеется в HittingState.LogicUpdate():
Debug.Log("Sostoyanie attackRequested: " + attackRequested);
```

---

## 📊 Итоговая таблица: Порядок вызовов

| Момент | Метод | Статус | Назначение |
|--------|-------|--------|-----------|
| Update() | HandleInput() | 1️⃣ | Читает WasPressedThisFrame() |
| Update() | LogicUpdate() | 2️⃣ | Проверяет условия переходов |
| FixedUpdate() | PhysicsUpdate() | 3️⃣ | Расчёты физики/Rigidbody |
| StateChange | Exit() | 4️⃣ | Выход из текущего состояния |
| StateChange | Enter() | 5️⃣ | Вход в новое состояние |

---

## 🎯 Вывод

**Проблема:** `attackRequested` остается `false` из-за отсутствия сброса в `Enter()`

**Решение:** Добавлена строка `attackRequested = false;` в `HittingState.Enter()`

**Файл изменён:** `Assets/Scripts/Player/States/Ground States/HittingState.cs`

**Статус:** ✅ Исправлено
