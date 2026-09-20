# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Проект

Psynetika — 2D-платформер на **Unity 6000.3.13f1** (URP 2D). Командный учебный проект (см. `README.md`).
Язык комментариев и документации в коде — **русский**; имена типов/членов — английские.

Ключевые зависимости: **UniTask** (асинхронность вместо корутин в новом коде), **Ink** (диалоги),
**FMOD** (звук), **MackySoft.SerializeReferenceExtensions** (`[SerializeReference, SubclassSelector]` +
`[AddTypeMenu("Категория/Имя")]` для полиморфных полей в инспекторе), **Input System**, **Cinemachine**.

## Сборка и запуск

Сборки/тестов из командной строки в репозитории нет — всё через Unity Editor (версия должна совпадать
с `ProjectSettings/ProjectVersion.txt`, иначе Unity переимпортирует проект).

* Игровые сцены: `Assets/Scenes/Level 1.unity`, `Assets/Scenes/Main menu.unity`, `Assets/Scenes/test level.unity`.
* Компиляцию скриптов можно проверить открытым редактором (окно Console) — `.csproj`/`.sln` в корне
  **генерируются Unity** и в `.gitignore`; править их руками бессмысленно.
* Тестовых ассембли (`EditMode`/`PlayMode`) в проекте нет, хотя `com.unity.test-framework` подключён.
  Если они появятся, headless-прогон:
  ```bash
  Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -logFile -
  ```

## Архитектура

### Общее

* **Нет asmdef для игрового кода** — весь `Assets/Scripts` компилируется в `Assembly-CSharp`,
  **без namespace**. Имена классов глобальны и должны быть уникальны по всему проекту.
* Настройки-данные лежат в ScriptableObject'ах в `Assets/Resource/**`, меню создания — префикс
  `Create → Psynetika/…`. Глобальный конфиг — `PsynetikaConfig` (доступ через `ConfigInstance.Value`,
  грузится из `Resources`), статика игрока — `Resources.Load<PlayerStaticSettings>("PlayerDefaultSettings")`.

### Игрок и враг: одинаковый паттерн

`PlayerController` и `EnemyController` — **тонкие координаторы**: держат ссылки на компоненты и
стейт-машину, сами логику не пишут. Логика живёт в компонентах (`*Movement`, `*Attack`, `*Health`, …)
и в состояниях (`State` / `EnemyStates`).

Цикл у обоих одинаковый: `Update → HandleInput() → LogicUpdate()`, `FixedUpdate → PhysicsUpdate()`.
Состояния игрока собирает `PlayerStateFactory`; состояния врага — `EnemyController.CreateStates()`.

Параметры аниматора кешируются как `static readonly int ...Hash = Animator.StringToHash("…")`.

### Урон и статусы — главный инвариант

Две принципиально разные точки входа урона, путать их нельзя:

| Интерфейс | Метод | Когда |
| --- | --- | --- |
| `IAbilityTarget` | `ReceiveDamage(DamageEvent)` | **полный пайплайн**: реакции/резисты статусов → урон → наложение статуса |
| `IDirectDamageReceiver` | `ApplyDamage(DamageEvent)` | **сырой** урон мимо статусов: тики Burn, дрейн HP кастера |

Тик Burn, пущенный через `ReceiveDamage`, бесконечно перенакладывал бы Burn сам на себя — поэтому
все периодические эффекты обязаны идти через `ApplyDamage`.

Единая точка нанесения урона по произвольному коллайдеру сцены — `DamageHelper.TryDamage(...)`
(`Assets/Scripts/Combat/`); не писать новые пары `GetComponent<EnemyHealth>()`/`GetComponent<DamageDummy>()`.

`StatusEffectHandler` работает с любым носителем `IDirectDamageReceiver` (+ опционально
`IAbilityStatOwner` для замедления Glitch). Реакция Fire×Glitch → взрыв (`explosionDamageMultiplier`),
настройки — в `StatusEffectConfig`.

### Система способностей (`Assets/Scripts/Abilities`)

`AbilityDefinition` (SO) — это кулдаун, тайминги каста и **список `AbilityNode`** в поле
`[SerializeReference, SubclassSelector] List<AbilityNode> root`. Ноды — обычные `[Serializable]`-классы
с `[AddTypeMenu(...)]`, выбираются в инспекторе из выпадающего списка, поэтому **новая механика
способности = новый класс-нода, без правки раннера**.

`AbilityRunner` (MonoBehaviour на кастере) исполняет список нод последовательно через UniTask:

* `AbilityContext` — контекст одного каста: владелец, цель, `Blackboard` (ноды обмениваются данными
  по строковым ключам, см. `[BlackboardKeyInput/Output]`), `CancellationToken`.
* `ctx.RegisterCleanup(...)` — откаты временных бафов/VFX; выполняются в `finally`, в том числе при
  отмене каста (`CancelAll()` на смерть/стан).
* Если первая же нода вернула `Failure`, кулдаун возвращается (каст «не состоялся»).

Кастер реализует `IAbilityCaster` (+ по необходимости `IAbilityDamageSource`, `IAbilityHealth`,
`IAbilityStatOwner`); цель — `IAbilityTarget`. Команды — `Team.Player/Enemy/Neutral`.

### Диалоги

`DialogueManager` (синглтон) + `IDialogueSource`/`IDialogueRunner`: два источника — `InkDialogue`
(скомпилированный Ink JSON) и `LinearDialogue` (простой список реплик). Запуск из сцены —
`DialogueInteractable`, `DialogueTriggerZone`, `NPCSpawnTrigger`.

## Работа с Unity-ассетами

* Каждый новый `.cs` требует парный `.meta` — Unity сгенерирует его при следующем фокусе на редакторе;
  **коммитить `.cs` без `.meta` нельзя**, иначе у остальных слетят ссылки.
* Префабы и `.asset` ссылаются на скрипты по **GUID из `.meta`**, а на поля — **по имени поля**.
  Переименование класса или serialized-поля молча теряет данные в существующих ассетах;
  при необходимости оставлять старое поле + миграцию (см. `EnemyConfig.ResolveAttacks()`).
* `.prefab`/`.unity`/`.asset` — это YAML; править их руками можно, но только точечно и осознанно.
* В `Assets/` много сторонних пакетов (`Ink`, `FMOD`, `MackySoft`, `Plugins/UniTask`, `TextMesh Pro`,
  `ErbGameArt`, `Thaleah_PixelFont`) — их не трогаем; наш код только в `Assets/Scripts`.

## Враги

Подробный гайд «как добавить врага» — [`Assets/Scripts/Enemy/README.md`](Assets/Scripts/Enemy/README.md).

Коротко: враг = префаб с `EnemyController` + ссылка на `EnemyConfig`. Компоненты
(`EnemyHealth`, `EnemyMovement`, `EnemyAttack`, `EnemySensor`, `StatusEffectHandler`) контроллер
добавляет сам, если их нет на префабе. Поведение и атаки задаются **данными в конфиге**:

* `EnemyConfig.attacks` — список `[SerializeReference]` модулей (`MeleeAttackModule`,
  `BulletAttackModule`, `AbilityAttackModule`); новый тип атаки = новый класс-модуль.
* `EnemyConfig.perception` — зоны агро/атаки: триггер-коллайдеры или радиусы (без ручной обвязки).
* `EnemyConfig.patrol` — патруль/стояние на месте, остановка у обрывов.

Состояния врага живут в реестре по роли (`EnemyStateId`), см. `EnemyController.RegisterState`;
новый архетип **не требует** нового наследника контроллера, пока хватает существующих ролей.
