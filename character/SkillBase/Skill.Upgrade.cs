public partial class Skill
{
    public bool TryUpgrade()
    {
        if (Upgraded)
            return false;

        Upgraded = true;
        OnUpgrade();
        _cachedPlan = null;
        UpdateDescription();
        return true;
    }

    public void Upgrade()
    {
        _ = TryUpgrade();
    }

    protected virtual void OnUpgrade() { }

    protected int UpAdd(int baseValue, int upgradeDelta) =>
        baseValue + (Upgraded ? upgradeDelta : 0);

    protected int UpTo(int baseValue, int upgradedValue) => Upgraded ? upgradedValue : baseValue;
}
