shader_type canvas_item;

// --- Colors ---
uniform vec4 color_start : source_color = vec4(0.2, 0.3, 0.9, 1.0);
uniform vec4 color_end : source_color = vec4(0.6, 0.9, 1.0, 1.0);

// --- Shape Settings ---
uniform float size : hint_range(0.1, 1.5) = 0.5;
uniform float thickness : hint_range(0.0, 0.2) = 0.03;
uniform float curvature : hint_range(0.1, 1.0) = 0.6;

// --- Animation ---
uniform float rotation_speed = 0.2;
uniform float pulse_speed = 1.0;

// --- Border Fade ---
uniform float edge_softness : hint_range(0.0, 0.5) = 0.1; // How close to the edge to start fading

vec2 rotate(vec2 uv, float angle) {
    float c = cos(angle);
    float s = sin(angle);
    return vec2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
}

void fragment() {
    // 1. Center Coordinates (-1.0 to 1.0)
    vec2 p = 2.0 * (UV - 0.5);

    // 2. Animation
    float time = TIME;
    vec2 rotated_p = rotate(p, time * rotation_speed);
    float current_size = size + sin(time * pulse_speed) * 0.05;

    // 3. Star Shape Math
    float star_shape = pow(abs(rotated_p.x), curvature) + pow(abs(rotated_p.y), curvature);
    float sdf = star_shape - current_size;

    // 4. Glow Logic
    float glow = thickness / abs(sdf);
    glow = clamp(glow, 0.0, 1.0);

    // 5. Gradient Color
    float gradient_pos = (p.x + p.y + 2.0) / 4.0;
    vec4 final_color = mix(color_start, color_end, gradient_pos);

    // 6. BORDER MASK (The Fix)
    // Calculate distance to the closest edge of the UV box
    // max(abs(p.x), abs(p.y)) is 0.0 at center and 1.0 at edges.
    float dist_to_edge = 1.0 - max(abs(p.x), abs(p.y));
    
    // Smoothly fade from 1.0 (visible) to 0.0 (invisible) near the edge
    float mask = smoothstep(0.0, edge_softness, dist_to_edge);

    // 7. Apply
    COLOR = final_color;
    COLOR.a *= glow * mask;
}