shader_type canvas_item;

// Navigation reticle at the warp vanishing point: centre dot, closed inner
// ring, two counter-rotating broken rings (incommensurable speeds so the
// idle loop never visibly repeats), and four inward crosshair ticks that
// breathe. flare (driven by hovering the primary button) tightens and
// brightens the whole assembly.

uniform vec4 ring_color : source_color = vec4(0.949, 0.78, 0.29, 1.0);
uniform vec4 accent_color : source_color = vec4(0.345, 0.902, 1.0, 1.0);
uniform float rot_speed = 0.31;
uniform float flare : hint_range(0.0, 1.0) = 0.0;
uniform float base_alpha : hint_range(0.0, 1.0) = 0.75;

vec2 rot(vec2 v, float a) {
    float c = cos(a);
    float s = sin(a);
    return vec2(c * v.x - s * v.y, s * v.x + c * v.y);
}

void fragment() {
    vec2 uv = (UV - 0.5) / mix(1.0, 0.87, flare);
    float d = length(uv);
    float aa = 0.004;

    float dot_m = smoothstep(0.014, 0.006, d);
    float inner = smoothstep(aa, 0.0, abs(d - 0.10) - 0.0035);

    vec2 u1 = rot(uv, TIME * rot_speed);
    float g1 = step(0.15, cos(atan(u1.y, u1.x) * 3.0 + 0.7));
    float r1 = smoothstep(aa, 0.0, abs(d - 0.16) - 0.005) * g1;

    vec2 u2 = rot(uv, -TIME * rot_speed * 0.73);
    float g2 = step(0.35, cos(atan(u2.y, u2.x) * 4.0));
    float r2 = smoothstep(aa, 0.0, abs(d - 0.215) - 0.004) * g2;

    vec2 au = abs(uv);
    float axis = min(au.x, au.y);
    float rr = 0.30 + sin(TIME * 2.0) * 0.01;
    float ticks = smoothstep(0.004, 0.0, axis) * smoothstep(0.05, 0.02, abs(d - rr));

    vec3 col = ring_color.rgb * (dot_m + inner + r1 + ticks) + accent_color.rgb * r2 * 0.6;
    float alpha = clamp(dot_m + inner + r1 + r2 * 0.6 + ticks, 0.0, 1.0)
        * (base_alpha + flare * 0.45);
    COLOR = vec4(col, alpha);
}
