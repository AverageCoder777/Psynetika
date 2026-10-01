# Враги: как это устроено и как добавить нового

## Быстрый старт: новый враг без единой строки кода

1. **Create → Psynetika → Enemy Config** — новый ассет рядом с `Assets/Resource/enemy/`.
2. Префаб: спрайт + `Rigidbody2D` + `Collider2D` (не триггер) + `Animator` + компонент **`EnemyController`**.
3. В `EnemyController.Config` положить конфиг из шага 1.
4. В конфиге заполнить: `maxHp`, `moveSpeed`, блок **Обнаружение игрока**, список **Атаки**.

`EnemyHealth`, `EnemyMovement`, `EnemyAttack`, `EnemySensor`, `StatusEffectHandler` контроллер
добавляет сам, если их нет на префабе. Опционально можно докинуть `DamageFlash` (нужен `SpriteRenderer`)
и `EnemyLoot` (дроп монет из конфига).

Всё остальное — данные:

| Хочу | Что настроить |
| --- | --- |
| ближник | Атаки → `Ближний бой/Удар` |
| стрелок | Атаки → `Дальний бой/Выстрел` + префаб пули с `targetLayers = Player` |
| кастер / босс | Атаки → `Способности/Каст способности` + `AbilityDefinition` |
| несколько атак | несколько модулей + `attackSelection` (Priority / Random) и `minRange`/`maxRange` у модулей |
| патрулирующий | Idle → `Патруль по линии`, `distance` (±distance от точки спавна) |
| патруль «отсюда досюда» у конкретного врага | компонент `EnemyPatrolPath` на враге в сцене, точки тащить в Scene View |
| своя территория у конкретного врага | компонент `EnemyAggroZone` на враге в сцене, прямоугольник тянуть в Scene View |
| стоит на месте до встречи | Idle → `Стоять` (или пусто) |
| не падает с платформ | Body → `Ходьба`, `stopAtLedges = true` + заполнить `groundMask` |
| не уходит далеко от спавна | `home.leashRadius` |
| возвращается домой, потеряв игрока | `home.returnWhenLost = true` |
| зоны агро без возни с триггерами | `perception.mode = Radius` (или Auto без триггеров на префабе) |
| враг физически упирается в игрока и толкает его (щитоносец, стена) | `blocksPlayer = true` (по умолчанию игрок проходит сквозь врагов) |
| мечется вокруг игрока и наскакивает с укусом | Engage → `Наскоки`, у атаки `cooldown` + `cooldownRandom` (3 + 2 = раз в 3–5 с) |
| бесцельно бродит, пока не заметил игрока | Idle → `Блуждание` |
| атакуют по очереди, а не толпой | `squad.enabled = true`, общий `squad.groupId` у всей команды |
| тело взрывается после смерти и травит игрока | Смерть → `deathEffects` → `Взрыв тела с отравлением` |

## Из чего собран враг

```
EnemyController            тонкий координатор: ссылки на компоненты + реестр состояний
├── EnemyHealth            HP, события Damaged/Died, IAbilityTarget + IDirectDamageReceiver
├── EnemyMovement          исполняет MoveIntent через тело из конфига, поворот, спавн, поводок, отбрасывание
├── EnemyAttack            выбор атаки, кулдауны, урон/пули/касты, IAbilityCaster + IAbilityStatOwner
├── EnemySensor            зоны агро и атаки: триггеры или радиусы
├── StatusEffectHandler    Burn/Glitch и их реакция
├── EnemyLoot (опц.)       дроп монет
└── DamageFlash (опц.)     подсветка при уроне
```

### Движение: тело, тактика, режим

Три независимых слоя:

```
Режим (состояние)   Rest ─► Engage ⇄ Attack        Return ─► Rest        Dead
        │ спрашивает у тактики из конфига
Тактика             idle: Стоять / Патруль по линии      engage: Преследование
        │ выдаёт MoveIntent («куда и с какой долей скорости»)
Тело                Ходьба  (дальше: полёт, лазание по фоновым стенам)
```

