shader_type canvas_item;

uniform vec4 ink_color : source_color = vec4(0.03, 0.025, 0.02, 1.0);
uniform vec4 paper_color : source_color = vec4(0.86, 0.66, 0.46, 1.0);
uniform vec4 accent_color : source_color = vec4(1.0, 0.84, 0.36, 1.0);
uniform vec4 glow_color : source_color = vec4(0.95, 0.72, 0.34, 0.30);
uniform float line_width : hint_range(0.01, 0.12) = 0.030;
uniform float aa_scale : hint_range(0.5, 2.5) = 1.25;
uniform float active : hint_range(0.0, 1.0) = 0.0;
uniform float pulse_speed : hint_range(0.0, 4.0) = 1.4;

float sd_segment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float aa_width(float distance) {
    return max(fwidth(distance) * aa_scale, 0.0012);
}

float stroke(float distance, float width) {
    float aa = aa_width(distance);
    return smoothstep(width + aa, width - aa, distance);
}

float fill_sdf(float distance) {
    float aa = aa_width(distance);
    return smoothstep(aa, -aa, distance);
}

float circle(vec2 p, vec2 center, float radius) {
    float distance = length(p - center);
    float aa = aa_width(distance);
    return smoothstep(radius + aa, radius - aa, distance);
}

float stroke_segment(vec2 p, vec2 a, vec2 b, float width) {
    return stroke(sd_segment(p, a, b), width);
}

