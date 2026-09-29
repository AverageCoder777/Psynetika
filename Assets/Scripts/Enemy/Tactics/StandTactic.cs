using System;

// Стоять на месте.
[Serializable]
[AddTypeMenu("Стоять")]
public class StandTactic : EnemyTactic
{
    public override EnemyTacticRuntime CreateRuntime(EnemyController owner) => new Runtime(owner);

    private class Runtime : EnemyTacticRuntime
    {
        public Runtime(EnemyController owner) : base(owner) { }

        public override MoveIntent Tick(float deltaTime) => MoveIntent.Stop;
    }
}
