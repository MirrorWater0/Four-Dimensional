shader_type canvas_item;

// --- Settings ---
uniform vec4 color : source_color = vec4(0.0, 1.0, 0.0, 1.0); // Green
uniform float thickness : hint_range(0.0, 1.0) = 0.25;        // Width of the bars
uniform float cross_size : hint_range(0.0, 1.0) = 0.8;        // Length of the bars (keep < 1.0 to avoid touching edges)
uniform float softness : hint_range(0.0, 0.1) = 0.01;         // Edge smoothing

void fragment() {
    vec2 uv = UV - 0.5;
    vec2 d = abs(uv);

    float half_thick = thickness * 0.5;
    float half_size = cross_size * 0.5;

    // --- Vertical Bar ---
    // Width defined by d.x, Height defined by d.y
    float v_bar_width = 1.0 - smoothstep(half_thick - softness, half_thick + softness, d.x);
    float v_bar_height = 1.0 - smoothstep(half_size - softness, half_size + softness, d.y);
    float vertical_bar = v_bar_width * v_bar_height; // Multiply to crop the height

    // --- Horizontal Bar ---
    // Height defined by d.y, Width defined by d.x
    float h_bar_height = 1.0 - smoothstep(half_thick - softness, half_thick + softness, d.y);
    float h_bar_width = 1.0 - smoothstep(half_size - softness, half_size + softness, d.x);
    float horizontal_bar = h_bar_height * h_bar_width; // Multiply to crop the width

    // Combine them
    float shape = max(vertical_bar, horizontal_bar);

    COLOR = vec4(color.rgb, shape * color.a);
}
