shader_type canvas_item;

uniform vec4 fill_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform vec4 rim_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 1.0, 0.12);
uniform vec4 accent_color : source_color = vec4(0.20, 0.88, 1.0, 1.0);
uniform vec4 core_color : source_color = vec4(0.05, 0.32, 0.92, 0.50);
uniform vec4 spark_color : source_color = vec4(1.0, 0.82, 0.34, 1.0);
uniform float ring_width : hint_range(0.01, 0.12) = 0.032;
uniform float glow_size : hint_range(0.0, 0.12) = 0.012;
uniform float soft_edge : hint_range(0.001, 0.04) = 0.01;
uniform float aa_strength : hint_range(0.5, 3.0) = 1.25;
uniform float center_brightness : hint_range(0.0, 1.0) = 0.45;
uniform float ring_radius : hint_range(0.18, 0.46) = 0.32;
uniform float arrow_radius : hint_range(0.24, 0.58) = 0.43;
uniform float arrow_size : hint_range(0.04, 0.18) = 0.105;
uniform float ring_rotation_speed : hint_range(-4.0, 4.0) = 0.85;
uniform float arrow_rotation_speed : hint_range(-4.0, 4.0) = 1.18;
uniform float inner_ring_radius : hint_range(0.10, 0.38) = 0.22;
uniform float outer_ring_radius : hint_range(0.34, 0.55) = 0.47;
uniform float pulse_speed : hint_range(0.0, 8.0) = 2.2;
uniform float scan_strength : hint_range(0.0, 1.0) = 0.35;
uniform float spark_strength : hint_range(0.0, 1.0) = 0.75;
uniform float tick_count : hint_range(8.0, 48.0) = 28.0;


vec2 rotate(vec2 p, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return vec2(p.x * c - p.y * s, p.x * s + p.y * c);
}

float hash(float n)
{
    return fract(sin(n) * 43758.5453123);
}

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