* **Тело** (`EnemyConfig.body`, `EnemyLocomotion`) — как враг физически двигается. Единственный, кто
  трогает `Rigidbody2D`. Движение идёт через скорость, поэтому `EnemyMovement.ApplyKnockback()` работает.
* **Тактика** (`EnemyConfig.idle` / `engage`, `EnemyTactic`) — куда враг хочет идти. Не знает, ходит он
  или летает: `Преследование` отдаёт точку игрока, а наземное тело само проецирует её на X и
  останавливается у обрыва.
* **Режим** (состояние FSM) — когда какую тактику включать. Режимов мало, и они общие для всех врагов.

**Маршрут на сцене.** `EnemyPatrolPath` на экземпляре врага подменяет тактику `idle` из конфига:
режим покоя ходит по его точкам (PingPong или Loop, пауза в каждой точке). Точки хранятся смещениями
от врага и тащатся мышкой в Scene View. Так у пауков с общим конфигом свой маршрут у каждого.

**Зона агро на сцене.** `EnemyAggroZone` на экземпляре — прямоугольник-территория врага:
игрок внутри — враг его замечает (вместо радиуса/триггера агро; зона удара прежняя), сам враг за
границу не выходит ни в погоне, ни в патруле. Игрок вышел — враг ждёт у границы `loseDelay` секунд
и теряет его. Ограничение движения живёт в `EnemyMovement` рядом с поводком, поэтому работает с любой
тактикой и любым телом.

И тело, и тактика — это **настройки** в общем для всех экземпляров конфиге. Состояние конкретного врага
(куда идёт патруль, сколько ждать) живёт в рантайме, который модуль создаёт через `CreateRuntime()`.
Поэтому в полях модуля рантайм-данные хранить нельзя.

### Состояния

Состояния лежат в реестре **по роли** (`EnemyStateId`), переходы идут по роли, а не по ссылке на объект:

```csharp
controller.ChangeState(EnemyStateId.Engage);
```

Зарегистрированы по умолчанию: `Rest`, `Engage`, `Attack`, `Return`, `Dead`.
Роль `Stagger` свободна — достаточно написать состояние и зарегистрировать его.

Смерть — такое же состояние (`EnemyDeadState`), поэтому предыдущее состояние корректно
отрабатывает `Exit()` и не оставляет включённым флаг аниматора посреди замаха.

### Команда: атаки по очереди

`EnemyConfig.squad` объединяет врагов в команду (`groupId`; пусто — все враги с этим конфигом).
У команды одна очередь (`EnemySquad`): на сближение и удар в каждый момент идёт только один,
остальные ждут, а после его удара следующий может начать не раньше чем через `turnGap`.
Очередь берут тактика `Наскоки` (на время сближения) и `EnemyAttackState` (на время удара) и отпускают
в своих `Exit()`, поэтому смерть или потеря игрока её освобождают. Застрявший у обрыва не держит
очередь дольше `maxTurnTime`.

### Эффекты смерти

`EnemyConfig.deathEffects` — список модулей `EnemyDeathEffect`, их запускает `EnemyDeadState`.
`Begin()` возвращает, сколько секунд эффекту нужно тело; тело живёт `max(deathDespawnDelay, …)`.
`Взрыв тела с отравлением` через 2–3 с мигает, взрывается и травит игрока в радиусе
(`StatusEffectHandler.TryApplyPoison`; обработчик статусов доставляется на игрока при первом отравлении).

### Атаки как данные

`EnemyAttackState` ничего не наносит сам: он проигрывает тайминги выбранного модуля —
**замах (`windup`) → момент удара (`module.Execute`) → отход (`recovery`)** — и запускает кулдауны.
Скорость атаки врага (`config.attackSpeed`) и множитель от бафов/`Glitch` растягивают весь цикл.

Атаку на текущий кадр выбирает `EnemyAttack.PickAttack()`: отсеивает по общему и персональному
кулдауну, по `minRange`/`maxRange` и по `module.CanUse()`, затем берёт первую (Priority) или
случайную (Random) из оставшихся.

