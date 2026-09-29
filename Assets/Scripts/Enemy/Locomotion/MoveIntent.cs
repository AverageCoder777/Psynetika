using UnityEngine;

/*
Намерение движения на один физический шаг: что хочет тактика, без знания о том, как враг двигается.
Тело (EnemyLocomotion) решает, как его исполнить: наземное идёт к точке только по X,
летающее — напрямую в 2D.
*/
public readonly struct MoveIntent
{
    public readonly bool HasDestination;
    public readonly Vector2 Destination;
    public readonly float SpeedScale;

    private MoveIntent(Vector2 destination, float speedScale)
    {
        HasDestination = true;
        Destination = destination;
        SpeedScale = speedScale;
    }

    // Стоять на месте (гравитация и отбрасывание продолжают работать).
    public static MoveIntent Stop => default;

    // Идти к точке со скоростью moveSpeed * speedScale.
    public static MoveIntent To(Vector2 destination, float speedScale = 1f) => new(destination, speedScale);
}
