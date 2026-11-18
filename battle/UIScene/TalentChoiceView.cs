using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class TalentChoiceView {
    private static readonly Font TalentFont = GD.Load<Font>("res://battle/UIScene/BattleUiFont.tres");
    public static void Populate(Control root, PlayerInfoStructure info,
        IReadOnlyList<TalentNodeDefinition> choices, Action<string> choose) {
        if (root == null || !GodotObject.IsInstanceValid(root))
            return;
        foreach (Node child in root.GetChildren()) {
            root.RemoveChild(child);
            child.QueueFree();
        }
        float width = Math.Max(620, root.Size.X);
        float columnWidth = (width - 20) / 2;
        var title = new Label {
            Text = choices.Count > 0 ? choices.Count == 1
                ? I18n.Tr("ui.talent.last_choice", "仅剩一个未获得的天赋")
                : I18n.Tr("ui.talent.choose", "选择一个天赋")
                : I18n.Tr("ui.talent.no_choice", "暂无待选天赋"),
            Size = new Vector2(width, 30), MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeFontOverride("font", TalentFont);
        root.AddChild(title);
        for (int i = 0; i < choices.Count; i++) {
            var talent = choices[i];
            var button = new Button {
                Position = new Vector2(i * (columnWidth + 20), 40),
                Size = new Vector2(columnWidth, 160), FocusMode = Control.FocusModeEnum.None,
            };
            var style = new StyleBoxFlat {
                BgColor = new Color(0.07f, 0.09f, 0.12f, 0.85f),
                BorderColor = new Color(0.62f, 0.72f, 0.84f, 0.75f),
                BorderWidthLeft = 2, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            };
            button.AddThemeStyleboxOverride("normal", style);
            var hover = (StyleBoxFlat)style.Duplicate();
            hover.BgColor = new Color(0.14f, 0.18f, 0.23f, 0.95f);
            button.AddThemeStyleboxOverride("hover", hover);
            button.Pressed += () => choose(talent.Id);
            button.SetMeta("talent_id", talent.Id);
            root.AddChild(button);
            var label = new RichTextLabel {
                Position = new Vector2(18, 16), Size = new Vector2(columnWidth - 36, 128),
                BbcodeEnabled = true, ScrollActive = false, MouseFilter = Control.MouseFilterEnum.Ignore,
                Text = $"[b]{talent.DisplayName}[/b]\n\n{talent.EffectDescription}",
            };
            label.AddThemeFontSizeOverride("normal_font_size", 18);
            label.AddThemeFontSizeOverride("bold_font_size", 20);
            label.AddThemeFontOverride("normal_font", TalentFont);
            label.AddThemeFontOverride("bold_font", TalentFont);
            button.AddChild(label);
        }
        var owned = TalentTree.GetNodes(info).Where(node => TalentTree.HasUnlocked(info, node.Id)).ToArray();
        var ownedLabel = new RichTextLabel {
            Position = new Vector2(0, 215), Size = new Vector2(width, Math.Max(95, root.Size.Y - 215)),
            BbcodeEnabled = true, ScrollActive = true, MouseFilter = Control.MouseFilterEnum.Stop,
            Text = $"[b]{I18n.Tr("ui.talent.owned", "已获得的天赋")}[/b]\n"
                + (owned.Length == 0 ? I18n.Tr("ui.talent.none", "暂无")
                    : string.Join("\n", owned.Select(node => $"{node.DisplayName}：{node.EffectDescription}"))),
        };
        ownedLabel.AddThemeFontSizeOverride("normal_font_size", 15);
        ownedLabel.AddThemeFontSizeOverride("bold_font_size", 16);
        ownedLabel.AddThemeFontOverride("normal_font", TalentFont);
        ownedLabel.AddThemeFontOverride("bold_font", TalentFont);
        root.AddChild(ownedLabel);
    }
}
