shader_type canvas_item;

// --- State & Interaction ---
uniform float state : hint_range(0.0, 1.0) = 0.0; // 0.0 = Off, 1.0 = On
uniform float hover : hint_range(0.0, 1.0) = 0.0; // Subtle glow on hover

// --- Colors ---
uniform vec4 bg_off : source_color = vec4(0.1, 0.12, 0.15, 1.0);
uniform vec4 bg_on : source_color = vec4(0.1, 0.4, 0.8, 1.0);
uniform vec4 knob_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.5, 0.8, 1.0, 1.0);

// --- Settings ---
uniform float knob_size : hint_range(0.5, 0.9) = 0.8; // Relative to height
uniform float softness : hint_range(0.0, 0.05) = 0.01;

// SDF for a Rounded Box (The Capsule)
float sdRoundedBox(vec2 p, vec2 b, vec4 r) {
    r.xy = (p.x > 0.0) ? r.xy : r.zw;
    r.x  = (p.y > 0.0) ? r.x  : r.y;
    vec2 q = abs(p) - b + r.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
}

void fragment() {
    // 1. Aspect Ratio Correction
    vec2 res = 1.0 / SCREEN_PIXEL_SIZE;
    float aspect = res.x / res.y;
    vec2 uv = UV - 0.5;
    uv.x *= aspect;

    // Track dimensions
    float h = 0.4;
    float w = aspect * 0.45;
    float r = h; // Full rounding

    // 2. Track (Background)
    float d_track = sdRoundedBox(uv, vec2(w, h), vec4(r));
    float track_mask = smoothstep(softness, -softness, d_track);

    // Smooth transition between Off/On background colors
    vec3 current_bg = mix(bg_off.rgb, bg_on.rgb, state);
    // Add a subtle "glow" when ON or HOVERED
    current_bg += accent_color.rgb * (state * 0.2 + hover * 0.1);

    // 3. Knob (The Slider)
    // Movement range: from left to right inside the capsule
    float x_limit = w - h;
    float knob_x = mix(-x_limit, x_limit, state);

    vec2 knob_uv = uv - vec2(knob_x, 0.0);
    float knob_r = h * knob_size;
    float d_knob = length(knob_uv) - knob_r;
    float knob_mask = smoothstep(softness, -softness, d_knob);

    // 4. Knob Design (Glass & Lighting)
    // Basic knob color + top-down gloss highlight
    float gloss = smoothstep(0.1, -0.1, length(knob_uv + vec2(0.0, knob_r * 0.5)) - knob_r * 0.5);
    vec3 knob_final = mix(knob_color.rgb, vec3(1.0), gloss * 0.5);

    // 5. Final Composition
    vec3 color = mix(current_bg, knob_final, knob_mask);

    // Internal Rim Lighting for the track
    float rim = smoothstep(-0.02, 0.0, d_track) * smoothstep(0.02, 0.0, d_track + 0.04);
    color += accent_color.rgb * rim * (state * 0.5 + 0.2);

    COLOR = vec4(color, track_mask);
}
