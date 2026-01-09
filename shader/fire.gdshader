shader_type canvas_item;

// --- Palette ---
uniform vec4 color_outer : source_color = vec4(0.96, 0.35, 0.15, 1.0);
uniform vec4 color_inner : source_color = vec4(1.0, 0.8, 0.0, 1.0);
uniform vec4 color_core : source_color = vec4(1.0, 1.0, 0.9, 1.0);

// --- Controls ---
uniform float speed = 1.0;
uniform float horizontal_sway = 0.5;
uniform vec2 noise_scale = vec2(2.0, 3.0);

// --- Thresholds ---
uniform float outer_threshold : hint_range(0.0, 1.0) = 0.25;
uniform float inner_threshold : hint_range(0.0, 1.0) = 0.55;
uniform float core_threshold : hint_range(0.0, 1.0) = 0.8;

// --- Boundary Limit ---
uniform float edge_margin : hint_range(0.0, 0.5) = 0.15; // How far from edge to stop

// --- Helper Functions ---
vec2 hash(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

float noise(vec2 p) {
    const float K1 = 0.366025404;
    const float K2 = 0.211324865;
    vec2 i = floor(p + (p.x + p.y) * K1);
    vec2 a = p - i + (i.x + i.y) * K2;
    float m = step(a.y, a.x);
    vec2 o = vec2(m, 1.0 - m);
    vec2 b = a - o + K2;
    vec2 c = a - 1.0 + 2.0 * K2;
    vec3 h = max(0.5 - vec3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
    vec3 n = h * h * h * h * vec3(dot(a, hash(i + 0.0)), dot(b, hash(i + o)), dot(c, hash(i + 1.0)));
    return dot(n, vec3(70.0));
}

void fragment() {
    vec2 uv = UV;
    vec2 centered_uv = uv - 0.5;

    // 1. Base Gradient (Shape)
    float y_grad = smoothstep(1.0, 0.2, uv.y);
    float x_grad = 1.0 - abs(centered_uv.x) * 2.5;
    x_grad = smoothstep(0.0, 1.0, x_grad);
    float base_shape = y_grad * x_grad;

    // 2. Distortion & Noise
    float time = TIME * speed;
    vec2 noise_uv = uv * noise_scale;
    noise_uv.y -= time;
    noise_uv.x += sin(uv.y * 10.0 - time) * horizontal_sway;

    float n = noise(noise_uv);

    // 3. Calculate Heat
    float heat = base_shape + n * 0.5 - (uv.y * 0.8);

    // 4. BOUNDARY LIMIT (The Fix)
    // We calculate the distance to the closest edge (Left, Right, Top, or Bottom)
    // "d_edge" will be 0.0 at the edge and 0.5 at the center
    float d_edge = min(uv.x, min(uv.y, min(1.0 - uv.x, 1.0 - uv.y)));

    // We create a multiplier that is 0.0 at the edge and 1.0 inside
    float boundary_mask = smoothstep(0.0, edge_margin, d_edge);

    // Multiply HEAT by the mask.
    // This reduces the heat near edges, forcing the threshold check
    // below to fail, causing the fire to "shrink" cleanly.
    heat *= boundary_mask;

    // 5. Threshold Coloring (Toon Look)
    vec4 final_color = vec4(0.0);

    if (heat > core_threshold) {
        final_color = color_core;
    } else if (heat > inner_threshold) {
        final_color = color_inner;
    } else if (heat > outer_threshold) {
        final_color = color_outer;
    }

    COLOR = final_color;
}