using Godot;

public partial class CharacterControl
{
    private const int PileOverlayCardPoolPrewarmCount = 15;

    private bool _pileCardHolderPoolPrewarmed;

    private void PrewarmPileOverlayCardPool()
    {
        if (_pileCardHolderPoolPrewarmed)
            return;

        while (_pileCardHolderPool.Count < PileOverlayCardPoolPrewarmCount)
        {
            var holder = GD.Load<PackedScene>("res://battle/UIScene/PileCardHolder.tscn").Instantiate<Control>();
            _pileCardHolderPool.Push(holder);
        }

        _pileCardHolderPoolPrewarmed = true;
    }
}
