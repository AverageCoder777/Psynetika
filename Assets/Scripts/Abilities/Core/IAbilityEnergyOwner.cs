// Кастер со шкалой энергии ульты (игрок). Враги не реализуют — ноды энергии у них no-op.
public interface IAbilityEnergyOwner
{
    // source — способность, давшая энергию: по ней определяется герой-владелец шкалы.
    void AddEnergy(AbilityDefinition source, float amount);
}
