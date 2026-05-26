shader_type canvas_item;

uniform vec4 fill_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 rim_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 1.0, 0.12);
uniform float ring_width : hint_range(0.01, 0.12) = 0.032;
uniform float glow_size : hint_range(0.0, 0.12) = 0.012;
uniform float soft_edge : hint_range(0.001, 0.04) = 0.01;
uniform float aa_strength : hint_range(0.5, 3.0) = 1.25;
uniform float center_brightness : hint_range(0.0, 1.0) = 0.0;
uniform float ring_radius : hint_range(0.18, 0.46) = 0.32;
uniform float arrow_radius : hint_range(0.24, 0.58) = 0.43;
uniform float arrow_size : hint_range(0.04, 0.18) = 0.105;
uniform float ring_rotation_speed : hint_range(-4.0, 4.0) = 0.85;
uniform float arrow_rotation_speed : hint_range(-4.0, 4.0) = 1.18;

float angle_diff(float a, float b)
{
    return atan(sin(a - b), cos(a - b));
}

float line_segment(vec2 p, vec2 a, vec2 b)
{
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float stroke(float d, float width)
{
    float edge = max(fwidth(d) * aa_strength, soft_edge * 0.35);
    return 1.0 - smoothstep(width - edge, width + edge, d);
}

float arc_segment(vec2 p, float radius, float center_angle, float arc_length)
{
    float angle = atan(p.y, p.x);
    float radial = abs(length(p) - radius);
    float angle_delta = abs(angle_diff(angle, center_angle));
    float angle_edge = max(fwidth(angle_delta) * aa_strength, soft_edge * 1.5);
    float angle_mask = 1.0 - smoothstep(
        arc_length * 0.5,
        arc_length * 0.5 + angle_edge,
        angle_delta
    );
    return stroke(radial, ring_width) * angle_mask;
}

float arrow_outline(vec2 p, float angle)
{
    vec2 normal = vec2(cos(angle), sin(angle));
    vec2 center = normal * arrow_radius;
    vec2 tangent = vec2(-sin(angle), cos(angle));

    vec2 tip = center - normal * arrow_size * 0.58;
    vec2 base_center = center + normal * arrow_size * 0.44;
    vec2 base_a = base_center + tangent * arrow_size * 0.46;
    vec2 base_b = base_center - tangent * arrow_size * 0.46;

    float d = min(
        line_segment(p, tip, base_a),
        min(line_segment(p, base_a, base_b), line_segment(p, base_b, tip))
    );
    return stroke(d, ring_width * 0.72);
}

void fragment()
{
    vec2 p = UV - vec2(0.5);
    float ring_rotation = TIME * ring_rotation_speed;
    float arrow_rotation = TIME * arrow_rotation_speed;

    float arcs = 0.0;
    arcs = max(arcs, arc_segment(p, ring_radius, ring_rotation - 1.40, 1.02));
    arcs = max(arcs, arc_segment(p, ring_radius, ring_rotation - 3.00, 0.92));
    arcs = max(arcs, arc_segment(p, ring_radius, ring_rotation + 0.20, 0.84));
    arcs = max(arcs, arc_segment(p, ring_radius, ring_rotation + 1.62, 0.96));

    float arrows = 0.0;
    arrows = max(arrows, arrow_outline(p, arrow_rotation - 0.98));
    arrows = max(arrows, arrow_outline(p, arrow_rotation + 0.42));
    arrows = max(arrows, arrow_outline(p, arrow_rotation + 1.98));
    arrows = max(arrows, arrow_outline(p, arrow_rotation - 2.70));

    float lines = clamp(max(arcs, arrows), 0.0, 1.0);
    float line_glow = smoothstep(0.0, 1.0, lines) * glow_size;
    float subtle_center = center_brightness * (1.0 - smoothstep(0.0, ring_radius, length(p)));

    vec3 color = fill_color.rgb * fill_color.a;
    color = mix(color, glow_color.rgb, clamp(line_glow, 0.0, 1.0));
    color = mix(color, rim_color.rgb, lines);
    color += rim_color.rgb * subtle_center;

    float alpha = lines * rim_color.a;
    alpha = max(alpha, line_glow * glow_color.a);
    COLOR = vec4(color, alpha * COLOR.a);
}
