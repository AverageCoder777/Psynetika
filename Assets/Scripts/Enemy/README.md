# Враги: как это устроено и как добавить нового

## Быстрый старт: новый враг без единой строки кода

1. **Create → Psynetika → Enemy Config** — новый ассет рядом с `Assets/Resource/enemy/`.
2. Префаб: спрайт + `Rigidbody2D` + `Collider2D` (не триггер) + `Animator` + компонент **`EnemyController`**.
3. В `EnemyController.Config` положить конфиг из шага 1.
4. В конфиге заполнить: `maxHp`, `moveSpeed`, блок **Обнаружение игрока**, список **Атаки**.

`EnemyHealth`, `EnemyMovement`, `EnemyAttack`, `EnemySensor`, `StatusEffectHandler`, `EnemyLoot` контроллер
добавляет сам, если их нет на префабе. Опционально можно докинуть `DamageFlash` (нужен `SpriteRenderer`).

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
| монеты при смерти | блок **Лут** (`CoinDrop`): префаб `Укурокоин Прайм`, количество, сила разлёта, `pickupDelay` |
| стая мелочи не толкается друг с другом | `blocksEnemies = false` |
| мечется вокруг игрока и наскакивает с укусом | Engage → `Наскоки`, у атаки `cooldown` + `cooldownRandom` (3 + 2 = раз в 3–5 с) |
| бесцельно бродит, пока не заметил игрока | Idle → `Блуждание` |
| атакуют по очереди, а не толпой | `squad.enabled = true`, общий `squad.groupId` у всей команды |
| тело взрывается после смерти и травит игрока | Смерть → `deathEffects` → `Взрыв тела с отравлением` |
| прыгает на лицо, игрок проходит QTE (нейрончик) | Атаки → `Захват/Прыжок на лицо (QTE)`: набор кнопок, сколько нажатий нужно, время до провала, замешательство после сброса |
| плюётся паутиной, опутанный игрок проходит QTE | Атаки → `Дальний бой/Плевок паутиной (QTE)` + префаб `WebSpit` |
| плюётся ядом, на земле остаётся ядовитая лужа | Атаки → `Дальний бой/Плевок ядом (лужа)` + префабы `PoisonGlob`, `PoisonPuddle`, `groundMask` = Floor + Platform |

## Из чего собран враг

```
EnemyController            тонкий координатор: ссылки на компоненты + реестр состояний
├── EnemyHealth            HP, события Damaged/Died, IAbilityTarget + IDirectDamageReceiver
├── EnemyMovement          исполняет MoveIntent через тело из конфига, поворот, спавн, поводок, отбрасывание
├── EnemyAttack            выбор атаки, кулдауны, урон/пули/касты, IAbilityCaster + IAbilityStatOwner
├── EnemySensor            зоны агро и атаки: триггеры или радиусы
├── StatusEffectHandler    Burn/Glitch и их реакция
├── EnemyLoot              дроп монет при смерти (EnemyConfig.loot)
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
Роли `Stagger` и `Grab` регистрируют модули атак, которым они нужны: `EnemyAttackModule.RegisterStates()`
вызывается для каждого врага после `CreateStates()`, поэтому атаке со своим режимом наследник контроллера не нужен.
Модуль может и сам перевести врага в другой режим прямо из `Execute()` — `EnemyAttackState` это учитывает.

### Захват с QTE (нейрончик)

`FaceGrabAttackModule` в момент удара вызывает `PlayerController.TryGrab()`: игрок уходит в `GrabbedState`
(действия заблокированы, на экране `QtePromptUI` — собирается кодом, в сцену ничего класть не нужно),
враг — в `EnemyFaceGrabState` (роль `Grab`: тело вне физики, сидит на лице героя, рисуется поверх него).
На каждый захват выбирается одна случайная кнопка из `qte.keyPool`; каждое нажатие сокращает захват,
нужно `qte.pressesToEscape` нажатий за `qte.timeLimit` (единственный таймер), иначе провал.
Прошёл QTE — враг отлетает в `EnemyStunnedState` (роль `Stagger`, не атакует); провалил — герой погибает
(`PlayerHealth.KillCurrentCharacter()`), враг спрыгивает. Погиб враг на лице — `Exit()` сам отпускает игрока.
Параметры аниматора (`Grabbing`, `Stunned`, у героя `Grabbed`) необязательны.
С `qte.killOnFail = false` провал не убивает, а наносит `qte.failDamage` — так работает паутина Мамы паука.

### Мини-босс «Мама паук» (`Bosses/SpiderMother`)

Пример босса на наследнике контроллера. Арена — объект `SpiderMother Arena` в `Level 1` (камера арены —
его дочерний объект, двигается вместе с ним); песочница — `Assets/Scenes/Boss Arena - Mama Spider.unity`,
префабы — `Assets/Prefabs/Enemies/SpiderMother/`, конфиги — `SpiderMotherConfig` и `MicroSpiderConfig`.

* `SpiderMotherController` регистрирует свои режимы: `Rest` — ждёт игрока, `Engage` — сидит на стене и
  плюётся, `Relocate` — ползёт по маршруту, `Summon` — под щитом рожает паучков, `Stagger` — стан.
* `SpiderMotherConfig : EnemyConfig` — обычный конфиг врага + фазы боя (сколько атак с точки, сколько точек
  до спуска, паучки, прочность щита, стан ≤ 5 с).
* `SpiderMotherArena` в сцене — стены (подножие + точки снизу вверх, по одной на уровень платформ), место
  на земле, прямоугольник начала боя и объекты на время боя (ворота, камера арены). Босс — её дочерний объект.
* Тело `Лазание по стенам (без гравитации)` — кинематическое, идёт к точке напрямую в 2D.
* Атаки — обычные модули: `Плевок паутиной (QTE)` (`SpiderWebProjectile`, перекат проскакивает сквозь)
  и `Плевок ядом (лужа)` (`PoisonGlobProjectile` → `PoisonPuddle` на 5–7 с, кратковременный яд).
* Щит — `SpiderWebShield` на дочернем объекте: принимает урон как враг, босс в это время
  `EnemyHealth.IsInvulnerable`. Полоска здоровья — `BossHealthBarUI`, собирается кодом.

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
