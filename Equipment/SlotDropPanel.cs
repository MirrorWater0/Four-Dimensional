using Godot;

public partial class SlotDropPanel : PanelContainer
{
    [Signal]
    public delegate void InventoryCardDroppedEventHandler(int slotIndex, NodePath sourceCardPath);

    [Export]
    public int SlotIndex = -1;

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
            return false;

        var dict = (Godot.Collections.Dictionary)data;
        if (!dict.ContainsKey("source_path"))
            return false;

        Variant sourcePath = dict["source_path"];
        return sourcePath.VariantType == Variant.Type.NodePath;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (SlotIndex < 0 || data.VariantType != Variant.Type.Dictionary)
            return;

        var dict = (Godot.Collections.Dictionary)data;
        if (!dict.ContainsKey("source_path"))
            return;

        Variant sourcePath = dict["source_path"];
        if (sourcePath.VariantType != Variant.Type.NodePath)
            return;

        EmitSignal(SignalName.InventoryCardDropped, SlotIndex, sourcePath.AsNodePath());
    }
}
