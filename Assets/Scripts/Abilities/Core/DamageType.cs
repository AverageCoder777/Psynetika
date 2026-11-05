public enum DamageType
{
    Physical = 0,
    Fire = 1,
    Glitch = 2,
    Poison = 3,
    // Сам статуса не накладывает: попадание по носителю Burn/Glitch разносит их на соседей.
    Wind = 4,
}
