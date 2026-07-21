using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Relic
{
    private const int BlessingDamage = 35;
    private const int MatrixShieldBlock = 7;
    private const int BackpackFirstTurnDrawBonus = 2;
    private const int EnergyTankEnergy = 1;
    private const int EnergyStorageTankStacks = 1;
    private const int FusionCoreActionInterval = 3;
    private const int FusionCoreEnergy = 1;
    private const int SpiralAcceleratorTurnInterval = 3;
    private const int SpiralAcceleratorDrawCount = 1;
    private const int MechanicalEnergyConverterEnergy = 1;
    private const int PhilosophersStoneEnergyBonus = 1;
    private const int PhilosophersStoneEnemyPower = 1;
    private const int EternalGlassDrawBonus = 1;
    private const int BattleBannerPartyPower = 1;
    private const int GuardBadgePartySurvivability = 1;
    private const int VitalityCapsulePartyMaxLife = 5;
    private const int KingsSwordPartyPower = 3;
    private const int RefractometerDamageImmuneStacks = 1;
    private const int PurifierDebuffImmunityStacks = 1;
    private const int PulseControllerStunStacks = 1;
    private const int KnightHelmetMinimumCost = 2;
    private const int KnightHelmetBlock = 5;
    private const int IonizedVoiceSpecialSkillInterval = 3;
    private const int IonizedVoiceDrawCount = 1;
    private const int HealingBeaconBattleEndHeal = 5;
    private const float MembershipCardShopPriceMultiplier = 0.5f;
    private const int ToolboxSelectionCount = 2;
    private const float TriggerPopupIconSize = 60f;
    private const float TriggerPopupHeadOffset = -208f;
    private const float TriggerPopupRise = -82f;

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

    /// <summary>仅开局三选一可获得的遗物，不参与战斗/商店/事件等常规掉落。</summary>
    public static RelicID[] GetStarterBonusRelicPool() => [RelicID.Blessing];

    public static RelicID[] GetStandardOfferPool()
    {
        return
        [
            RelicID.BattleBanner,
            RelicID.GuardBadge,
            RelicID.VitalityCapsule,
            RelicID.HealingBeacon,
            RelicID.ArmorBreaker,
            RelicID.WeakeningEye,
            RelicID.CompressionCore,
            RelicID.MembershipCard,
            RelicID.Toolbox,
            RelicID.MatrixShield,
            RelicID.Backpack,
            RelicID.EnergyTank,
            RelicID.EnergyStorageTank,
            RelicID.FusionCore,
            RelicID.SpiralAccelerator,
            RelicID.MechanicalEnergyConverter,
            RelicID.Refractometer,
            RelicID.Purifier,
            RelicID.KnightHelmet,
            RelicID.IonizedVoice,
        ];
    }

    public static RelicID[] GetUnownedOfferPool()
    {
        List<RelicID> result = new();
        RelicID[] pool = GetStandardOfferPool();

        for (int i = 0; i < pool.Length; i++)
        {
            var relicId = pool[i];
            if (!GameInfo.HasRelic(relicId))
                result.Add(relicId);
        }

        return result.ToArray();
    }

    public static Relic Create(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => new Relic(RelicID.Blessing),
            RelicID.BattleBanner => new Relic(RelicID.BattleBanner),
            RelicID.GuardBadge => new Relic(RelicID.GuardBadge),
            RelicID.VitalityCapsule => new Relic(RelicID.VitalityCapsule),
            RelicID.HealingBeacon => new Relic(RelicID.HealingBeacon),
            RelicID.ArmorBreaker => new Relic(RelicID.ArmorBreaker),
            RelicID.WeakeningEye => new Relic(RelicID.WeakeningEye),
            RelicID.CompressionCore => new Relic(RelicID.CompressionCore),
            RelicID.MembershipCard => new Relic(RelicID.MembershipCard),
            RelicID.Toolbox => new Relic(RelicID.Toolbox),
            RelicID.MatrixShield => new Relic(RelicID.MatrixShield),
            RelicID.Backpack => new Relic(RelicID.Backpack),
            RelicID.EnergyTank => new Relic(RelicID.EnergyTank),
            RelicID.EnergyStorageTank => new Relic(RelicID.EnergyStorageTank),
            RelicID.FusionCore => new Relic(RelicID.FusionCore),
            RelicID.SpiralAccelerator => new Relic(RelicID.SpiralAccelerator),
            RelicID.MechanicalEnergyConverter => new Relic(RelicID.MechanicalEnergyConverter),
            RelicID.Refractometer => new Relic(RelicID.Refractometer),
            RelicID.Purifier => new Relic(RelicID.Purifier),
            RelicID.KnightHelmet => new Relic(RelicID.KnightHelmet),
            RelicID.IonizedVoice => new Relic(RelicID.IonizedVoice),
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
            RelicID.SpiralAccelerator => 0,
            RelicID.IonizedVoice => 0,
            _ => -1,
        };
    }

    public static string GetIconShaderPath(RelicID relicID)
    {
        return relicID switch
        {
            RelicID.Blessing => "res://shader/Icon/RelicIcon/Point.gdshader",
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
            RelicID.Blessing => "res://asset/svg/RelicIcon/Blessing.svg",
            RelicID.BattleBanner => "res://asset/svg/RelicIcon/BattleBanner.svg",
            RelicID.GuardBadge => "res://asset/svg/RelicIcon/GuardBadge.svg",
            RelicID.VitalityCapsule => "res://asset/svg/RelicIcon/VitalityCapsule.svg",
            RelicID.HealingBeacon => "res://asset/svg/RelicIcon/HealingBeacon.svg",
            RelicID.ArmorBreaker => "res://asset/svg/RelicIcon/ArmorBreaker.svg",
            RelicID.WeakeningEye => "res://asset/svg/RelicIcon/WeakeningEye.svg",
            RelicID.CompressionCore => "res://asset/svg/RelicIcon/CompressionCore.svg",
            RelicID.MembershipCard => "res://asset/svg/RelicIcon/MembershipCard.svg",
            RelicID.Toolbox => "res://asset/svg/RelicIcon/Toolbox.svg",
            RelicID.MatrixShield => "res://asset/svg/RelicIcon/MatrixShield.svg",
            RelicID.Backpack => "res://asset/svg/RelicIcon/Backpack.svg",
            RelicID.EnergyTank => "res://asset/svg/RelicIcon/EnergyTank.svg",
            RelicID.EnergyStorageTank => "res://asset/svg/RelicIcon/EnergyStorageTank.svg",
            RelicID.FusionCore => "res://asset/svg/RelicIcon/FusionCore.svg",
            RelicID.SpiralAccelerator => "res://asset/svg/RelicIcon/SpiralAccelerator.svg",
            RelicID.MechanicalEnergyConverter => "res://asset/svg/RelicIcon/MechanicalEnergyConverter.svg",
            RelicID.Refractometer => "res://asset/svg/RelicIcon/Refractometer.svg",
            RelicID.Purifier => "res://asset/svg/RelicIcon/Purifier.svg",
            RelicID.KnightHelmet => "res://asset/svg/RelicIcon/KnightHelmet.svg",
            RelicID.IonizedVoice => "res://asset/svg/RelicIcon/IonizedVoice.svg",
            RelicID.PhilosophersStone => "res://asset/svg/RelicIcon/PhilosophersStone.svg",
            RelicID.EternalGlass => "res://asset/svg/RelicIcon/EternalGlass.svg",
            RelicID.KingsSword => "res://asset/svg/RelicIcon/KingsSword.svg",
            RelicID.PulseController => "res://asset/svg/RelicIcon/PulseController.svg",
            RelicID.curse => "res://asset/svg/RelicIcon/Curse.svg",
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
        GameInfo.SetRelicCount(relicID, num);
        ApplyAcquireEffect(relicID);
        if (relicID == RelicID.Toolbox)
            _ = RunToolboxSelectionAsync(playerResourceState);
    }

    public static int ApplyElectricityCoinBonus(int baseAmount)
    {
        if (baseAmount <= 0)
            return 0;

        if (!GameInfo.HasRelic(RelicID.CompressionCore))
            return baseAmount;

        return Mathf.CeilToInt(baseAmount * 1.2f);
    }

    public static int CalculateShopPrice(int basePrice)
    {
        if (basePrice <= 0)
            return 0;

        if (!GameInfo.HasRelic(RelicID.MembershipCard))
            return basePrice;

        return Math.Max(1, Mathf.CeilToInt(basePrice * MembershipCardShopPriceMultiplier));
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

            if (relic.ID == RelicID.MatrixShield && playerActionNumber == 1)
            {
                ApplyBlockToPlayers(battle, MatrixShieldBlock, relic.ID);
                continue;
            }

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

    public static void ApplyPlayerTeamTurnStartRelicEffects(
        Battle battle,
        Character teamContext,
        bool refreshUi = true
    )
    {
        if (
            battle?.MapNode?.PlayerResourceState?.RelicList == null
            || teamContext == null
            || !teamContext.IsPlayer
            || teamContext.State == Character.CharacterState.Dying
        )
        {
            return;
        }

        foreach (var relic in battle.MapNode.PlayerResourceState.RelicList)
        {
            if (relic == null || relic.ID != RelicID.SpiralAccelerator)
                continue;

            relic.Num = Math.Max(0, relic.Num) + 1;
            if (relic.Num >= SpiralAcceleratorTurnInterval)
            {
                ApplySpiralAcceleratorTeamTurnStart(battle, teamContext, refreshUi);
                relic.Num = 0;
            }
            relic.UpdateIconLabel();
        }
    }

    public static void ApplyCarryRelicEffects(Character carriedCharacter)
    {
        if (
            carriedCharacter is not PlayerCharacter
            || carriedCharacter.State == Character.CharacterState.Dying
            || !HasRelic(RelicID.MechanicalEnergyConverter)
        )
        {
            return;
        }

        using var _ = carriedCharacter.BeginEffectSource(
            GetRelicName(RelicID.MechanicalEnergyConverter)
        );
        ShowRelicTriggerPopup(carriedCharacter, RelicID.MechanicalEnergyConverter);
        carriedCharacter.BattleNode?.UpdataEnergy(
            carriedCharacter,
            MechanicalEnergyConverterEnergy,
            carriedCharacter
        );
    }

    public static int GetTurnStartEnergyGainBonus(Character character)
    {
        if (character is not PlayerCharacter)
            return 0;

        return HasRelic(RelicID.PhilosophersStone) ? PhilosophersStoneEnergyBonus : 0;
    }

    public static int GetTurnStartDrawBonus(Battle battle, Character visualTarget = null)
    {
        int bonus = 0;
        if (HasRelic(RelicID.EternalGlass))
        {
            ShowRelicTriggerPopup(visualTarget, RelicID.EternalGlass);
            bonus += EternalGlassDrawBonus;
        }

        if (HasRelic(RelicID.Backpack) && battle?.ElapsedTurnCount == 1)
        {
            ShowRelicTriggerPopup(visualTarget, RelicID.Backpack, 34f);
            bonus += BackpackFirstTurnDrawBonus;
        }

        return bonus;
    }

    public static void ApplySkillUsedRelicEffects(Skill skill)
    {
        if (
            skill?.OwnerCharater == null
            || !skill.OwnerCharater.IsPlayer
            || skill.OwnerCharater.State == Character.CharacterState.Dying
        )
        {
            return;
        }

        ApplyKnightHelmetSkillUsed(skill);
        ApplyIonizedVoiceSkillUsed(skill);
    }

    public static void ApplyBattleEndRelicEffects(Battle battle)
    {
        if (battle?.MapNode?.PlayerResourceState?.RelicList == null)
            return;

        foreach (var relic in battle.MapNode.PlayerResourceState.RelicList)
        {
            if (relic?.ID == RelicID.HealingBeacon)
                ApplyHealingBeaconBattleEndHeal(battle);
        }
    }

    private static void ApplyKnightHelmetSkillUsed(Skill skill)
    {
        if (skill.CardEnergyCost < KnightHelmetMinimumCost || !HasRelic(RelicID.KnightHelmet))
            return;

        using var _ = skill.OwnerCharater.BeginEffectSource(GetRelicName(RelicID.KnightHelmet));
        ShowRelicTriggerPopup(skill.OwnerCharater, RelicID.KnightHelmet);
        skill.OwnerCharater.UpdataBlock(KnightHelmetBlock, source: skill.OwnerCharater);
    }

    private static void ApplyIonizedVoiceSkillUsed(Skill skill)
    {
        if (skill.SkillType != Skill.SkillTypes.Special || !HasRelic(RelicID.IonizedVoice))
            return;

        Battle battle = skill.OwnerCharater?.BattleNode;
        Relic relic = battle
            ?.MapNode?.PlayerResourceState?.RelicList?.FirstOrDefault(r =>
                r != null && r.ID == RelicID.IonizedVoice
            );
        if (relic == null)
            return;

        relic.Num = Math.Max(0, relic.Num) + 1;
        if (relic.Num >= IonizedVoiceSpecialSkillInterval)
        {
            using var _ = skill.OwnerCharater.BeginEffectSource(GetRelicName(RelicID.IonizedVoice));
            ShowRelicTriggerPopup(skill.OwnerCharater, RelicID.IonizedVoice);
            battle.TryDrawPlayerTeamBattleCards(IonizedVoiceDrawCount);
            relic.Num = 0;
        }

        relic.UpdateIconLabel();
    }

    private static bool HasRelic(RelicID relicId) => GameInfo.HasRelic(relicId);

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
                {
                    ShowRelicTriggerPopup(battle.EnemiesList[i], ID);
                    list.Add(battle.EnemiesList[i].GetHurt(BlessingDamage));
                }
                await Task.WhenAll(list);
                Num--;
                break;
            case RelicID.BattleBanner:
            case RelicID.GuardBadge:
            case RelicID.VitalityCapsule:
                break;
            case RelicID.HealingBeacon:
                break;
            case RelicID.ArmorBreaker:
                ApplyDebuffToEnemies(battle, Buff.BuffName.Vulnerable, 1, ID);
                break;
            case RelicID.WeakeningEye:
                ApplyDebuffToEnemies(battle, Buff.BuffName.Weaken, 1, ID);
                break;
            case RelicID.CompressionCore:
            case RelicID.Toolbox:
                break;
            case RelicID.Backpack:
                break;
            case RelicID.EnergyTank:
                ApplyPlayerEnergy(battle, EnergyTankEnergy, ID);
                break;
            case RelicID.EnergyStorageTank:
                ApplyEnergyStorageToPlayers(battle, EnergyStorageTankStacks, ID);
                break;
            case RelicID.FusionCore:
            case RelicID.SpiralAccelerator:
            case RelicID.MechanicalEnergyConverter:
                break;
            case RelicID.Refractometer:
                ApplyRefractometerEffect(battle);
                break;
            case RelicID.Purifier:
                ApplyDebuffImmunityToPlayers(battle, PurifierDebuffImmunityStacks);
                break;
            case RelicID.KnightHelmet:
            case RelicID.IonizedVoice:
                break;
            case RelicID.PhilosophersStone:
                await ApplyPowerToEnemies(battle, PhilosophersStoneEnemyPower, ID);
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

        GameInfo.SetRelicCount(ID, Num);
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
            RelicID.BattleBanner => "战旗",
            RelicID.GuardBadge => "守护徽章",
            RelicID.VitalityCapsule => "活力胶囊",
            RelicID.HealingBeacon => "治疗信标",
            RelicID.ArmorBreaker => "裂甲棱镜",
            RelicID.WeakeningEye => "衰弱之眼",
            RelicID.CompressionCore => "压缩核心",
            RelicID.MembershipCard => "会员卡",
            RelicID.Toolbox => "工具箱",
            RelicID.MatrixShield => "矩阵护盾",
            RelicID.Backpack => "背包",
            RelicID.EnergyTank => "自动电池",
            RelicID.EnergyStorageTank => "能量储罐",
            RelicID.FusionCore => "聚变核心",
            RelicID.SpiralAccelerator => "螺旋加速器",
            RelicID.MechanicalEnergyConverter => "机械能转换器",
            RelicID.Refractometer => "折射仪",
            RelicID.Purifier => "净化器",
            RelicID.KnightHelmet => "骑士头盔",
            RelicID.IonizedVoice => "电离之声",
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
            RelicID.BattleBanner => $"拾起时全阵获得{BattleBannerPartyPower}点力量。",
            RelicID.GuardBadge => $"拾起时全阵获得{GuardBadgePartySurvivability}点生存。",
            RelicID.VitalityCapsule => $"拾起时全阵获得{VitalityCapsulePartyMaxLife}点生命上限。",
            RelicID.HealingBeacon => $"战斗结束时，为血量最低的角色回复{HealingBeaconBattleEndHeal}点生命。",
            RelicID.ArmorBreaker => "战斗开始时，敌方全阵获得1层易伤。",
            RelicID.WeakeningEye => "战斗开始时，敌方全阵获得1层虚弱。",
            RelicID.CompressionCore => "获得的电力币增加20%。",
            RelicID.MembershipCard => "商店中的所有商品价格降低50%。",
            RelicID.Toolbox => $"拾起时选择{ToolboxSelectionCount}张卡牌，为其添加保留。",
            RelicID.MatrixShield => $"第一次己方阵营回合开始时全阵获得{MatrixShieldBlock}点格挡。",
            RelicID.Backpack => $"第一次己方阵营回合开始时，额外抽{BackpackFirstTurnDrawBonus}张牌。",
            RelicID.EnergyTank => $"战斗开始时获得{EnergyTankEnergy}点能量。",
            RelicID.EnergyStorageTank => $"战斗开始时全阵获得{EnergyStorageTankStacks}层{Buff.BuffName.EnergyStorage.GetDescription()}。",
            RelicID.FusionCore => $"己方行动每{FusionCoreActionInterval}次行动当前角色获得{FusionCoreEnergy}点能量。",
            RelicID.SpiralAccelerator => $"每{SpiralAcceleratorTurnInterval}个己方阵营回合开始时，抽{SpiralAcceleratorDrawCount}张牌。",
            RelicID.MechanicalEnergyConverter => $"角色被连携时获得{MechanicalEnergyConverterEnergy}点能量。",
            RelicID.Refractometer => $"战斗开始时随机一名角色获得{RefractometerDamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。",
            RelicID.Purifier => $"战斗开始时全阵获得{PurifierDebuffImmunityStacks}层{Buff.BuffName.DebuffImmunity.GetDescription()}。",
            RelicID.KnightHelmet => $"每当角色打出{KnightHelmetMinimumCost}费及以上的卡牌时，获得{KnightHelmetBlock}点格挡。",
            RelicID.IonizedVoice => $"每使用{IonizedVoiceSpecialSkillInterval}张特殊卡牌，抽{IonizedVoiceDrawCount}张牌。",
            RelicID.PhilosophersStone => $"回合开始时获得的能量+{PhilosophersStoneEnergyBonus}。战斗开始时所有敌人获得{PhilosophersStoneEnemyPower}点力量。",
            RelicID.EternalGlass => $"回合开始时抽牌数+{EternalGlassDrawBonus}。",
            RelicID.KingsSword => $"拾起时全体角色增加{KingsSwordPartyPower}点力量。",
            RelicID.PulseController => $"战斗开始时随机一名敌人获得{PulseControllerStunStacks}层{Buff.BuffName.Stun.GetDescription()}。",
            _ => "暂无效果。",
        };
    }

    private static async Task RunToolboxSelectionAsync(PlayerResourceState playerResourceState)
    {
        try
        {
            if (playerResourceState == null || !GodotObject.IsInstanceValid(playerResourceState))
                return;

            EventCardSelectOverlay overlay = GetOrCreateToolboxCardSelectOverlay(playerResourceState);
            if (overlay == null)
                return;

            var entries = GameInfo.BuildSelectableDeckCardEntries();
            if (entries.Count == 0)
                return;

            string hint = "请选择要添加保留的卡牌";
            IReadOnlyList<EventCardSelection> selections = await overlay.SelectManyAsync(
                entries,
                hint,
                ToolboxSelectionCount
            );
            if (selections == null || selections.Count == 0)
                return;

            overlay.HideSelection();

            foreach (EventCardSelection selection in selections)
                GameInfo.AddToolboxRetainCard(selection.PlayerIndex, selection.SkillId);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"工具箱选牌失败：{ex.Message}");
        }
    }

    private const int ToolboxCardSelectLayer = 10;

    private static EventCardSelectOverlay GetOrCreateToolboxCardSelectOverlay(
        PlayerResourceState playerResourceState
    )
    {
        var tree = playerResourceState?.GetTree();
        Node map =
            tree?.Root?.GetNodeOrNull<Node>("Map")
            ?? tree?.Root?.GetNodeOrNull<Node>("/root/Map");
        if (map == null)
            return null;

        var overlayLayer = map.GetNodeOrNull<CanvasLayer>("ToolboxCardSelectLayer");
        if (overlayLayer == null)
        {
            overlayLayer = new CanvasLayer
            {
                Name = "ToolboxCardSelectLayer",
                Layer = ToolboxCardSelectLayer,
            };
            map.AddChild(overlayLayer);
        }

        var overlay = overlayLayer.GetNodeOrNull<EventCardSelectOverlay>("ToolboxCardSelectOverlay");
        if (overlay != null && GodotObject.IsInstanceValid(overlay))
            return overlay;

        overlay = new EventCardSelectOverlay { Name = "ToolboxCardSelectOverlay" };
        overlayLayer.AddChild(overlay);
        return overlay;
    }

    private static async Task ApplyEffectToFrontPlayers(
        Battle battle,
        PropertyType propertyType,
        int amount,
        RelicID relicID
    )
    {
        int targetCount = Math.Min(2, battle.PlayersList.Count);
        for (int i = 0; i < targetCount; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            ShowRelicTriggerPopup(player, relicID);
            await player.IncreaseProperties(propertyType, amount);
        }
    }

    private static void ApplyHealingBeaconBattleEndHeal(Battle battle)
    {
        var target = battle
            ?.PlayersList
            ?.Where(player => player != null && !player.IsSummon)
            .OrderBy(player => player.Life)
            .ThenBy(player => player.PositionIndex)
            .FirstOrDefault();
        if (target == null)
            return;

        using var _ = target.BeginEffectSource(GetRelicName(RelicID.HealingBeacon));
        ShowRelicTriggerPopup(target, RelicID.HealingBeacon);
        target.Recover(HealingBeaconBattleEndHeal, rebirth: true, source: target);
    }

    private static void ApplyDebuffToEnemies(
        Battle battle,
        Buff.BuffName buffName,
        int stacks,
        RelicID relicID
    )
    {
        if (battle?.EnemiesList == null)
            return;

        for (int i = 0; i < battle.EnemiesList.Count; i++)
        {
            var enemy = battle.EnemiesList[i];
            if (enemy == null || enemy.State == Character.CharacterState.Dying)
                continue;

            ShowRelicTriggerPopup(enemy, relicID);
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

    private static void ApplyBlockToPlayers(Battle battle, int block, RelicID relicID)
    {
        if (battle?.PlayersList == null || block <= 0)
            return;

        for (int i = 0; i < battle.PlayersList.Count; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            ShowRelicTriggerPopup(player, relicID);
            player.UpdataBlock(block);
        }
    }

    private static void ApplyEnergyToFrontPlayers(Battle battle, int energy, RelicID relicID)
    {
        if (battle?.PlayersList == null || energy == 0)
            return;

        int targetCount = Math.Min(2, battle.PlayersList.Count);
        for (int i = 0; i < targetCount; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            ShowRelicTriggerPopup(player, relicID);
            battle.UpdataEnergy(player, energy, player);
        }
    }

    private static void ApplyPlayerEnergy(Battle battle, int energy, RelicID relicID)
    {
        if (battle?.PlayersList == null || energy == 0)
            return;

        Character target = battle.PlayersList.FirstOrDefault(player =>
            player != null && player.State != Character.CharacterState.Dying
        );
        if (target == null)
            return;

        ShowRelicTriggerPopup(target, relicID);
        battle.UpdataEnergy(target, energy, target);
    }

    private static void ApplyEnergyStorageToPlayers(Battle battle, int stacks, RelicID relicID)
    {
        if (battle?.PlayersList == null || stacks <= 0)
            return;

        for (int i = 0; i < battle.PlayersList.Count; i++)
        {
            var player = battle.PlayersList[i];
            if (player == null || player.State == Character.CharacterState.Dying)
                continue;

            ShowRelicTriggerPopup(player, relicID);
            SpecialBuff.BuffAdd(Buff.BuffName.EnergyStorage, player, stacks, player);
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

        int seed =
            battle.CurrentLevelNode != null
                ? GameInfo.CreateRunRngSeed(
                    GameInfo.GetStreamForNodeType(battle.CurrentLevelNode.Type),
                    GameInfo.GetNodeContentQueueIndex(battle.CurrentLevelNode),
                    unchecked((int)0x4EF1AC70)
                )
                : GameInfo.Seed ^ unchecked((int)0x4EF1AC70);
        var rng = new Random(seed);
        Character target = candidates[rng.Next(candidates.Length)];
        ShowRelicTriggerPopup(target, RelicID.Refractometer);
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

            ShowRelicTriggerPopup(player, RelicID.Purifier);
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

        int seed =
            battle.CurrentLevelNode != null
                ? GameInfo.CreateRunRngSeed(
                    GameInfo.GetStreamForNodeType(battle.CurrentLevelNode.Type),
                    GameInfo.GetNodeContentQueueIndex(battle.CurrentLevelNode),
                    unchecked((int)0x51A7C011)
                )
                : GameInfo.Seed ^ unchecked((int)0x51A7C011);
        var rng = new Random(seed);
        Character target = candidates[rng.Next(candidates.Length)];
        ShowRelicTriggerPopup(target, RelicID.PulseController);
        SkillBuff.BuffAdd(Buff.BuffName.Stun, target, PulseControllerStunStacks, target);
    }

    private static async Task ApplyPowerToEnemies(Battle battle, int power, RelicID relicID)
    {
        if (battle?.EnemiesList == null || power == 0)
            return;

        foreach (var enemy in battle.EnemiesList.ToArray())
        {
            if (enemy == null || enemy.State == Character.CharacterState.Dying)
                continue;

            ShowRelicTriggerPopup(enemy, relicID);
            await enemy.IncreaseProperties(PropertyType.Power, power, enemy);
        }
    }

    private static void ApplyAcquireEffect(RelicID relicID)
    {
        switch (relicID)
        {
            case RelicID.BattleBanner:
                ApplyPartyAcquireStat(power: BattleBannerPartyPower);
                break;
            case RelicID.GuardBadge:
                ApplyPartyAcquireStat(survivability: GuardBadgePartySurvivability);
                break;
            case RelicID.VitalityCapsule:
                ApplyPartyAcquireStat(maxLife: VitalityCapsulePartyMaxLife);
                break;
            case RelicID.KingsSword:
                ApplyPartyAcquireStat(power: KingsSwordPartyPower);
                break;
        }
    }

    private static void ApplyPartyAcquireStat(int power = 0, int survivability = 0, int maxLife = 0)
    {
        if (GameInfo.PlayerCharacters == null)
            return;

        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var info = GameInfo.PlayerCharacters[i];
            info.Power += power;
            info.Survivability += survivability;
            info.LifeMax = Math.Max(1, info.LifeMax + maxLife);
            if (info.LifeInitialized)
                info.Life = Math.Clamp(info.Life, 0, info.LifeMax);
            GameInfo.PlayerCharacters[i] = info;
        }
    }

    private static void ApplyFusionCoreActionStart(Character actingCharacter)
    {
        if (actingCharacter == null)
            return;

        using var _ = actingCharacter.BeginEffectSource(GetRelicName(RelicID.FusionCore));
        ShowRelicTriggerPopup(actingCharacter, RelicID.FusionCore);
        actingCharacter.BattleNode?.UpdataEnergy(
            actingCharacter,
            FusionCoreEnergy,
            actingCharacter
        );
    }

    private static void ApplySpiralAcceleratorTeamTurnStart(
        Battle battle,
        Character teamContext,
        bool refreshUi
    )
    {
        if (battle == null || teamContext == null)
            return;

        using var _ = teamContext.BeginEffectSource(GetRelicName(RelicID.SpiralAccelerator));
        ShowRelicTriggerPopup(teamContext, RelicID.SpiralAccelerator);
        battle.TryDrawPlayerTeamBattleCards(SpiralAcceleratorDrawCount, refreshUi);
    }

    private static void ShowRelicTriggerPopup(
        Character target,
        RelicID relicID,
        float horizontalOffset = 0f
    )
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || IconScene == null)
            return;

        var popup = new Node2D
        {
            Name = $"RelicTrigger_{relicID}",
            ZIndex = 1000,
            ZAsRelative = false,
            Modulate = new Color(1f, 1f, 1f, 0f),
            Scale = new Vector2(0.55f, 0.55f),
        };

        var icon = IconScene.Instantiate<Control>();
        ConfigureTriggerPopupIcon(icon, relicID);
        popup.AddChild(icon);
        target.AddChild(popup);

        Vector2 start = target.GlobalPosition + new Vector2(horizontalOffset, TriggerPopupHeadOffset);
        popup.GlobalPosition = start;

        Tween tween = popup.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(popup, "modulate:a", 1f, 0.1f).SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(popup, "scale", Vector2.One, 0.14f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween
            .TweenProperty(popup, "global_position", start + new Vector2(0f, -24f), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        tween
            .TweenProperty(popup, "global_position", start + new Vector2(0f, TriggerPopupRise), 0.55f)
            .SetDelay(0.2f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(popup, "modulate:a", 0f, 0.28f).SetDelay(0.47f);
        tween.TweenProperty(popup, "scale", new Vector2(0.82f, 0.82f), 0.28f).SetDelay(0.47f);
        tween.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(popup))
                popup.QueueFree();
        };
    }

    private static void ConfigureTriggerPopupIcon(Control icon, RelicID relicID)
    {
        if (icon == null)
            return;

        Vector2 size = new(TriggerPopupIconSize, TriggerPopupIconSize);
        icon.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        icon.Position = -size * 0.5f;
        icon.Size = size;
        icon.CustomMinimumSize = size;
        icon.PivotOffset = size * 0.5f;
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        ApplyIconVisual(icon, relicID);

        icon.GetNodeOrNull<Label>("Label")?.Hide();
        if (icon.GetNodeOrNull<Panel>("Panel") is Panel panel)
        {
            panel.MouseFilter = Control.MouseFilterEnum.Ignore;
            panel.Position = Vector2.Zero;
            panel.Size = size;
            panel.CustomMinimumSize = size;
        }
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
            tip.MinContentWidth = 260f;
            tip.SetText(BuildTooltip());
        };
        icon.MouseExited += () =>
        {
            if (tip != null)
                tip.HideTooltip();
        };
    }

    private void UpdateRelicTipText()
    {
        if (IconNode == null)
            return;
        var tip = GetOrCreateRelicTip(IconNode);
        if (tip == null || !tip.Visible)
            return;
        tip.MinContentWidth = 260f;
        tip.SetText(BuildTooltip());
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
        tip.MinContentWidth = 260f;
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
    BattleBanner,
    GuardBadge,
    VitalityCapsule,
    HealingBeacon,
    ArmorBreaker,
    WeakeningEye,
    CompressionCore,
    MembershipCard,
    Toolbox,
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
    MechanicalEnergyConverter,
    EnergyStorageTank,
    SpiralAccelerator,
    IonizedVoice,
}
