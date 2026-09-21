using System;
using UnityEngine;

// Килл-зона: триггер плюс существующий скрипт DeadZone из Assets/Scripts/Level.
[Serializable]
[AddTypeMenu("Локация/Килл-зона")]
public class DeadZoneAction : TriggerZoneAction
{
    public override string Describe() => "килл-зона (DeadZone)";

    protected override void Configure(GameObject target, LocationBuildContext ctx)
    {
        target.AddComponent<DeadZone>();
        ctx.Note("DeadZone");
    }
}
