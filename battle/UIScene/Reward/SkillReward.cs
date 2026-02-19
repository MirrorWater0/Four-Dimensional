using Godot;
using System;

public partial class SkillReward : Panel
{
    public RichTextLabel Description => field ??= GetNode<RichTextLabel>("Description");
    public Label NameLabel => field ??= GetNode<Label>("NameLabel");
    public Button Button => field ??= GetNode<Button>("Button");
    public SkillButton SkillTypeIcon => field ??= GetNode<SkillButton>("SkillIcon");
    public Panel HoverHint => field ??= GetNode<Panel>("HoverHint");
}
