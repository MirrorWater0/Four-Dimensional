using System;

public partial class Skill
{

    protected int DamageFromPower(int baseDamage = 0, int powerMultiplier = 1, int clampMax = 9999)
    {
        int damage = baseDamage + OwnerPower * powerMultiplier;
        return Math.Clamp(damage, 0, clampMax);
    }

    protected int BlockFromSurvivability(
        int baseBlock = 0,
        int survivabilityMultiplier = 1,
        int clampMax = 999
    )
    {
        int block = baseBlock + OwnerSurvivability * survivabilityMultiplier;
        return Math.Clamp(block, 0, clampMax);
    }

    protected bool TrySpendEnergy(int cost)
    {
        if (OwnerCharater == null)
            return false;
        if (OwnerCharater.Energy < cost)
            return false;
        OwnerCharater.UpdataEnergy(-cost);
        return true;
    }
}