float ring_band(vec2 p, float radius, float width)
{
    return stroke(abs(length(p) - radius), width);
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

float tick_marks(vec2 p, float radius, float rotation, float count, float mark_width, float mark_length)
{
    float r = length(p);
    float angle = atan(p.y, p.x) + rotation;
    float step_angle = TAU / count;
    float tick_center = (floor((angle + PI) / step_angle) + 0.5) * step_angle - PI;
    float tick_angle = abs(angle_diff(angle, tick_center));
    float angular_mask = 1.0 - smoothstep(mark_width, mark_width + soft_edge * 1.8, tick_angle);
    float radial_mask = 1.0 - smoothstep(mark_length, mark_length + soft_edge, abs(r - radius));
    float every_other = 0.72 + 0.28 * step(0.5, hash(floor((angle + PI) / step_angle)));
    return angular_mask * radial_mask * every_other;
}

float scan_wedge(vec2 p, float rotation)
{
    float angle = atan(p.y, p.x);
    float delta = abs(angle_diff(angle, rotation));
    float radial = smoothstep(0.02, outer_ring_radius, length(p));
    float wedge = 1.0 - smoothstep(0.0, 0.45, delta);
    return wedge * radial * scan_strength;
}

float energy_spark(vec2 p, float rotation, float index)
{
    float seed = index * 41.73;
    float time_cell = floor(TIME * 8.0 + hash(seed) * 4.0);
    float active = step(0.72, hash(time_cell + seed));
    float angle = rotation + hash(time_cell + seed * 1.37) * TAU;
    float radius = mix(inner_ring_radius + 0.04, outer_ring_radius - 0.02, hash(time_cell + seed * 2.21));
    vec2 origin = vec2(cos(angle), sin(angle)) * radius;
    vec2 dir = normalize(vec2(cos(angle + mix(-0.85, 0.85, hash(seed))), sin(angle + mix(-0.85, 0.85, hash(seed)))));
    vec2 end = origin + dir * mix(0.05, 0.13, hash(time_cell + seed * 0.71));
    float bolt = stroke(line_segment(p, origin, end), ring_width * 0.18);
    float flicker = 0.65 + 0.35 * sin(TIME * 42.0 + seed);
    return bolt * active * flicker * spark_strength;
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
    vec4 modulate_color = COLOR;
    vec2 p = UV - vec2(0.5);
    float radius = length(p);
    float time = TIME;
    float ring_rotation = TIME * ring_rotation_speed;
    float arrow_rotation = TIME * arrow_rotation_speed;
    float pulse = 0.78 + 0.22 * sin(time * pulse_speed);
    float fast_pulse = 0.65 + 0.35 * sin(time * pulse_speed * 2.7);

    float arcs = 0.0;
    arcs = max(arcs, arc_segment(p, ring_radius, ring_rotation - 1.40, 1.02));
    arcs = max(arcs, arc_segment(p, ring_radius, ring_rotation - 3.00, 0.92));
    arcs = max(arcs, arc_segment(p, ring_radius, ring_rotation + 0.20, 0.84));
    arcs = max(arcs, arc_segment(p, ring_radius, ring_rotation + 1.62, 0.96));
    arcs = max(arcs, arc_segment(p, inner_ring_radius, -ring_rotation * 1.35 + 0.20, 0.64) * 0.82);
    arcs = max(arcs, arc_segment(p, inner_ring_radius, -ring_rotation * 1.35 + 2.10, 0.52) * 0.82);
    arcs = max(arcs, arc_segment(p, outer_ring_radius, ring_rotation * 0.52 - 0.15, 0.50) * 0.62);
    arcs = max(arcs, arc_segment(p, outer_ring_radius, ring_rotation * 0.52 + 2.75, 0.44) * 0.62);

    float arrows = 0.0;
    arrows = max(arrows, arrow_outline(p, arrow_rotation - 0.98));
    arrows = max(arrows, arrow_outline(p, arrow_rotation + 0.42));
    arrows = max(arrows, arrow_outline(p, arrow_rotation + 1.98));
    arrows = max(arrows, arrow_outline(p, arrow_rotation - 2.70));

    float ticks = tick_marks(p, outer_ring_radius - 0.025, ring_rotation * 0.42, tick_count, 0.026, 0.018);
    ticks += tick_marks(p, inner_ring_radius + 0.035, -ring_rotation * 0.9, max(8.0, tick_count * 0.5), 0.04, 0.012) * 0.72;

    float inner_halo = ring_band(p, inner_ring_radius, ring_width * 1.45) * 0.22;
    float outer_halo = ring_band(p, outer_ring_radius, ring_width * 1.2) * 0.18;
    float scan = scan_wedge(p, arrow_rotation * 0.72);
    float sparks = 0.0;
    sparks = max(sparks, energy_spark(p, arrow_rotation, 1.0));
    sparks = max(sparks, energy_spark(p, -ring_rotation, 2.0));
    sparks = max(sparks, energy_spark(p, ring_rotation * 0.35, 3.0));

    float scanline = (sin((UV.y + time * 0.12) * 145.0) * 0.5 + 0.5) * 0.045 * scan_strength;
    float radial_wave = pow(1.0 - smoothstep(0.0, outer_ring_radius, radius), 2.0);
    radial_wave *= 0.5 + 0.5 * sin(radius * 72.0 - time * 7.0);

    float lines = clamp(max(max(arcs, arrows), ticks), 0.0, 1.0);
    float energy = clamp(lines + inner_halo + outer_halo + scan * 0.35 + sparks, 0.0, 1.0);
    float line_glow = (smoothstep(0.0, 1.0, energy) * glow_size * 10.0 + sparks * 0.08) * pulse;
    float core_strength = 0.25 + center_brightness * 0.75;
    float subtle_center = core_strength * (1.0 - smoothstep(0.02, ring_radius, radius));
    float vignette = 1.0 - smoothstep(outer_ring_radius + 0.08, 0.68, radius);

    vec3 color = fill_color.rgb * fill_color.a;
    color += core_color.rgb * core_color.a * subtle_center * pulse;
    color += accent_color.rgb * radial_wave * core_color.a * 0.35;
    color += accent_color.rgb * scan * 0.35;
    color += glow_color.rgb * clamp(line_glow, 0.0, 1.0);
    color = mix(color, accent_color.rgb, clamp(inner_halo + outer_halo + ticks * 0.45, 0.0, 0.7));
    color = mix(color, rim_color.rgb, lines);
    color = mix(color, spark_color.rgb, clamp(sparks, 0.0, 1.0));
    color += rim_color.rgb * subtle_center * 0.55;
    color += accent_color.rgb * scanline * vignette * fast_pulse;

    float alpha = lines * rim_color.a;
    alpha = max(alpha, (inner_halo + outer_halo + subtle_center * 0.38 + radial_wave * 0.18) * core_color.a);
    alpha = max(alpha, line_glow * glow_color.a * 8.0);
    alpha = max(alpha, (scan * 0.22 + scanline * 0.25) * accent_color.a);
    alpha = max(alpha, sparks * spark_color.a);
    COLOR = vec4(color, clamp(alpha, 0.0, 1.0) * modulate_color.a);
}
