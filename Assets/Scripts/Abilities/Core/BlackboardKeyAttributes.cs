using UnityEngine;

// Помечают строковые поля нод, работающие с ключами Blackboard.
// Редактор собирает все ключи графа и предлагает их в дропдауне,
// а по output/input-ролям валидирует, что каждый читаемый ключ кто-то пишет.

// Поле — ключ, ПОД КОТОРЫЙ нода записывает значение в Blackboard.
public sealed class BlackboardKeyOutputAttribute : PropertyAttribute { }

// Поле — ключ, ИЗ КОТОРОГО нода читает значение Blackboard.
public sealed class BlackboardKeyInputAttribute : PropertyAttribute { }
