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
| патрулирующий | `patrol.enabled = true`, `patrol.distance` |
| не падает с платформ | `ground.stopAtLedges = true` + заполнить `ground.groundMask` |
| зоны агро без возни с триггерами | `perception.mode = Radius` (или Auto без триггеров на префабе) |

## Из чего собран враг

```
EnemyController            тонкий координатор: ссылки на компоненты + реестр состояний
├── EnemyHealth            HP, события Damaged/Died, IAbilityTarget + IDirectDamageReceiver
├── EnemyMovement          шаг по X, поворот спрайта, проверка обрыва/стены, точка спавна
├── EnemyAttack            выбор атаки, кулдауны, урон/пули/касты, IAbilityCaster + IAbilityStatOwner
├── EnemySensor            зоны агро и атаки: триггеры или радиусы
├── StatusEffectHandler    Burn/Glitch и их реакция
├── EnemyLoot (опц.)       дроп монет
└── DamageFlash (опц.)     подсветка при уроне
```

### Состояния

Состояния лежат в реестре **по роли** (`EnemyStateId`), переходы идут по роли, а не по ссылке на объект:

```csharp
controller.ChangeState(EnemyStateId.Follow);
```

Зарегистрированы по умолчанию: `Idle`, `Patrol`, `Follow`, `Attack`, `Dead`.
Роли `Retreat` и `Stagger` свободны — достаточно написать состояние и зарегистрировать его.

Смерть — такое же состояние (`EnemyDeadState`), поэтому предыдущее состояние корректно
отрабатывает `Exit()` и не оставляет включённым флаг аниматора посреди замаха.

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

### Новое поведение (состояние)

```csharp
public class BossPhaseTwoState : EnemyStates { /* ... */ }

public class BossController : EnemyController
{
    protected override void CreateStates()
    {
        base.CreateStates();
        RegisterState(EnemyStateId.Retreat, new BossPhaseTwoState(this, StateMachine));
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

Если список `attacks` пуст, `EnemyConfig.ResolveAttacks()` собирает модули из устаревших полей
внизу конфига (`meleeDamage`/`attackDuration`, либо `bulletPrefab`/`bulletDamage`/`bulletSpawnOffset`,
плюс список `abilities`). Миграция работает по принципу «всё или ничего»: как только в `attacks`
появился хотя бы один модуль, устаревшие поля перестают читаться.

Поэтому существующие конфиги (`EnemyConfig.asset`, `SpiderBoyConfig.asset`) ведут себя как раньше,
но при первой правке атак их стоит перенести в `attacks` и почистить устаревший блок.

Отдельного `RangedEnemyController` больше нет — стрелок описывается `BulletAttackModule` в конфиге.
