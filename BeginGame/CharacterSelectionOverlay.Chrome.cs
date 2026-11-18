using Godot;

public partial class CharacterSelectionOverlay {
    private void RevealRoster() {
        for (int i = 0; i < _options.Count; i++)
            RevealItem(_options[i].CardButton, 0.06f + i * 0.055f);
    }

    // Animate opacity on the container-managed row, never its layout position.
    private static void RevealItem(Control item, float delay) {
        item.Modulate = Colors.White with { A = 0.12f };
        var tween = item.CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(item, "modulate:a", 1f, 0.28f).SetDelay(delay);
    }

    private void AnimateDossier(bool animate) {
        var material = (ShaderMaterial)HeroImage.Material;
        DossierText.Modulate = Colors.White with { A = animate ? 0.38f : 1f };
        material.SetShaderParameter("reveal", animate ? 0f : 1f);
        if (!animate)
            return;

        _previewTween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _previewTween.TweenProperty(material, "shader_parameter/reveal", 1f, 0.44f);
        _previewTween.TweenProperty(DossierText, "modulate:a", 1f, 0.24f);
    }
}
