shader_type canvas_item;
render_mode unshaded;

// At rest this is exactly the original editor-visible hairline.
uniform float reveal : hint_range(0.0, 1.0) = 1.0;
void fragment() {
    float front = mix(-0.02, 1.06, reveal);
    float mask = 1.0 - smoothstep(front - 0.008, front, UV.x);
    float glint = smoothstep(front - 0.054, front - 0.010, UV.x) * (1.0 - smoothstep(0.90, 1.0, reveal));
    COLOR.rgb = mix(COLOR.rgb, vec3(0.85, 0.94, 0.97), glint);
    COLOR.a = mix(COLOR.a, 0.83, glint) * mask;
}
