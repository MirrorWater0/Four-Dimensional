shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float size : hint_range(0.1, 1.0) = 0.5;
uniform float thickness : hint_range(0.005, 0.2) = 0.02;
uniform float smoothing : hint_range(0.001, 0.1) = 0.03;
uniform float rotation = 1.5707963;

vec2 rotate_uv(vec2 p, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float sd_regular_polygon(vec2 p, float radius, float sides) {
    float angle = atan(p.y, p.x);
    float sector = 6.28318530718 / sides;
    return cos(floor(0.5 + angle / sector) * sector - angle) * length(p) - radius;
}

void fragment() {
    vec2 uv = rotate_uv(UV - 0.5, rotation);
    float dist = sd_regular_polygon(uv, size * 0.5, 6.0);
    float hollow_dist = abs(dist) - thickness;
    float alpha = smoothstep(smoothing, 0.0, hollow_dist);
    COLOR *= vec4(line_color.rgb, line_color.a * alpha);
}
