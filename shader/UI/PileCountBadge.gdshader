shader_type canvas_item;

uniform vec4 fill_color : source_color = vec4(0.025, 0.055, 0.10, 0.96);
uniform vec4 border_color : source_color = vec4(0.55, 0.90, 1.0, 0.95);
uniform float border_width : hint_range(0.01, 0.2) = 0.065;

void fragment()
{
    vec2 point = UV - vec2(0.5);
    float distance_to_center = length(point);
    float antialias_width = max(fwidth(distance_to_center) * 1.25, 0.0015);
    float outer_mask = 1.0 - smoothstep(0.5 - antialias_width, 0.5 + antialias_width, distance_to_center);
    float inner_radius = 0.5 - border_width;
    float inner_mask = 1.0 - smoothstep(inner_radius - antialias_width, inner_radius + antialias_width, distance_to_center);
    vec4 badge_color = mix(border_color, fill_color, inner_mask);
    COLOR = vec4(badge_color.rgb, badge_color.a * outer_mask);
}