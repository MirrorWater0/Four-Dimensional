shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float strength = 0.0;
varying vec4 sprite_tint;

// Leave room for the halo even on icons whose silhouette touches the texture edge.
void vertex() {
    VERTEX *= 1.12;
    UV = (UV - 0.5) * 1.12 + 0.5;
    sprite_tint = COLOR;
}

float mask_at(sampler2D icon, vec2 uv) {
    if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0)))) {
        return 0.0;
    }
    return texture(icon, uv).a;
}

void fragment() {
    float body = mask_at(TEXTURE, UV);
    vec2 d = vec2(0.032, 0.0);
    float halo = 0.25 * (mask_at(TEXTURE, UV + d) + mask_at(TEXTURE, UV - d)
        + mask_at(TEXTURE, UV + d.yx) + mask_at(TEXTURE, UV - d.yx));
    float rim = abs(body - halo);
    vec3 ink = texture(TEXTURE, clamp(UV, vec2(0.0), vec2(1.0))).rgb;
    vec3 light = mix(ink, vec3(0.85, 0.94, 1.0), 0.64);
    COLOR = vec4(light, strength * (0.17 * body + 0.26 * halo + 0.65 * rim)) * sprite_tint;
}
