using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public static class BattleStartResourcePreloader
{
    private const string BattleScenePath = "res://battle/Battle.tscn";
    // Each prewarm item can trigger resource creation or a texture upload. Keep those
    // operations isolated so they never turn a loading indicator into a frozen frame.
    private const int YieldEvery = 1;
    private static readonly string[] SharedEffectTexturePaths =
    [
        "res://asset/Effect/explode.png",
        "res://asset/Effect/Increase.png",
        "res://asset/Effect/Shield.png",
        "res://asset/Effect/lightingDe.png",
        "res://asset/Effect/Energy.png",
        "res://asset/Effect/Recovery.png",
        "res://asset/Effect/Transition.png",
        "res://asset/particle/particle1.png",
        "res://asset/particle/particle2.png",
    ];
    private static readonly HashSet<string> PrewarmedKeys = new(StringComparer.Ordinal);
    private static bool _running;
    private static bool _sharedResourcesPrewarmed;

    public static Task PrewarmStartupResourcesAsync(Node owner)
    {
        return PrewarmSharedBattleResourcesAsync(owner);
    }

    public static async Task PrewarmForMapAsync(Node owner, LevelProgress levelProgress)
    {
        if (_running || owner == null || levelProgress == null)
            return;

        _running = true;
        try
        {
            await PrewarmSharedBattleResourcesAsync(owner);
            await PrewarmPlayerResourcesAsync(owner);

            var nodes = levelProgress
                .GetChildren()
                .OfType<Control>()
                .SelectMany(child => child.GetChildren().OfType<LevelNode>())
                .Where(IsBattleNode)
                .Where(node => node.State == LevelNode.LevelState.Unlocked)
                .Take(3)
                .ToArray();

            foreach (var node in nodes)
                await PrewarmBattleNodeAsync(owner, node, allocateEncounter: false);
        }
        finally
        {
            _running = false;
        }
    }

    public static async Task PrewarmBattleNodeAsync(
        Node owner,
        LevelNode node,
        bool allocateEncounter = false
    )
    {
        if (owner == null || node == null || !IsBattleNode(node))
            return;

        bool hasCurrentEncounter = node.EnemiesRegeditList?.Count > 0;
        string key =
            $"{node.SelfCoordinate.X}:{node.SelfCoordinate.Y}:{node.Type}:{allocateEncounter}:{hasCurrentEncounter}";
        if (!PrewarmedKeys.Add(key))
            return;

        await PrewarmSharedBattleResourcesAsync(owner);
        await PrewarmPlayerResourcesAsync(owner);

        var enemies = node.EnemiesRegeditList;
        if (enemies == null || enemies.Count == 0)
            enemies = node.ProduceEnemies(allocateEncounter);

        int processed = 0;
        foreach (var regedit in enemies)
        {
            if (regedit == null)
                continue;

            _ = regedit.CharacterScene;
            PreloadeScene.GetTexture(regedit.PortaitPath);
            processed++;
            if (processed % YieldEvery == 0 && !await YieldFrame(owner))
                return;
        }
    }

    private static async Task PrewarmSharedBattleResourcesAsync(Node owner)
    {
        if (_sharedResourcesPrewarmed)
            return;

        PreloadeScene.GetPackedScene(BattleScenePath);
        if (!await YieldFrame(owner))
            return;

        Texture2D[] effectTextures = await PrewarmSharedEffectTexturesAsync(owner);
        if (!await YieldFrame(owner))
            return;

        SkillCard.PrewarmExhaustEffect();
        if (!await YieldFrame(owner))
            return;

        CharacterEffect.Prewarm(owner, 4);
        if (!await YieldFrame(owner))
            return;

        AttackEffect.Prewarm(2);
        if (!await YieldFrame(owner))
            return;

        HitParticle.Prewarm(6);
        if (!await YieldFrame(owner))
            return;

        BuffGainParticle.Prewarm(4);
        if (!await YieldFrame(owner))
            return;

        BuffTriggerFlashVfx.Prewarm(3);
        if (!await YieldFrame(owner))
            return;

        await PrewarmTextureUploadsAsync(owner, effectTextures);
        if (!await YieldFrame(owner))
            return;

        _sharedResourcesPrewarmed = true;
    }

    private static async Task<Texture2D[]> PrewarmSharedEffectTexturesAsync(Node owner)
    {
        var textures = new List<Texture2D>(SharedEffectTexturePaths.Length);
        for (int i = 0; i < SharedEffectTexturePaths.Length; i++)
        {
            Texture2D texture = PreloadeScene.GetTexture(SharedEffectTexturePaths[i]);
            if (texture != null)
                textures.Add(texture);

            if (!await YieldFrame(owner))
                return textures.ToArray();
        }

        return textures.ToArray();
    }

    private static async Task PrewarmTextureUploadsAsync(Node owner, IReadOnlyList<Texture2D> textures)
    {
        if (
            owner == null
            || !GodotObject.IsInstanceValid(owner)
            || !owner.IsInsideTree()
            || textures == null
            || textures.Count == 0
        )
        {
            return;
        }

        var root = new Node2D
        {
            Name = "BattleEffectTextureWarmup",
            ZIndex = -4096,
            Modulate = new Color(1f, 1f, 1f, 0.001f),
            Scale = Vector2.One * 0.001f,
        };
        owner.AddChild(root);

        for (int i = 0; i < textures.Count; i++)
        {
            var sprite = new Sprite2D
            {
                Texture = textures[i],
                Centered = false,
                Position = Vector2.Zero,
            };
            root.AddChild(sprite);

            if ((i + 1) % YieldEvery == 0 && !await YieldFrame(owner))
            {
                if (GodotObject.IsInstanceValid(root))
                    root.QueueFree();
                return;
            }
        }

        await YieldFrame(owner);

        if (GodotObject.IsInstanceValid(root))
            root.QueueFree();
    }

    private static async Task PrewarmPlayerResourcesAsync(Node owner)
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || players.Length == 0)
            return;

        int processed = 0;
        for (int i = 0; i < players.Length; i++)
        {
            PlayerInfoStructure character = players[i];
            PreloadeScene.GetPackedScene(character.CharacterScenePath);
            PreloadeScene.GetTexture(character.PortaitPath);

            string characterKey = ExtractCharacterKeyFromScenePath(character.CharacterScenePath);
            foreach (SkillID skillId in EnumeratePrewarmSkillIds(character))
            {
                Skill skill = Skill.GetSkill(skillId);
                if (skill == null || skill.SkillType == Skill.SkillTypes.none || skill.IsStatusCard)
                    continue;

                skill.SetPreviewStats(
                    TalentTree.GetEffectivePower(character),
                    TalentTree.GetEffectiveSurvivability(character),
                    1,
                    playerIndex: i
                );
                SkillCard.PrewarmSkillResources(skill, character.CharacterName, characterKey);

                processed++;
                if (processed % YieldEvery == 0 && !await YieldFrame(owner))
                    return;
            }

            processed++;
            if (processed % YieldEvery == 0 && !await YieldFrame(owner))
                return;
        }
    }

    private static IEnumerable<SkillID> EnumeratePrewarmSkillIds(PlayerInfoStructure character)
    {
        if (character.TakenSkills != null)
        {
            foreach (SkillID skillId in character.TakenSkills)
                yield return skillId;
        }

        if (character.GainedSkills != null)
        {
            foreach (SkillID skillId in character.GainedSkills.Distinct())
                yield return skillId;
        }
    }

    private static bool IsBattleNode(LevelNode node)
    {
        return node.Type is LevelNode.LevelType.Normal
            or LevelNode.LevelType.Elite
            or LevelNode.LevelType.Boss;
    }

    private static string ExtractCharacterKeyFromScenePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var parts = path.Replace('\\', '/').Split('/');
        return parts.Length >= 2 ? parts[^2] : null;
    }

    private static async Task<bool> YieldFrame(Node owner)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree())
            return false;

        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
        return owner != null && GodotObject.IsInstanceValid(owner) && owner.IsInsideTree();
    }
}
