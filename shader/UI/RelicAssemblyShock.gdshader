shader_type canvas_item;
render_mode unshaded, blend_add;

// Small local accents only; never flash the whole screen or require HDR bloom.
uniform float phase = 0.0;

float line_mask(float d, float thickness) {
    return 1.0 - smoothstep(thickness, thickness + 0.012, abs(d));
}

void fragment() {
    vec2 p = (UV - 0.5) * 2.0;
    float r = length(p);
    float t = clamp(phase, 0.0, 1.0);
    float decay = (1.0 - t) * (1.0 - t);
    float advance = 1.0 - pow(1.0 - t, 3.0);
    float radius = mix(0.11, 0.70, advance);
    float ring = line_mask(r - radius, mix(0.025, 0.005, t));
    float angle = atan(p.y, p.x + 0.00001);
    float shards = smoothstep(0.78, 0.99, cos(11.0 * angle + 2.2 * sin(3.0 * angle)));
    float ejecta = shards * line_mask(r - radius * 1.04, 0.11 * (1.0 - t));
    vec2 q = p * mat2(vec2(0.976, -0.218), vec2(0.218, 0.976));
    float crosslight = exp(-abs(q.y) * 90.0 - abs(q.x) * 4.5)
        + 0.68 * exp(-abs(q.x) * 115.0 - abs(q.y) * 7.5);
    float core = exp(-r * r * 66.0);
    float intensity = ring * 0.85 + ejecta * 0.92 + (crosslight + core) * (1.0 - advance);
    vec3 color = mix(vec3(0.62, 0.82, 1.0), vec3(1.0, 0.91, 0.68), clamp(crosslight + core, 0.0, 1.0));
    COLOR = vec4(color, clamp(intensity * decay, 0.0, 1.0) * (1.0 - smoothstep(0.94, 1.0, r)));
}
