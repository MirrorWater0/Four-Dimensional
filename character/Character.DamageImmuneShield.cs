using Godot;

public partial class Character
{
    private DamageImmuneShieldVfx _damageImmuneShield;

    private void RefreshDamageImmuneShield()
    {
        if (WarmupMode || !IsInsideTree() || IsQueuedForDeletion())
            return;

        bool active = false;
        if (State != CharacterState.Dying && HurtBuffs != null)
        {
            foreach (HurtBuff buff in HurtBuffs)
            {
                if (buff != null && buff.ThisBuffName == Buff.BuffName.DamageImmune && buff.Stack > 0)
                {
                    active = true;
                    break;
                }
            }
        }

        if (active && !GodotObject.IsInstanceValid(_damageImmuneShield))
            _damageImmuneShield = DamageImmuneShieldVfx.Spawn(this);

        if (GodotObject.IsInstanceValid(_damageImmuneShield))
            _damageImmuneShield.SetActive(active, immediate: State == CharacterState.Dying);
    }

    internal void PlayDamageImmuneShieldImpact(Character attacker)
    {
        RefreshDamageImmuneShield();
        if (GodotObject.IsInstanceValid(_damageImmuneShield))
            _damageImmuneShield.PlayImpact(attacker);
    }
}
