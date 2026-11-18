shader_type canvas_item;
// Write alpha as well as color so the rectangular backing fills transparent card corners.
render_mode blend_mix;

// Keep width as the visibility control used by SkillCard.SetPlayableHighlight.
uniform float width : hint_range(0.0, 1.0) = 0.0;
uniform vec2 card_size = vec2(240.0, 370.0);
uniform vec2 effect_margin = vec2(96.0, 96.0);
uniform vec2 ray_length_px = vec2(84.0, 76.0);
uniform float line_width_px : hint_range(0.5, 4.0) = 1.1;
uniform float ray_width_px : hint_range(1.0, 10.0) = 4.5;
uniform float glow_radius_px : hint_range(2.0, 32.0) = 12.0;
uniform vec2 fill_extent_px = vec2(72.0, 68.0);
uniform float fill_strength : hint_range(0.0, 1.0) = 0.65;
uniform float inner_fill_strength : hint_range(0.0, 1.0) = 1.0;
uniform vec4 inner_fill_color : source_color = vec4(0.32, 0.68, 0.9, 1.0);
uniform vec4 inner_center_color : source_color = vec4(0.14, 0.3, 0.55, 1.0);
uniform vec4 core_color : source_color = vec4(0.88, 0.97, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.32, 0.68, 0.9, 1.0);
uniform vec4 fill_outer_color : source_color = vec4(0.16, 0.36, 0.7, 1.0);
uniform vec4 spark_color : source_color = vec4(0.68, 0.9, 1.0, 1.0);
uniform float flow_speed : hint_range(0.0, 3.0) = 0.65;
uniform float pulse_speed : hint_range(0.0, 6.0) = 2.0;
uniform float intensity : hint_range(0.0, 3.0) = 1.0;

// One card edge and its two tapered extensions. Swapping axes gives the other pair.
vec3 edge_light(float along, float across, float half_length, float extension) {
    float beyond_corner = abs(along) - half_length;
    float tail = clamp(max(beyond_corner, 0.0) / max(extension, 1.0), 0.0, 1.0);
    float taper = pow(1.0 - tail, 1.4);
    // Fade along the entire extension, keeping the root bright and the tip transparent.
    float tip_opacity = pow(1.0 - smoothstep(0.0, 1.0, tail), 1.25);
    float aa = max(fwidth(across), 0.65);
    float line = 1.0 - smoothstep(line_width_px * taper,
        line_width_px * taper + aa, abs(across));

    // The wider ray starts at the corner and narrows to a point along the edge axis.
    float ray_radius = max(ray_width_px * taper, 0.15);
    float ray = exp(-2.0 * across * across / (ray_radius * ray_radius));
    ray *= smoothstep(-4.0, 1.0, beyond_corner) * taper;
    float halo = exp(-abs(across) / max(glow_radius_px, 1.0));
    halo *= exp(-max(beyond_corner, 0.0) / max(extension * 0.45, 1.0));
    return vec3(line, ray, halo) * tip_opacity;
}

float outward_wave(float distance_from_edge, float phase) {
    float envelope = smoothstep(0.0, 0.16, phase)
        * (1.0 - smoothstep(0.65, 1.0, phase));
    float front_distance = (distance_from_edge - phase * 88.0) / 9.0;
    return exp(-front_distance * front_distance) * envelope;
}

void fragment() {
    vec4 tint = COLOR;
    vec2 effect_size = card_size + effect_margin * 2.0;
    vec2 p = UV * effect_size - effect_margin - card_size * 0.5;
    vec2 half_size = max(card_size * 0.5, vec2(1.0));
    vec2 edge_delta = abs(p) - half_size;
    float outside_distance = max(max(edge_delta.x, edge_delta.y), 0.0);
    float outside_mask = smoothstep(-0.8, 0.8, max(edge_delta.x, edge_delta.y));
    float visible = smoothstep(0.0, 0.1, width);

    vec3 horizontal = edge_light(p.x, edge_delta.y, half_size.x, ray_length_px.x);
    vec3 vertical = edge_light(p.y, edge_delta.x, half_size.y, ray_length_px.y);
    float line = max(horizontal.x, vertical.x);
    float rays = horizontal.y + vertical.y;
    float edge_halo = max(horizontal.z, vertical.z);

    // Negative components lie along the card edge, so they must not reduce the fill.
    // This covers the FULL top/bottom/left/right strips, including their midpoints.
    vec2 fill_uv = max(edge_delta, vec2(0.0)) / max(fill_extent_px, vec2(1.0));
    float fill_gradient = clamp(length(fill_uv), 0.0, 1.0);
    float area_fill = pow(1.0 - smoothstep(0.0, 1.0, fill_gradient), 1.1);
    vec4 fill_color = mix(accent_color, fill_outer_color,
        smoothstep(0.0, 1.0, fill_gradient));

    // Two overlapping fronts travel outward from the card without flickering noise.
    float phase = fract(TIME * flow_speed);
    float wave = outward_wave(outside_distance, phase)
        + outward_wave(outside_distance, fract(phase + 0.5));
    float ray_flow = 0.82 + wave * 0.35;
    float pulse = 0.94 + 0.06 * sin(TIME * pulse_speed);

    float core_energy = line * 0.8 * core_color.a;
    float fill_energy = (area_fill * fill_strength * (0.94 + wave * 0.12)
        + edge_halo * 0.07) * fill_color.a;
    float ray_energy = (rays * 0.46 * ray_flow + edge_halo * 0.12) * spark_color.a;

    // A full rectangular backing, independent of the chamfered card silhouette.
    // Keep its opacity constant through the interior, including all four cut-off corners.
    vec2 inner_uv = abs(p) / half_size;
    float inner_gradient = pow(clamp(max(inner_uv.x, inner_uv.y), 0.0, 1.0), 1.5);
    vec4 inner_color = mix(inner_center_color, inner_fill_color, inner_gradient);
    float outer_energy = core_energy + fill_energy + ray_energy;
    vec3 outer_color = (core_color.rgb * core_energy + fill_color.rgb * fill_energy
        + spark_color.rgb * ray_energy) / max(outer_energy, 0.0001);
    float outer_alpha = clamp(outer_energy * intensity * pulse, 0.0, 1.0) * outside_mask;
    float inner_alpha = clamp(inner_fill_strength * inner_color.a * intensity, 0.0, 1.0)
        * (1.0 - outside_mask);
    float coverage = outer_alpha + inner_alpha;
    vec3 rgb = (outer_color * outer_alpha + inner_color.rgb * inner_alpha)
        / max(coverage, 0.0001);

    // Fade before the ColorRect boundary even when material lengths are enlarged.
    vec2 boundary_fade = vec2(1.0) - smoothstep(half_size + effect_margin - vec2(8.0),
        half_size + effect_margin, abs(p));
    float alpha = coverage * visible * boundary_fade.x * boundary_fade.y;
    COLOR = vec4(rgb, alpha) * tint;
}
