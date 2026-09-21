using Godot;

/// <summary>
/// Design tokens and widget factories for the pre-battle tactical screen.
/// Values follow the start screen's neutral industrial language: charcoal surfaces, cool-grey
/// rules, square corners, and high-contrast white type.
/// </summary>
public static class BattleReadyStyle
{
    public const string LatinFontPath = "res://asset/font/CormorantSC-Bold.ttf";
    public const string PanelShaderPath = "res://shader/UI/HSRPanel.gdshader";

    // --- Palette ---------------------------------------------------------
    public static readonly Color Gold = new(0.78f, 0.79f, 0.82f);
    public static readonly Color GoldBright = new(0.94f, 0.95f, 0.97f);
    public static readonly Color GoldDeep = new(0.53f, 0.55f, 0.59f);
    public static readonly Color Steel = new(0.68f, 0.70f, 0.74f);
    public static readonly Color Cyan = new(0.82f, 0.84f, 0.88f);
    public static readonly Color InkOnGold = new(0.07f, 0.075f, 0.085f);
    public static readonly Color TextPrimary = new(0.92f, 0.93f, 0.95f);
    public static readonly Color PanelTop = new(0.065f, 0.07f, 0.08f, 0.88f);
    public static readonly Color PanelBottom = new(0.028f, 0.03f, 0.036f, 0.88f);
    public static readonly Color SurfaceDim = new(0.075f, 0.08f, 0.09f, 0.62f);
    public static readonly Color SurfaceHover = new(0.13f, 0.135f, 0.15f, 0.74f);
    public static readonly Color Hairline = new(0.76f, 0.77f, 0.80f, 0.22f);
    public static readonly Color Bracket = new(0.90f, 0.91f, 0.94f, 0.18f);

    // Semantic stat colors, kept identical to the existing preview panel so the
    // restyle does not silently change what a number means.
    public static readonly Color StatLife = new(1f, 0.48f, 0.52f);
    public static readonly Color StatPower = new(1f, 0.78f, 0.38f);
    public static readonly Color StatSurvive = new(0.54f, 1f, 0.99f);

    private static FontFile _latinFont;
    private static FontVariation _trackedFont;
    private static Shader _panelShader;

    public static FontFile LatinFont => _latinFont ??= GD.Load<FontFile>(LatinFontPath);

    /// <summary>Wide-tracked latin face used for every decorative micro-label.</summary>
    public static FontVariation TrackedFont =>
        _trackedFont ??= new FontVariation { BaseFont = LatinFont, SpacingGlyph = 5 };

    public static Shader PanelShader => _panelShader ??= GD.Load<Shader>(PanelShaderPath);

    // --- Factories -------------------------------------------------------