float ring(vec2 p, vec2 center, float radius, float width) {
    return stroke(abs(length(p - center) - radius), width);
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

float compass_ray(vec2 p, vec2 tip, vec2 left, vec2 right, vec2 center) {
    float ray_sdf = sd_triangle(p, tip, left, right);
    float fill = fill_sdf(ray_sdf);
    float outline = stroke(abs(ray_sdf), 0.014);
    float spine = stroke_segment(p, tip, center, 0.009) * fill;
    return max(fill * 0.78, max(outline, spine));
}

float hatch(vec2 p, vec2 tip, vec2 left, vec2 right, vec2 center, float phase) {
    float fill = fill_sdf(sd_triangle(p, tip, left, right));
    vec2 uv = p - center;
    float stripe_coord = (uv.x + uv.y) * 17.0 + phase;
    float stripe_distance = abs(fract(stripe_coord) - 0.5);
    float stripe_aa = max(fwidth(stripe_coord) * 0.65, 0.001);
    float stripes = smoothstep(0.055 + stripe_aa, 0.055 - stripe_aa, stripe_distance);
    return stripes * fill * 0.34;
}

float letter_n(vec2 p, vec2 c, float s) {
    float w = 0.006;
    float n = stroke_segment(p, c + vec2(-0.030, 0.040) * s, c + vec2(-0.030, -0.040) * s, w);
    n = max(n, stroke_segment(p, c + vec2(0.030, 0.040) * s, c + vec2(0.030, -0.040) * s, w));
    n = max(n, stroke_segment(p, c + vec2(-0.030, -0.040) * s, c + vec2(0.030, 0.040) * s, w));
    return n;
}

float letter_e(vec2 p, vec2 c, float s) {
    float w = 0.006;
    float e = stroke_segment(p, c + vec2(-0.030, -0.040) * s, c + vec2(-0.030, 0.040) * s, w);
    e = max(e, stroke_segment(p, c + vec2(-0.030, -0.040) * s, c + vec2(0.032, -0.040) * s, w));
    e = max(e, stroke_segment(p, c + vec2(-0.030, 0.000) * s, c + vec2(0.022, 0.000) * s, w));
    e = max(e, stroke_segment(p, c + vec2(-0.030, 0.040) * s, c + vec2(0.032, 0.040) * s, w));
    return e;
}

float letter_w(vec2 p, vec2 c, float s) {
    float w = 0.006;
    float l = stroke_segment(p, c + vec2(-0.040, -0.040) * s, c + vec2(-0.020, 0.040) * s, w);
    l = max(l, stroke_segment(p, c + vec2(-0.020, 0.040) * s, c + vec2(0.000, -0.010) * s, w));
    l = max(l, stroke_segment(p, c + vec2(0.000, -0.010) * s, c + vec2(0.020, 0.040) * s, w));
    l = max(l, stroke_segment(p, c + vec2(0.020, 0.040) * s, c + vec2(0.040, -0.040) * s, w));
    return l;
}

float letter_s(vec2 p, vec2 c, float s) {
    float w = 0.006;
    float l = stroke_segment(p, c + vec2(0.030, -0.035) * s, c + vec2(-0.026, -0.035) * s, w);
    l = max(l, stroke_segment(p, c + vec2(-0.026, -0.035) * s, c + vec2(-0.030, 0.000) * s, w));
    l = max(l, stroke_segment(p, c + vec2(-0.030, 0.000) * s, c + vec2(0.026, 0.000) * s, w));
    l = max(l, stroke_segment(p, c + vec2(0.026, 0.000) * s, c + vec2(0.030, 0.035) * s, w));
    l = max(l, stroke_segment(p, c + vec2(0.030, 0.035) * s, c + vec2(-0.030, 0.035) * s, w));
    return l;
}

void fragment() {
    vec2 p = UV;
    vec2 center = vec2(0.5);
    vec2 v = p - center;
    float dist = length(v);
    float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed);

    float cardinal = 0.0;
    cardinal = max(cardinal, compass_ray(p, vec2(0.50, 0.04), vec2(0.42, 0.43), vec2(0.58, 0.43), center));
    cardinal = max(cardinal, compass_ray(p, vec2(0.96, 0.50), vec2(0.57, 0.42), vec2(0.57, 0.58), center));
    cardinal = max(cardinal, compass_ray(p, vec2(0.50, 0.96), vec2(0.58, 0.57), vec2(0.42, 0.57), center));
    cardinal = max(cardinal, compass_ray(p, vec2(0.04, 0.50), vec2(0.43, 0.58), vec2(0.43, 0.42), center));

    float diagonal = 0.0;
    diagonal = max(diagonal, compass_ray(p, vec2(0.80, 0.20), vec2(0.55, 0.39), vec2(0.61, 0.45), center));
    diagonal = max(diagonal, compass_ray(p, vec2(0.80, 0.80), vec2(0.61, 0.55), vec2(0.55, 0.61), center));
    diagonal = max(diagonal, compass_ray(p, vec2(0.20, 0.80), vec2(0.45, 0.61), vec2(0.39, 0.55), center));
    diagonal = max(diagonal, compass_ray(p, vec2(0.20, 0.20), vec2(0.39, 0.45), vec2(0.45, 0.39), center));

    float texture_lines = 0.0;
    texture_lines = max(texture_lines, hatch(p, vec2(0.50, 0.04), vec2(0.42, 0.43), vec2(0.58, 0.43), center, 0.0));
    texture_lines = max(texture_lines, hatch(p, vec2(0.96, 0.50), vec2(0.57, 0.42), vec2(0.57, 0.58), center, 0.18));
    texture_lines = max(texture_lines, hatch(p, vec2(0.50, 0.96), vec2(0.58, 0.57), vec2(0.42, 0.57), center, 0.35));
    texture_lines = max(texture_lines, hatch(p, vec2(0.04, 0.50), vec2(0.43, 0.58), vec2(0.43, 0.42), center, 0.52));

    float center_outer = ring(p, center, 0.170, 0.017);
    float center_mid = ring(p, center, 0.105, 0.013);
    float center_cross = stroke(abs(v.x), 0.006) * smoothstep(0.17, 0.15, abs(v.y));
    center_cross = max(center_cross, stroke(abs(v.y), 0.006) * smoothstep(0.17, 0.15, abs(v.x)));
    float center_dot = circle(p, center, 0.044);
    float center_fill = circle(p, center, 0.185) * 0.58;

    float letters = 0.0;
    letters = max(letters, letter_n(p, vec2(0.50, 0.285), 1.05));
    letters = max(letters, letter_e(p, vec2(0.700, 0.505), 0.95));
    letters = max(letters, letter_s(p, vec2(0.50, 0.705), 0.95));
    letters = max(letters, letter_w(p, vec2(0.300, 0.505), 0.95));

    float shape = max(max(cardinal, diagonal * 0.78), center_fill);
    float ink = max(max(texture_lines, center_outer), max(center_mid, center_cross));
    ink = max(ink, max(center_dot, letters));

    float glow = smoothstep(0.62, 0.16, dist) * (0.22 + active * (0.30 + pulse * 0.24));
    vec3 fill_color = mix(paper_color.rgb, accent_color.rgb, active * 0.45);
    vec3 rgb = glow_color.rgb;
    rgb = mix(rgb, fill_color, shape);
    rgb = mix(rgb, ink_color.rgb, ink);

    float alpha = glow * glow_color.a;
    alpha = max(alpha, shape * paper_color.a);
    alpha = max(alpha, ink * ink_color.a);
    alpha *= smoothstep(0.72, 0.49, dist);

    COLOR = vec4(rgb, alpha);
}