## Расширение кодом

### Новый вид атаки

```csharp
[Serializable]
[AddTypeMenu("Ближний бой/Рывок с ударом")]
public class DashAttackModule : EnemyAttackModule
{
    public float dashDistance = 3f;
    public int damage = 15;

    public override bool CanUse(EnemyController controller) => controller.Sensor.PlayerTransform != null;

    public override void Execute(EnemyController controller)
    {
        // момент удара
    }
}
```

Класс сразу появится в выпадающем списке `EnemyConfig.attacks`. Контроллер, состояния и сенсор
править не нужно.

### Новая тактика движения

```csharp
[Serializable]
[AddTypeMenu("Держать дистанцию")]
public class KeepDistanceTactic : EnemyTactic
{
    public float preferred = 5f;

    public override EnemyTacticRuntime CreateRuntime(EnemyController owner) => new Runtime(this, owner);

    private class Runtime : EnemyTacticRuntime
    {
        private readonly KeepDistanceTactic settings;
        public Runtime(KeepDistanceTactic settings, EnemyController owner) : base(owner) => this.settings = settings;

        // Вызывается на физическом шаге; результат сразу исполняет тело.
        public override MoveIntent Tick(float deltaTime) { /* ... */ return MoveIntent.Stop; }
    }
}
```

Тактика появится в выпадающих списках `Idle`/`Engage` конфига. Новое тело — так же, наследник
`EnemyLocomotion` + `EnemyLocomotionRuntime`, список `Body`.

### Новое поведение (состояние)

```csharp
public class BossPhaseTwoState : EnemyStates { /* ... */ }

public class BossController : EnemyController
{
    protected override void CreateStates()
    {
        base.CreateStates();
        RegisterState(EnemyStateId.Stagger, new BossPhaseTwoState(this, StateMachine));
    }
}
```

`RegisterState` на уже занятую роль подменяет реализацию — например, можно заменить `Attack`
на собственное состояние, не трогая остальные.

## Инварианты, которые легко нарушить

* **Урон только через `IAbilityTarget.ReceiveDamage`** (полный пайплайн со статусами).
  Периодический урон (тики Burn, дрейн HP) — только через `IDirectDamageReceiver.ApplyDamage`,
  иначе Fire-тик будет бесконечно перенакладывать Burn сам на себя.
* После фактического применения атаки состояние обязано вызвать `EnemyAttack.NotifyAttackUsed(module)`,
  иначе кулдаун не запустится.
* У пули **врага** в префабе `Bullet.targetLayers` должен указывать на слой `Player`:
  при пустом поле пуля бьёт по слою `Enemy` (дефолт для пуль игрока).
* `EnemyConfig` — общий ассет для всех экземпляров врага. Рантайм-состояние держим в компонентах,
  в конфиг ничего не пишем.

## Миграция старых конфигов

Пустые `body`/`idle`/`engage` собираются из устаревших блоков: `ground` → `Ходьба` с теми же
проверками обрыва, `patrol.enabled` → `Патруль по линии` (иначе `Стоять`), бой → `Преследование`.
Каждое поле мигрирует независимо: заданный `idle` не отключает миграцию `body`.

Если список `attacks` пуст, `EnemyConfig.ResolveAttacks()` собирает модули из устаревших полей
внизу конфига (`meleeDamage`/`attackDuration`, либо `bulletPrefab`/`bulletDamage`/`bulletSpawnOffset`,
плюс список `abilities`). Миграция работает по принципу «всё или ничего»: как только в `attacks`
появился хотя бы один модуль, устаревшие поля перестают читаться.

Поэтому существующие конфиги (`EnemyConfig.asset`, `SpiderBoyConfig.asset`) ведут себя как раньше,
но при первой правке атак их стоит перенести в `attacks` и почистить устаревший блок.

Отдельного `RangedEnemyController` больше нет — стрелок описывается `BulletAttackModule` в конфиге.
