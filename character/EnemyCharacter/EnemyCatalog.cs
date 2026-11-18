using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class EnemyCatalog {
    private static readonly Dictionary<string, Func<EnemyRegedit>> Factories = new() {
        ["Evil"] = () => new EvilRegedit(),
        ["FearWorm"] = () => new FearWormRegedit(),
        ["Armon"] = () => new ArmonRegedit(),
        ["Arrogance"] = () => new ArroganceRegedit(),
        ["AlienBody"] = () => new AlienBodyRegedit(),
        ["RedHusk"] = () => new RedHuskRegedit(),
        ["War"] = () => new WarRegedit(),
        ["Ferociouess"] = () => new FerociouessRegedit(),
        ["Turbine"] = () => new TurbineRegedit(),
        ["BlackHawk"] = () => new BlackHawkRegedit(),
        ["Inexorability"] = () => new InexorabilityRegedit(),
        ["GraveWraith"] = () => new GraveWraithRegedit(),
        ["VoidAcolyte"] = () => new VoidAcolyteRegedit(),
        ["VoidRotor"] = () => new VoidRotorRegedit(),
        ["HollowBulwark"] = () => new HollowBulwarkRegedit(),
        ["MarrowReaver"] = () => new MarrowReaverRegedit(),
        ["FearElite"] = () => new FearEliteRegedit(),
        ["AngerElite"] = () => new AngerEliteRegedit(),
        ["EnvyElite"] = () => new EnvyEliteRegedit(),
        ["Death"] = () => new DeathRegedit(),
        ["Havoc"] = () => new HavocRegedit(),
        ["WarThrall"] = () => new WarThrallRegedit(),
    };

    internal static IEnumerable<string> Keys => Factories.Keys;
    public static bool Contains(string key) => key != null && Factories.ContainsKey(key);
    public static EnemyRegedit Create(string key) => Factories[key]();
    public static EnemyRegedit[] CreateAll() => Factories.Values.Select(factory => factory()).ToArray();
}

// A catalog definition for the summon; battles still spawn the original WarThrall scene.
public partial class WarThrallRegedit : EnemyRegedit {
    public WarThrallRegedit() {
        CharacterName = "战仆";
        PortaitPath = "res://asset/EnemyCharater/War.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/WarThrall.tscn");
        SkillIDs = [SkillID.WarThrallAttack];
        // 战仆只有冲锋，跟随召唤者行动重复执行。
        IntentionCycle = [SkillID.WarThrallAttack];
        PassiveName = "召唤物";
        PassiveDescription = WarThrall.GetPassiveDescription();
    }
}
