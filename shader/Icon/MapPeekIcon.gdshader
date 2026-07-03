shader_type canvas_item;

uniform vec4 ink_color : source_color = vec4(0.78, 0.93, 1.0, 1.0);
uniform vec4 paper_color : source_color = vec4(0.08, 0.19, 0.25, 0.86);
uniform vec4 accent_color : source_color = vec4(0.36, 0.78, 1.0, 1.0);
uniform vec4 glow_color : source_color = vec4(0.30, 0.82, 1.0, 0.32);
uniform float line_width : hint_range(0.01, 0.12) = 0.030;
uniform float aa_scale : hint_range(0.5, 2.5) = 1.25;
uniform float active : hint_range(0.0, 1.0) = 0.0;
uniform float hover_amount : hint_range(0.0, 1.0) = 0.0;
uniform float pulse_speed : hint_range(0.0, 4.0) = 1.4;

float ease_out(float t) {
    return 1.0 - pow(1.0 - clamp(t, 0.0, 1.0), 3.0);
}

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

float sd_round_box(vec2 p, vec2 half_size, float radius) {
    vec2 q = abs(p) - half_size + vec2(radius);
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
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

float circle(vec2 p, vec2 center, float radius) {
    return fill_sdf(length(p - center) - radius);
}

float ring(vec2 p, vec2 center, float radius, float width) {
    return stroke_sdf(length(p - center) - radius, width);
}

float page_mask(vec2 p, vec2 center, vec2 half_size, float radius) {
    return fill_sdf(sd_round_box(p - center, half_size, radius));
}

float page_edge(vec2 p, vec2 center, vec2 half_size, float radius, float width) {
    return stroke_sdf(sd_round_box(p - center, half_size, radius), width);
}

float fold_line(vec2 p, float x, float y0, float y1, float width) {
    return stroke_segment(p, vec2(x, y0), vec2(x, y1), width);
}

void blend_layer(inout vec3 rgb, inout float alpha, vec3 layer_rgb, float layer_alpha) {
    layer_alpha = clamp(layer_alpha, 0.0, 1.0);
    rgb = mix(rgb, layer_rgb, layer_alpha);
    alpha = max(alpha, layer_alpha);
}

void fragment() {
    vec2 p = UV;
    vec2 c = vec2(0.5);
    float hover = ease_out(hover_amount);
    float state = max(active, hover * 0.75);
    float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed);

    float spread = mix(0.010, 0.052, hover);
    vec2 page_size = vec2(mix(0.155, 0.175, hover), mix(0.300, 0.330, hover));
    vec2 left_center = vec2(0.385 - spread, 0.505);
    vec2 mid_center = vec2(0.500, 0.500 - hover * 0.010);
    vec2 right_center = vec2(0.615 + spread, 0.505);

    float left = page_mask(p, left_center, page_size, 0.030);
    float mid = page_mask(p, mid_center, page_size + vec2(0.010, 0.020), 0.032);
    float right = page_mask(p, right_center, page_size, 0.030);
    float page = max(max(left, mid), right);

    float edge_width = mix(0.010, 0.013, state);
    float edge = 0.0;
    edge = max(edge, page_edge(p, left_center, page_size, 0.030, edge_width));
    edge = max(edge, page_edge(p, mid_center, page_size + vec2(0.010, 0.020), 0.032, edge_width));
    edge = max(edge, page_edge(p, right_center, page_size, 0.030, edge_width));

    float fold = 0.0;
    fold = max(fold, fold_line(p, 0.442 - spread * 0.44, 0.215, 0.795, 0.006));
    fold = max(fold, fold_line(p, 0.558 + spread * 0.44, 0.215, 0.795, 0.006));
    fold *= 0.70 + hover * 0.20;

    float route_width = mix(0.012, 0.016, state);
    float route = 0.0;
    route = max(route, stroke_segment(p, vec2(0.285 - spread, 0.610), vec2(0.405 - spread * 0.45, 0.485), route_width));
    route = max(route, stroke_segment(p, vec2(0.405 - spread * 0.45, 0.485), vec2(0.535 + spread * 0.10, 0.565), route_width));
    route = max(route, stroke_segment(p, vec2(0.535 + spread * 0.10, 0.565), vec2(0.710 + spread, 0.400), route_width));
    route *= page;

    float waypoint = 0.0;
    waypoint = max(waypoint, circle(p, vec2(0.285 - spread, 0.610), 0.021 + hover * 0.004));
    waypoint = max(waypoint, circle(p, vec2(0.535 + spread * 0.10, 0.565), 0.018 + hover * 0.004));
    waypoint = max(waypoint, circle(p, vec2(0.710 + spread, 0.400), 0.023 + hover * 0.004));
    waypoint *= page;

    vec2 pin_center = vec2(0.498, 0.395 - hover * 0.014);
    float pin_outer = ring(p, pin_center, 0.069 + hover * 0.006, 0.016);
    float pin_dot = circle(p, pin_center, 0.022 + hover * 0.004);
    float pin_tail = stroke_segment(
        p,
        pin_center + vec2(0.000, 0.067),
        pin_center + vec2(0.000, 0.133 + hover * 0.012),
        0.014
    );
    float pin = max(max(pin_outer, pin_dot), pin_tail) * max(mid, 0.55 + hover * 0.45);

    float highlight = page_mask(p, mid_center + vec2(-0.040, -0.105), vec2(0.115, 0.020), 0.010) * 0.24;
    highlight += page_mask(p, right_center + vec2(0.028, 0.095), vec2(0.072, 0.014), 0.008) * 0.18;
    highlight *= page * (0.45 + hover * 0.65);

    float shadow = page_mask(p + vec2(0.000, -0.026), c, vec2(0.365 + hover * 0.045, 0.340), 0.055);
    float glow = smoothstep(0.58, 0.08, length(p - c)) * (0.10 + state * (0.22 + pulse * 0.09));

    vec3 rgb = glow_color.rgb * 0.30;
    float alpha = 0.0;
    blend_layer(rgb, alpha, vec3(0.0), shadow * 0.26);

    vec3 page_rgb = mix(paper_color.rgb, vec3(0.075, 0.235, 0.315), hover * 0.35);
    page_rgb = mix(page_rgb, accent_color.rgb * 0.55 + page_rgb * 0.45, active * 0.26);
    blend_layer(rgb, alpha, page_rgb, page * paper_color.a);

    blend_layer(rgb, alpha, accent_color.rgb, glow * glow_color.a);
    blend_layer(rgb, alpha, ink_color.rgb * 0.56, fold * 0.46);
    blend_layer(rgb, alpha, accent_color.rgb, route * (0.72 + state * 0.24));
    blend_layer(rgb, alpha, accent_color.rgb * 1.14, waypoint * (0.88 + state * 0.12));
    blend_layer(rgb, alpha, ink_color.rgb, edge * (0.72 + state * 0.20));
    blend_layer(rgb, alpha, vec3(1.0), highlight * 0.42);
    blend_layer(rgb, alpha, mix(ink_color.rgb, accent_color.rgb, 0.55 + state * 0.30), pin * (0.90 + state * 0.10));

    alpha *= smoothstep(0.76, 0.46, length(p - c));
    COLOR = vec4(rgb, alpha);
}
