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
            var holder = new Control();
            holder.AddChild(SkillCardScene.Instantiate<SkillCard>());
            _pileCardHolderPool.Push(holder);
        }

        _pileCardHolderPoolPrewarmed = true;
    }
}
