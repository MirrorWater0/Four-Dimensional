shader_type canvas_item;
render_mode unshaded;
uniform float emphasis : hint_range(0.0, 1.0) = 0.0;
float grain_hash(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}
void fragment() {
    vec2 uv = SCREEN_UV;
    float horizontal = smoothstep(0.0, 1.0, uv.x);
    float vertical = smoothstep(0.0, 1.0, uv.y);

    // Broad tonal fields leave the animated crystal as the only visual subject.
    vec3 upper = mix(vec3(0.095, 0.105, 0.12), vec3(0.32, 0.31, 0.325), horizontal);
    vec3 lower = mix(vec3(0.17, 0.19, 0.20), vec3(0.19, 0.205, 0.22), horizontal);
    vec3 color = mix(upper, lower, vertical);
    float diagonal = uv.x * 0.68 + (1.0 - uv.y) * 0.46;
    float pearl = smoothstep(0.28, 0.77, diagonal) * (1.0 - smoothstep(0.82, 1.18, diagonal));
    color += vec3(0.08, 0.085, 0.09) * pearl;
    float lower_light = smoothstep(0.48, 1.15, uv.y) * (1.0 - smoothstep(0.12, 0.85, uv.x));
    color += vec3(0.028, 0.033, 0.034) * lower_light;
    color += vec3(0.015, 0.017, 0.018) * emphasis;

    // Static subpixel grain prevents visible bands without adding moving texture.
    color += (grain_hash(floor(FRAGCOORD.xy)) - 0.5) / 255.0;
    COLOR *= vec4(color, 1.0);
}
