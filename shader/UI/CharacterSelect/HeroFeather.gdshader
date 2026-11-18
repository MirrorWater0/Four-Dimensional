shader_type canvas_item;

// The illustration is a printed plate: keep the pale paper and the face intact.
// Only its inner edge dissolves into the ink-coloured column carrying the copy.
uniform float reveal : hint_range(0.0, 1.0) = 1.0;
void fragment() {
    float edge = UV.x + (UV.y - 0.5) * 0.11;
    float ink = smoothstep(0.16, 0.55, edge);
    vec2 sample_uv = (UV - vec2(0.5, 0.23)) / mix(1.045, 1.0, reveal) + vec2(0.5, 0.23);
    vec4 art = texture(TEXTURE, sample_uv);
    float pass = smoothstep(-0.20, 0.17, 1.30 * reveal - UV.x);
    COLOR = vec4(art.rgb * mix(0.88, 1.0, pass), ink * art.a * mix(0.40, 1.0, pass) * COLOR.a);
}