    /// <summary>A dim, wide-tracked latin caption — the signature decorative label.</summary>
    public static Label MicroLabel(string text, int fontSize = 16, float alpha = 0.55f)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", TrackedFont);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", Steel with { A = alpha });
        return label;
    }

    public static Label TextLabel(
        string text,
        int fontSize,
        Color color,
        HorizontalAlignment align = HorizontalAlignment.Left
    )
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = align,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    /// <summary>Title set in the latin display face (also used for CJK headings).</summary>
    public static Label DisplayLabel(string text, int fontSize, Color color)
    {
        var label = TextLabel(text, fontSize, color);
        label.AddThemeFontOverride("font", LatinFont);
        return label;
    }

    /// <summary>1px steel rule.</summary>
    public static ColorRect Hairline1(float alpha = 0.18f)
    {
        return new ColorRect
        {
            CustomMinimumSize = new Vector2(0f, 1f),
            Color = Steel with { A = alpha },
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    /// <summary>Neutral bar + 45-degree diamond + thin steel line: the section rule.</summary>
    public static HBoxContainer TitleRule(float goldWidth = 180f)
    {
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 10);

        row.AddChild(
            new ColorRect
            {
                CustomMinimumSize = new Vector2(goldWidth, 4f),
                Color = Gold with { A = 0.95f },
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            }
        );
        row.AddChild(
            new ColorRect
            {
                CustomMinimumSize = new Vector2(9f, 9f),
                Color = Gold with { A = 0.95f },
                Rotation = Mathf.Pi * 0.25f,
                PivotOffset = new Vector2(4.5f, 4.5f),
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            }
        );
        row.AddChild(
            new ColorRect
            {
                CustomMinimumSize = new Vector2(0f, 1f),
                Color = Steel with { A = 0.25f },
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            }
        );
        return row;
    }

    /// <summary>
    /// Cut-corner console panel backed by HSRPanel.gdshader. The shader needs the pixel size of
    /// the rect, so the returned node keeps <c>rect_size</c> in sync with its own resizing.
    /// </summary>
    public static ColorRect CutPanel(
        Vector2 size,
        float cut = 24f,
        float gridAlpha = 0.035f,
        float sweepAlpha = 0.05f
    )
    {
        ColorRect rect = MakeCutRect(cut, gridAlpha, sweepAlpha);
        rect.CustomMinimumSize = size;
        rect.Size = size;
        ((ShaderMaterial)rect.Material).SetShaderParameter("rect_size", size);
        return rect;
    }

    /// <summary>
    /// Drops a cut-corner backdrop behind an existing control and keeps it sized to the host.
    /// Used to re-skin panels that were authored in the scene file.
    /// </summary>
    public static ColorRect AttachCutBackdrop(Control host, float cut = 22f)
    {
        ColorRect rect = MakeCutRect(cut, 0.03f, 0.045f);
        rect.AnchorRight = 1f;
        rect.AnchorBottom = 1f;
        rect.GrowHorizontal = Control.GrowDirection.Both;
        rect.GrowVertical = Control.GrowDirection.Both;

        host.AddChild(rect);
        host.MoveChild(rect, 0);

        var material = (ShaderMaterial)rect.Material;
        material.SetShaderParameter("rect_size", host.Size);
        host.Resized += () => material.SetShaderParameter("rect_size", host.Size);
        return rect;
    }

    private static ColorRect MakeCutRect(float cut, float gridAlpha, float sweepAlpha)
    {
        var material = new ShaderMaterial { Shader = PanelShader };
        material.SetShaderParameter("bg_top", PanelTop);
        material.SetShaderParameter("bg_bottom", PanelBottom);
        material.SetShaderParameter("border_color", Steel with { A = 0.2f });
        material.SetShaderParameter("cut_color", GoldBright with { A = 0.9f });
        material.SetShaderParameter("cut_size", cut);
        material.SetShaderParameter("border_width", 1.0f);
        material.SetShaderParameter("grid_alpha", gridAlpha);
        material.SetShaderParameter("sweep_alpha", sweepAlpha);

        return new ColorRect
        {
            Color = new Color(1f, 1f, 1f, 0f),
            Material = material,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    /// <summary>Flat angular surface — no corner radius anywhere in this language.</summary>
    public static StyleBoxFlat AngularBox(
        Color bg,
        Color border,
        int borderWidth = 1,
        int leftAccent = 0,
        Color? accentColor = null
    )
    {
        var box = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusBottomLeft = 0,
        };

        if (leftAccent > 0)
        {
            box.BorderWidthLeft = leftAccent;
            // A single-colour StyleBoxFlat cannot tint one edge, so the accent colour wins
            // and the remaining hairlines are folded into it at a lower alpha by the caller.
            box.BorderColor = accentColor ?? border;
        }

        return box;
    }

    /// <summary>Hatched capacity strip used as a lightweight meter.</summary>
    public static Control HatchBar(float width, float height, float fill, Color color)
    {
        var root = new Control
        {
            CustomMinimumSize = new Vector2(width, height),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = true,
        };

        root.AddChild(
            new ColorRect
            {
                Color = Steel with { A = 0.12f },
                OffsetRight = width,
                OffsetBottom = height,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            }
        );

        float filledWidth = Mathf.Clamp(fill, 0f, 1f) * width;
        int tick = 0;
        for (float x = 0f; x < filledWidth; x += 6f, tick++)
        {
            root.AddChild(
                new ColorRect
                {
                    Color = color with { A = tick % 2 == 0 ? 0.85f : 0.45f },
                    OffsetLeft = x,
                    OffsetRight = Mathf.Min(x + 4f, filledWidth),
                    OffsetBottom = height,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                }
            );
        }

        return root;
    }

    /// <summary>Small filled square used as a status LED before a readout.</summary>
    public static ColorRect StatusDot(Color color, float size = 8f)
    {
        return new ColorRect
        {
            CustomMinimumSize = new Vector2(size, size),
            Color = color,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    /// <summary>Eight ColorRects forming L-shaped marks at the four screen corners.</summary>
    public static Control CornerBrackets(
        Vector2 screenSize,
        float inset = 40f,
        float arm = 36f,
        float weight = 3f
    )
    {
        var root = new Control
        {
            Size = screenSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        void Mark(float ax, float ay, float ox, float oy, float w, float h)
        {
            root.AddChild(
                new ColorRect
                {
                    AnchorLeft = ax,
                    AnchorRight = ax,
                    AnchorTop = ay,
                    AnchorBottom = ay,
                    OffsetLeft = ox,
                    OffsetTop = oy,
                    OffsetRight = ox + w,
                    OffsetBottom = oy + h,
                    Color = Bracket,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                }
            );
        }

        Mark(0f, 0f, inset, inset, arm, weight);
        Mark(0f, 0f, inset, inset, weight, arm);
        Mark(1f, 0f, -inset - arm, inset, arm, weight);
        Mark(1f, 0f, -inset - weight, inset, weight, arm);
        Mark(0f, 1f, inset, -inset - weight, arm, weight);
        Mark(0f, 1f, inset, -inset - arm, weight, arm);
        Mark(1f, 1f, -inset - arm, -inset - weight, arm, weight);
        Mark(1f, 1f, -inset - weight, -inset - arm, weight, arm);

        return root;
    }
}
