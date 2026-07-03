shader_type canvas_item;

uniform vec4 glass_color : source_color = vec4(0.74, 0.92, 1.0, 0.92);
uniform vec4 sand_color : source_color = vec4(0.96, 0.84, 0.30, 1.0);
uniform vec4 glow_color : source_color = vec4(0.30, 0.78, 1.0, 0.22);
uniform float line_width : hint_range(0.006, 0.08) = 0.026;
uniform float aa_scale : hint_range(0.5, 2.5) = 1.15;
uniform float pulse_speed : hint_range(0.0, 4.0) = 1.2;

float aa_width(float distance) {
    return max(fwidth(distance) * aa_scale, 0.0012);
}

float fill_sdf(float distance) {
    float aa = aa_width(distance);
    return smoothstep(aa, -aa, distance);
}

float stroke_sdf(float distance, float width) {
    float aa = aa_width(distance);
    return smoothstep(width + aa, width - aa, abs(distance));
}

float sd_segment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float stroke_segment(vec2 p, vec2 a, vec2 b, float width) {
    return stroke_sdf(sd_segment(p, a, b), width);
}

float sd_triangle(vec2 p, vec2 a, vec2 b, vec2 c) {
    vec2 e0 = b - a;
    vec2 e1 = c - b;
    vec2 e2 = a - c;
    vec2 v0 = p - a;
    vec2 v1 = p - b;
    vec2 v2 = p - c;
    vec2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
    vec2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
    vec2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
    float s = sign(e0.x * e2.y - e0.y * e2.x);
    vec2 d = min(
        min(vec2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
            vec2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
        vec2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x))
    );
    return -sqrt(d.x) * sign(d.y);
}

float triangle(vec2 p, vec2 a, vec2 b, vec2 c) {
    return fill_sdf(sd_triangle(p, a, b, c));
}

float rounded_cap(vec2 p, vec2 center, vec2 half_size, float radius) {
    vec2 q = abs(p - center) - half_size + vec2(radius);
    float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
    return fill_sdf(d);
}

void blend_layer(inout vec3 rgb, inout float alpha, vec3 layer_rgb, float layer_alpha) {
    layer_alpha = clamp(layer_alpha, 0.0, 1.0);
    rgb = mix(rgb, layer_rgb, layer_alpha);
    alpha = max(alpha, layer_alpha);
}

void fragment() {
    vec2 p = UV;
    vec2 c = vec2(0.5);
    float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed);

    float frame = 0.0;
    frame = max(frame, stroke_segment(p, vec2(0.28, 0.19), vec2(0.72, 0.19), line_width));
    frame = max(frame, stroke_segment(p, vec2(0.28, 0.81), vec2(0.72, 0.81), line_width));
    frame = max(frame, stroke_segment(p, vec2(0.32, 0.24), vec2(0.50, 0.50), line_width));
    frame = max(frame, stroke_segment(p, vec2(0.68, 0.24), vec2(0.50, 0.50), line_width));
    frame = max(frame, stroke_segment(p, vec2(0.32, 0.76), vec2(0.50, 0.50), line_width));
    frame = max(frame, stroke_segment(p, vec2(0.68, 0.76), vec2(0.50, 0.50), line_width));

    float cap = 0.0;
    cap = max(cap, rounded_cap(p, vec2(0.5, 0.17), vec2(0.27, 0.030), 0.020));
    cap = max(cap, rounded_cap(p, vec2(0.5, 0.83), vec2(0.27, 0.030), 0.020));

    float top_sand = triangle(p, vec2(0.36, 0.28), vec2(0.64, 0.28), vec2(0.50, 0.47));
    float bottom_sand = triangle(p, vec2(0.39, 0.76), vec2(0.61, 0.76), vec2(0.50, 0.56));
    float stream = stroke_segment(p, vec2(0.50, 0.475), vec2(0.50, 0.565), 0.010 + pulse * 0.003);
    float sand = max(max(top_sand, bottom_sand), stream);

    float shine = stroke_segment(p, vec2(0.39, 0.285), vec2(0.48, 0.415), 0.006) * 0.45;
    shine = max(shine, stroke_segment(p, vec2(0.59, 0.610), vec2(0.64, 0.735), 0.005) * 0.32);
    float glow = smoothstep(0.57, 0.10, length(p - c)) * (0.18 + pulse * 0.07);

    vec3 rgb = glow_color.rgb;
    float alpha = 0.0;
    blend_layer(rgb, alpha, glow_color.rgb, glow * glow_color.a);
    blend_layer(rgb, alpha, glass_color.rgb, frame * glass_color.a);
    blend_layer(rgb, alpha, glass_color.rgb * 0.72, cap * 0.55);
    blend_layer(rgb, alpha, sand_color.rgb, sand * sand_color.a);
    blend_layer(rgb, alpha, vec3(1.0), shine * 0.34);

    alpha *= smoothstep(0.73, 0.48, length(p - c));
    COLOR = vec4(rgb, alpha);
}
