using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Relic
{
    private const int BlessingDamage = 20;
    private const int MatrixShieldBlock = 15;
    private const int BackpackExtraDrawStacks = 1;
    private const int EnergyTankEnergy = 1;
    private const int FusionCoreActionInterval = 3;
    private const int FusionCoreEnergy = 1;
    private const int PhilosophersStoneEnergyBonus = 1;
    private const int PhilosophersStoneEnemyPower = 4;
    private const int EternalGlassDrawBonus = 1;
    private const int KingsSwordPartyPower = 4;
    private const int RefractometerDamageImmuneStacks = 1;
    private const int PurifierDebuffImmunityStacks = 1;
    private const int PulseControllerStunStacks = 1;
    private const int PulseControllerVulnerableStacks = 3;
    private const int PulseControllerWeakenStacks = 3;
    private const int KnightHelmetMinimumCost = 2;
    private const int KnightHelmetBlock = 6;

    public RelicID ID;
    public string RelicName;
    public string RelicDescription;
    public int Num = -1;
    public Control IconNode;
    public static PackedScene IconScene = GD.Load<PackedScene>("res://Relic/RelicIcon.tscn");

    public Relic(RelicID relicID)
    {
        ID = relicID;
        RelicName = GetRelicName(relicID);
        RelicDescription = GetRelicDescription(relicID);
    }

    public static RelicID[] GetUnownedOfferPool()
    {
        var ownedRelics = GameInfo.Relics;
        List<RelicID> result = new();
        RelicID[] pool =
        {
            RelicID.Blessing,
            RelicID.Triangle,
            RelicID.Square,
            RelicID.Pentagon,
            RelicID.Hexagon,
            RelicID.Heptagon,
            RelicID.Octagon,
            RelicID.CompressionCore,
            RelicID.MatrixShield,
            RelicID.Backpack,
            RelicID.EnergyTank,
            RelicID.FusionCore,
            RelicID.Refractometer,
            RelicID.Purifier,
            RelicID.KnightHelmet,
        };

        for (int i = 0; i < pool.Length; i++)
        {
            var relicId = pool[i];
            if (ownedRelics == null || !ownedRelics.ContainsKey(relicId))
                result.Add(relicId);
        }

        return result.ToArray();
    }

    public static Relic Create(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => new Relic(RelicID.Blessing),
            RelicID.Triangle => new Relic(RelicID.Triangle),
            RelicID.Square => new Relic(RelicID.Square),
            RelicID.Pentagon => new Relic(RelicID.Pentagon),
            RelicID.Hexagon => new Relic(RelicID.Hexagon),
            RelicID.Heptagon => new Relic(RelicID.Heptagon),
            RelicID.Octagon => new Relic(RelicID.Octagon),
            RelicID.CompressionCore => new Relic(RelicID.CompressionCore),
            RelicID.MatrixShield => new Relic(RelicID.MatrixShield),
            RelicID.Backpack => new Relic(RelicID.Backpack),
            RelicID.EnergyTank => new Relic(RelicID.EnergyTank),
            RelicID.FusionCore => new Relic(RelicID.FusionCore),
            RelicID.Refractometer => new Relic(RelicID.Refractometer),
            RelicID.Purifier => new Relic(RelicID.Purifier),
            RelicID.KnightHelmet => new Relic(RelicID.KnightHelmet),
            RelicID.PhilosophersStone => new Relic(RelicID.PhilosophersStone),
            RelicID.EternalGlass => new Relic(RelicID.EternalGlass),
            RelicID.KingsSword => new Relic(RelicID.KingsSword),
            RelicID.PulseController => new Relic(RelicID.PulseController),
            _ => new Relic(RelicID.curse),
        };
    }

    public static RelicID[] GetBossRelicOfferPool()
    {
        return [
            RelicID.PhilosophersStone,
            RelicID.EternalGlass,
            RelicID.KingsSword,
            RelicID.PulseController,
        ];
    }

    public static int GetAcquireAmount(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => 3,
            RelicID.FusionCore => 0,
            _ => -1,
        };
    }

    public static string GetIconShaderPath(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => "res://shader/Icon/RelicIcon/Point.gdshader",
            RelicID.Triangle => "res://shader/Icon/RelicIcon/Triangle.gdshader",
            RelicID.Square => "res://shader/Icon/RelicIcon/Square.gdshader",
            RelicID.Pentagon => "res://shader/Icon/RelicIcon/Pentagon.gdshader",
            RelicID.Hexagon => "res://shader/Icon/RelicIcon/Hexagon.gdshader",
            RelicID.Heptagon => "res://shader/Icon/RelicIcon/Heptagon.gdshader",
            RelicID.Octagon => "res://shader/Icon/RelicIcon/Octagon.gdshader",
            RelicID.CompressionCore => "res://shader/Icon/RelicIcon/CompressionCore.gdshader",
            RelicID.PhilosophersStone => "res://shader/Icon/RelicIcon/Hexagon.gdshader",
            RelicID.EternalGlass => "res://shader/Icon/RelicIcon/Octagon.gdshader",
            RelicID.KingsSword => "res://shader/Icon/RelicIcon/Pentagon.gdshader",
            _ => null,
        };
    }

    public static string GetIconTexturePath(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.MatrixShield => "res://asset/svg/RelicIcon/MatrixShield.svg",
            RelicID.Backpack => "res://asset/svg/RelicIcon/Backpack.svg",
            RelicID.EnergyTank => "res://asset/svg/RelicIcon/EnergyTank.svg",
            RelicID.FusionCore => "res://asset/svg/RelicIcon/FusionCore.svg",
            RelicID.Refractometer => "res://asset/svg/RelicIcon/Refractometer.svg",
            RelicID.Purifier => "res://asset/svg/RelicIcon/Purifier.svg",
            RelicID.KnightHelmet => "res://asset/svg/RelicIcon/KnightHelmet.svg",
            RelicID.PhilosophersStone => "res://asset/svg/RelicIcon/PhilosophersStone.svg",
            RelicID.EternalGlass => "res://asset/svg/RelicIcon/EternalGlass.svg",
            RelicID.KingsSword => "res://asset/svg/RelicIcon/KingsSword.svg",
            RelicID.PulseController => "res://asset/svg/RelicIcon/PulseController.svg",
            _ => null,
        };
    }

    public static void ApplyIconVisual(Control icon, RelicID relicID)
    {
        if (icon == null)
            return;

        ClearGeneratedTextureIcon(icon);

        string texturePath = GetIconTexturePath(relicID);
        Texture2D texture = string.IsNullOrWhiteSpace(texturePath)
            ? null
            : GD.Load<Texture2D>(texturePath);
        if (texture != null)
        {
            if (icon is ColorRect colorRect)
            {
                colorRect.Color = Colors.Transparent;
                colorRect.Material = null;
            }

            var textureIcon = new TextureRect
            {
                Name = "GeneratedTextureIcon",
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            textureIcon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            textureIcon.OffsetLeft = 1f;
            textureIcon.OffsetTop = 1f;
            textureIcon.OffsetRight = -1f;
            textureIcon.OffsetBottom = -1f;
            icon.AddChild(textureIcon);
            icon.MoveChild(textureIcon, 0);
            return;
        }

        if (icon is ColorRect shaderIcon)
        {
            shaderIcon.Color = Colors.White;
            string shaderPath = GetIconShaderPath(relicID);
            var shader = string.IsNullOrWhiteSpace(shaderPath) ? null : GD.Load<Shader>(shaderPath);
            shaderIcon.Material = shader == null ? null : new ShaderMaterial { Shader = shader };
        }
    }

    public static string FormatCountLabel(int count)
    {
        return count < 0 ? string.Empty : count.ToString();
    }

    public static void RelicAdd(PlayerResourceState playerResourceState, RelicID relicID)
    {
        Relic relic = Create(relicID);
        int num = GetAcquireAmount(relicID);
        relic.Num = num;
        relic.IconAdd(playerResourceState);
        playerResourceState.RelicList ??= new List<Relic>();
        playerResourceState.RelicList.Add(relic);
        GameInfo.Relics[relicID] = num;
        ApplyAcquireEffect(relicID);
    }

    public static int ApplyElectricityCoinBonus(int baseAmount)
    {
        if (baseAmount <= 0)
            return 0;

        if (GameInfo.Relics == null || !GameInfo.Relics.ContainsKey(RelicID.CompressionCore))
            return baseAmount;

        return Mathf.CeilToInt(baseAmount * 1.2f);
    }

    public static void ApplyPlayerActionStartRelicEffects(
        Battle battle,
        Character actingCharacter,
        int playerActionNumber
    )
    {
        if (
            battle?.MapNode?.PlayerResourceState?.RelicList == null
            || actingCharacter == null
            || !actingCharacter.IsPlayer
            || playerActionNumber <= 0
        )
        {
            return;
        }

        foreach (var relic in battle.MapNode.PlayerResourceState.RelicList)
        {
            if (relic == null)
                continue;

            if (relic.ID == RelicID.FusionCore)
            {
                relic.Num = Math.Max(0, relic.Num) + 1;
                if (relic.Num >= FusionCoreActionInterval)
                {
                    ApplyFusionCoreActionStart(actingCharacter);
                    relic.Num = 0;
                }
                relic.UpdateIconLabel();
            }
        }
    }

    public static int GetTurnStartEnergyGainBonus(Character character)
    {
        if (character is not PlayerCharacter)
            return 0;

        return HasRelic(RelicID.PhilosophersStone) ? PhilosophersStoneEnergyBonus : 0;
    }

    public static int GetTurnStartDrawBonus(Battle battle)
    {
        return HasRelic(RelicID.EternalGlass) ? EternalGlassDrawBonus : 0;
    }

    public static void ApplySkillUsedRelicEffects(Skill skill)
    {
        if (
            skill?.OwnerCharater == null
            || !skill.OwnerCharater.IsPlayer
            || skill.OwnerCharater.State == Character.CharacterState.Dying
            || skill.CardEnergyCost < KnightHelmetMinimumCost
            || !HasRelic(RelicID.KnightHelmet)
        )
        {
            return;
        }

        using var _ = skill.OwnerCharater.BeginEffectSource(GetRelicName(RelicID.KnightHelmet));
        skill.OwnerCharater.UpdataBlock(KnightHelmetBlock, source: skill.OwnerCharater);
    }

    private static bool HasRelic(RelicID relicId) =>
        GameInfo.Relics != null && GameInfo.Relics.ContainsKey(relicId);

    public void IconAdd(PlayerResourceState playerResourceState)
    {
        var icon = IconScene.Instantiate() as ColorRect;
        ApplyIconVisual(icon, ID);
        icon.GetNode<Label>("Label").Text = GetIconCountText();
        WireRelicTip(icon, playerResourceState);
        playerResourceState.RelicContainer.AddChild(icon);
        IconNode = icon;
    }

    public async Task BattleEffect(Battle battle)
    {
        switch (ID)
        {
            case RelicID.Blessing:
                if (Num <= 0)
                    return;
                List<Task> list = new();
                for (int i = 0; i < battle.EnemiesList.Count; i++)
                    list.Add(battle.EnemiesList[i].GetHurt(BlessingDamage));
                await Task.WhenAll(list);
                Num--;
                break;
            case RelicID.Triangle:
                await ApplyEffectToFrontPlayers(battle, PropertyType.Survivability, 2);
                break;
            case RelicID.Square:
                await ApplyEffectToFrontPlayers(battle, PropertyType.Power, 2);
                break;
            case RelicID.Pentagon:
                await ApplyEffectToFrontPlayers(battle, PropertyType.Speed, 2);
                break;
            case RelicID.Hexagon:
                await ApplyHexagonEffect(battle);
                break;
            case RelicID.Heptagon:
                ApplyDebuffToEnemies(battle, Buff.BuffName.Vulnerable, 1);
                break;
            case RelicID.Octagon:
                ApplyDebuffToEnemies(battle, Buff.BuffName.Weaken, 1);
                break;
            case RelicID.CompressionCore:
                break;
            case RelicID.MatrixShield:
                ApplyBlockToPlayers(battle, MatrixShieldBlock);
                break;
            case RelicID.Backpack:
                ApplyExtraDrawToFrontPlayers(battle, BackpackExtraDrawStacks);
                break;
            case RelicID.EnergyTank:
                ApplyEnergyToFrontPlayers(battle, EnergyTankEnergy);
                break;
            case RelicID.FusionCore:
                break;
            case RelicID.Refractometer:
                ApplyRefractometerEffect(battle);
                break;
            case RelicID.Purifier:
                ApplyDebuffImmunityToPlayers(battle, PurifierDebuffImmunityStacks);
                break;
            case RelicID.KnightHelmet:
                break;
            case RelicID.PhilosophersStone:
                await ApplyPowerToEnemies(battle, PhilosophersStoneEnemyPower);
                break;
            case RelicID.EternalGlass:
            case RelicID.KingsSword:
                break;
            case RelicID.PulseController:
                ApplyPulseControllerEffect(battle);
                break;
            case RelicID.curse:
                break;
        }

        GameInfo.Relics[ID] = Num;
        UpdateIconLabel();
        UpdateRelicTipText();
    }

    private string BuildTooltip()
    {
        string name = Colorize(RelicName, NameColorHex);
        string description = ColorizeNumbers(RelicDescription, NumberColorHex);
        return $"{name}\n{description}";
    }

    public void UpdateIconLabel()
    {
        if (IconNode == null)
            return;

        IconNode.GetNode<Label>("Label").Text = GetIconCountText();
    }

    private static string GetRelicName(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => "祝福",
            RelicID.Triangle => "三角形",
            RelicID.Square => "正方形",
            RelicID.Pentagon => "正五边形",
            RelicID.Hexagon => "正六边形",
            RelicID.Heptagon => "七边形",
            RelicID.Octagon => "八边形",
            RelicID.CompressionCore => "压缩核心",
            RelicID.MatrixShield => "矩阵护盾",
            RelicID.Backpack => "背包",
            RelicID.EnergyTank => "能量储罐",
            RelicID.FusionCore => "聚变核心",
            RelicID.Refractometer => "折射仪",
            RelicID.Purifier => "净化器",
            RelicID.KnightHelmet => "骑士头盔",
            RelicID.PhilosophersStone => "贤者之石",
            RelicID.EternalGlass => "亘古琉璃",
            RelicID.KingsSword => "王者之剑",
            RelicID.PulseController => "脉冲控制仪",
            _ => "诅咒",
        };
    }

    private static string GetRelicDescription(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => $"战斗开始时对所有敌人造成{BlessingDamage}伤害。",
            RelicID.Triangle => "战斗开始时第一位和第二位角色获得2点生存。",
            RelicID.Square => "战斗开始时第一位和第二位角色获得2点力量。",
            RelicID.Pentagon => "战斗开始时第一位和第二位角色获得2点速度。",
            RelicID.Hexagon => "战斗开始时第一位和第二位角色获得8点血量上限,并回复0点。",
            RelicID.Heptagon => "战斗开始时，敌方全阵获得1层易伤。",
            RelicID.Octagon => "战斗开始时，敌方全阵获得1层虚弱。",
            RelicID.CompressionCore => "获得的电力币增加20%。",
            RelicID.MatrixShield => $"战斗开始时全阵获得{MatrixShieldBlock}点格挡。",
            RelicID.Backpack => $"战斗开始时前2位角色获得{BackpackExtraDrawStacks}层额外抽卡。",
            RelicID.EnergyTank => $"战斗开始时前2位角色获得{EnergyTankEnergy}点能量。",
            RelicID.FusionCore => $"己方行动每{FusionCoreActionInterval}次行动当前角色获得{FusionCoreEnergy}点能量。",
            RelicID.Refractometer => $"战斗开始时随机一名角色获得{RefractometerDamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。",
            RelicID.Purifier => $"战斗开始时全阵获得{PurifierDebuffImmunityStacks}层{Buff.BuffName.DebuffImmunity.GetDescription()}。",
            RelicID.KnightHelmet => $"每当角色打出{KnightHelmetMinimumCost}费及以上的卡牌时，获得{KnightHelmetBlock}点格挡。",
            RelicID.PhilosophersStone => $"回合开始时获得的能量+{PhilosophersStoneEnergyBonus}。战斗开始时所有敌人获得{PhilosophersStoneEnemyPower}点力量。",
            RelicID.EternalGlass => $"回合开始时抽牌数+{EternalGlassDrawBonus}。",
            RelicID.KingsSword => $"拾起时全体角色增加{KingsSwordPartyPower}点力量。",
            RelicID.PulseController => $"战斗开始时随机一名敌人获得{PulseControllerStunStacks}层{Buff.BuffName.Stun.GetDescription()}、{PulseControllerVulnerableStacks}层{Buff.BuffName.Vulnerable.GetDescription()}和{PulseControllerWeakenStacks}层{Buff.BuffName.Weaken.GetDescription()}。",
            _ => "暂无效果。",
        };
    }

    private static async Task ApplyEffectToFrontPlayers(
        Battle battle,
        PropertyType propertyType,
        int amount
    )
    {
        int targetCount = Math.Min(2, battle.PlayersList.Count);
        for (int i = 0; i < targetCount; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            await player.IncreaseProperties(propertyType, amount);
        }
    }

    private static async Task ApplyHexagonEffect(Battle battle)
    {
        int targetCount = Math.Min(2, battle.PlayersList.Count);
        for (int i = 0; i < targetCount; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            await player.IncreaseProperties(PropertyType.MaxLife, 8);
            player.Recover(2);
        }
    }

    private static void ApplyDebuffToEnemies(Battle battle, Buff.BuffName buffName, int stacks)
    {
        if (battle?.EnemiesList == null)
            return;

        for (int i = 0; i < battle.EnemiesList.Count; i++)
        {
            var enemy = battle.EnemiesList[i];
            if (enemy == null || enemy.State == Character.CharacterState.Dying)
                continue;

            switch (buffName)
            {
                case Buff.BuffName.Vulnerable:
                    HurtBuff.BuffAdd(buffName, enemy, stacks);
                    break;
                case Buff.BuffName.Weaken:
                    AttackBuff.BuffAdd(buffName, enemy, stacks);
                    break;
            }
        }
    }

    private static void ApplyBlockToPlayers(Battle battle, int block)
    {
        if (battle?.PlayersList == null || block <= 0)
            return;

        for (int i = 0; i < battle.PlayersList.Count; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            player.UpdataBlock(block);
        }
    }

    private static void ApplyEnergyToFrontPlayers(Battle battle, int energy)
    {
        if (battle?.PlayersList == null || energy == 0)
            return;

        int targetCount = Math.Min(2, battle.PlayersList.Count);
        for (int i = 0; i < targetCount; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            player.UpdataEnergy(energy, player);
        }
    }

    private static void ApplyRefractometerEffect(Battle battle)
    {
        if (battle?.PlayersList == null)
            return;

        Character[] candidates = battle
            .PlayersList.Where(player =>
                player != null
                && GodotObject.IsInstanceValid(player)
                && player.State != Character.CharacterState.Dying
            )
            .Cast<Character>()
            .ToArray();
        if (candidates.Length == 0)
            return;

        int seed = (battle.CurrentLevelNode?.RandomNum ?? GameInfo.Seed) ^ unchecked((int)0x4EF1AC70);
        var rng = new Random(seed);
        Character target = candidates[rng.Next(candidates.Length)];
        HurtBuff.BuffAdd(
            Buff.BuffName.DamageImmune,
            target,
            RefractometerDamageImmuneStacks,
            target
        );
    }

    private static void ApplyDebuffImmunityToPlayers(Battle battle, int stacks)
    {
        if (battle?.PlayersList == null || stacks <= 0)
            return;

        for (int i = 0; i < battle.PlayersList.Count; i++)
        {
            var player = battle.PlayersList[i];
            if (
                player == null
                || !GodotObject.IsInstanceValid(player)
                || player.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            SpecialBuff.BuffAdd(Buff.BuffName.DebuffImmunity, player, stacks, player);
        }
    }

    private static void ApplyPulseControllerEffect(Battle battle)
    {
        if (battle?.EnemiesList == null)
            return;

        Character[] candidates = battle
            .EnemiesList.Where(enemy =>
                enemy != null
                && GodotObject.IsInstanceValid(enemy)
                && enemy.State != Character.CharacterState.Dying
            )
            .Cast<Character>()
            .ToArray();
        if (candidates.Length == 0)
            return;

        int seed = (battle.CurrentLevelNode?.RandomNum ?? GameInfo.Seed) ^ unchecked((int)0x51A7C011);
        var rng = new Random(seed);
        Character target = candidates[rng.Next(candidates.Length)];
        SkillBuff.BuffAdd(Buff.BuffName.Stun, target, PulseControllerStunStacks, target);
        HurtBuff.BuffAdd(Buff.BuffName.Vulnerable, target, PulseControllerVulnerableStacks, target);
        AttackBuff.BuffAdd(Buff.BuffName.Weaken, target, PulseControllerWeakenStacks, target);
    }

    private static void ApplyExtraDrawToFrontPlayers(Battle battle, int stacks)
    {
        if (battle?.PlayersList == null || stacks <= 0)
            return;

        int targetCount = Math.Min(2, battle.PlayersList.Count);
        for (int i = 0; i < targetCount; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            SpecialBuff.BuffAdd(Buff.BuffName.ExtraDraw, player, stacks, player);
        }
    }

    private static async Task ApplyPowerToEnemies(Battle battle, int power)
    {
        if (battle?.EnemiesList == null || power == 0)
            return;

        foreach (var enemy in battle.EnemiesList.ToArray())
        {
            if (enemy == null || enemy.State == Character.CharacterState.Dying)
                continue;

            await enemy.IncreaseProperties(PropertyType.Power, power, enemy);
        }
    }

    private static void ApplyAcquireEffect(RelicID relicID)
    {
        if (relicID != RelicID.KingsSword)
            return;

        if (GameInfo.PlayerCharacters == null)
            return;

        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var info = GameInfo.PlayerCharacters[i];
            info.Power += KingsSwordPartyPower;
            GameInfo.PlayerCharacters[i] = info;
        }
    }

    private static void ApplyFusionCoreActionStart(Character actingCharacter)
    {
        if (actingCharacter == null)
            return;

        using var _ = actingCharacter.BeginEffectSource(GetRelicName(RelicID.FusionCore));
        actingCharacter.UpdataEnergy(FusionCoreEnergy, actingCharacter);
    }

    private string GetIconCountText()
    {
        return FormatCountLabel(Num);
    }

    private void WireRelicTip(Control icon, PlayerResourceState playerResourceState)
    {
        if (icon == null || playerResourceState == null)
            return;

        var tip = GetOrCreateRelicTip(playerResourceState);
        if (tip == null)
            return;

        icon.MouseEntered += () =>
        {
            tip.FollowMouse = true;
            tip.Description.Text = BuildTooltip();
            tip.Visible = true;
        };
        icon.MouseExited += () =>
        {
            if (tip != null)
                tip.Visible = false;
        };
    }

    private void UpdateRelicTipText()
    {
        if (IconNode == null)
            return;
        var tip = GetOrCreateRelicTip(IconNode);
        if (tip == null || !tip.Visible)
            return;
        tip.Description.Text = BuildTooltip();
    }

    private static Tip GetOrCreateRelicTip(Node context)
    {
        var root = context?.GetTree()?.Root;
        if (root == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            if (root.IsInsideTree())
                root.AddChild(layer);
            else
                root.CallDeferred(Node.MethodName.AddChild, layer);
        }

        var existing = layer.GetNodeOrNull<Tip>("RelicTip");
        if (existing != null)
            return existing;

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return null;

        var tip = tipScene.Instantiate<Tip>();
        tip.Name = "RelicTip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 20f);
        layer.AddChild(tip);
        return tip;
    }

    private static void ClearGeneratedTextureIcon(Control icon)
    {
        var existing = icon.GetNodeOrNull<TextureRect>("GeneratedTextureIcon");
        if (existing != null)
            existing.QueueFree();
    }

    private const string NameColorHex = "#b78cff";
    private const string NumberColorHex = "#ffd24a";

    private static string Colorize(string text, string colorHex)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        return $"[color={colorHex}]{text}[/color]";
    }

    private static string ColorizeNumbers(string input, string colorHex)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var builder = new System.Text.StringBuilder(input.Length * 2);
        bool inTag = false;

        for (int i = 0; i < input.Length; i++)
        {
            char ch = input[i];

            if (ch == '[')
            {
                inTag = true;
                builder.Append(ch);
                continue;
            }

            if (ch == ']')
            {
                inTag = false;
                builder.Append(ch);
                continue;
            }

            if (!inTag && char.IsDigit(ch))
            {
                int start = i;
                while (i < input.Length && char.IsDigit(input[i]))
                    i++;

                builder.Append($"[color={colorHex}]");
                builder.Append(input, start, i - start);
                builder.Append("[/color]");
                i--;
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}

public enum RelicID
{
    Blessing,
    Triangle,
    Square,
    Pentagon,
    Hexagon,
    Heptagon,
    Octagon,
    CompressionCore,
    MatrixShield,
    Backpack,
    EnergyTank,
    FusionCore,
    Refractometer,
    Purifier,
    KnightHelmet,
    PhilosophersStone,
    EternalGlass,
    KingsSword,
    PulseController,
    curse,
}
