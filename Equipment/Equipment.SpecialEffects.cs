using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public partial class Equipment
{
    private const int ShockPendantStunStacks = 1;
    private const int OverloadMarkExtraPowerStacks = 2;
    private const int TauntBadgeTauntStacks = 3;
    private const int PhantomFeatherInvisibleStacks = 2;
    private const int ReserveCoreEnergyGain = 2;

    public static bool HasSpecialEffect(Equipment equipment)
    {
        return !string.IsNullOrWhiteSpace(GetSpecialEffectText(equipment));
    }

    public static string GetSpecialEffectText(Equipment equipment)
    {
        if (equipment == null)
            return string.Empty;

        return equipment.Name switch
        {
            EquipmentName.ShockPendant =>
                $"战斗开始时，如果敌方有与装备者站位相同的角色，令其获得{ShockPendantStunStacks}层眩晕。",
            EquipmentName.OverloadMark =>
                $"战斗开始时，获得{OverloadMarkExtraPowerStacks}层额外力量。",
            EquipmentName.TauntBadge => $"战斗开始时，获得{TauntBadgeTauntStacks}层嘲讽。",
            EquipmentName.PhantomFeather =>
                $"战斗开始时，获得{PhantomFeatherInvisibleStacks}层隐身。",
            EquipmentName.ReserveCore => $"战斗开始时，获得{ReserveCoreEnergyGain}点能量。",
            _ => string.Empty,
        };
    }

    public static string BuildSpecialEffectTooltipSection(IEnumerable<Equipment> equipments)
    {
        if (equipments == null)
            return string.Empty;

        StringBuilder sb = null;
        foreach (var equipment in equipments)
        {
            string effectText = GetSpecialEffectText(equipment);
            if (string.IsNullOrWhiteSpace(effectText))
                continue;

            sb ??= new StringBuilder(128);
            if (sb.Length == 0)
                sb.Append("[b]装备特效[/b]\n");
            else
                sb.Append('\n');

            string name = string.IsNullOrWhiteSpace(equipment.DisplayName)
                ? equipment.Name.ToString()
                : equipment.DisplayName;
            sb.Append($"[color=#ffd36b]{name}[/color]\n");
            sb.Append(effectText);
            sb.Append('\n');
        }

        if (sb == null)
            return string.Empty;

        string text = sb.ToString().TrimEnd();
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }

    public static Task ApplyBattleStartEffects(Battle battle)
    {
        if (battle?.PlayersList == null || battle.EnemiesList == null)
            return Task.CompletedTask;

        foreach (var player in battle.PlayersList)
        {
            if (!TryGetEquippedItems(player, out var equipments))
                continue;

            ApplyBattleStartEffects(player, battle, equipments);
        }

        return Task.CompletedTask;
    }

    private static bool TryGetEquippedItems(PlayerCharacter player, out Equipment[] equipments)
    {
        equipments = null;
        if (player == null || player.State == Character.CharacterState.Dying)
            return false;

        if (
            player.CharacterIndex < 0
            || GameInfo.PlayerCharacters == null
            || player.CharacterIndex >= GameInfo.PlayerCharacters.Length
        )
        {
            return false;
        }

        equipments = GameInfo.PlayerCharacters[player.CharacterIndex].Equipments;
        return equipments != null && equipments.Length > 0;
    }

    private static void ApplyBattleStartEffects(
        PlayerCharacter player,
        Battle battle,
        Equipment[] equipments
    )
    {
        HashSet<EquipmentName> triggeredEffects = new();
        foreach (var equipment in equipments)
        {
            if (equipment == null || !triggeredEffects.Add(equipment.Name))
                continue;

            ApplyBattleStartEffect(equipment.Name, player, battle);
        }
    }

    private static void ApplyBattleStartEffect(
        EquipmentName equipmentName,
        PlayerCharacter player,
        Battle battle
    )
    {
        switch (equipmentName)
        {
            case EquipmentName.ShockPendant:
                ApplyShockPendantEffect(player, battle.EnemiesList);
                break;
            case EquipmentName.OverloadMark:
                ApplyOverloadMarkEffect(player);
                break;
            case EquipmentName.TauntBadge:
                ApplyTauntBadgeEffect(player);
                break;
            case EquipmentName.PhantomFeather:
                ApplyPhantomFeatherEffect(player);
                break;
            case EquipmentName.ReserveCore:
                ApplyReserveCoreEffect(player);
                break;
        }
    }

    private static void ApplyShockPendantEffect(
        PlayerCharacter player,
        IEnumerable<EnemyCharacter> enemies
    )
    {
        if (player == null || enemies == null)
            return;

        foreach (var enemy in enemies)
        {
            if (
                enemy == null
                || enemy.State == Character.CharacterState.Dying
                || enemy.PositionIndex != player.PositionIndex
            )
            {
                continue;
            }

            SkillBuff.BuffAdd(Buff.BuffName.Stun, enemy, ShockPendantStunStacks, player);
        }
    }

    private static void ApplyOverloadMarkEffect(PlayerCharacter player)
    {
        if (player == null)
            return;

        SpecialBuff.BuffAdd(Buff.BuffName.ExtraPower, player, OverloadMarkExtraPowerStacks, player);
    }

    private static void ApplyTauntBadgeEffect(PlayerCharacter player)
    {
        if (player == null)
            return;

        HurtBuff.BuffAdd(Buff.BuffName.Taunt, player, TauntBadgeTauntStacks, player);
    }

    private static void ApplyPhantomFeatherEffect(PlayerCharacter player)
    {
        if (player == null)
            return;

        StartActionBuff.BuffAdd(
            Buff.BuffName.Invisible,
            player,
            PhantomFeatherInvisibleStacks,
            player
        );
    }

    private static void ApplyReserveCoreEffect(PlayerCharacter player)
    {
        if (player == null)
            return;

        player.UpdataEnergy(ReserveCoreEnergyGain, player);
    }
}
