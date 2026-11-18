shader_type canvas_item;
render_mode unshaded;
uniform vec2 panel_size = vec2(400.0, 200.0);
// 1.0 is the resting pane, including in the scene editor.
uniform float deploy : hint_range(0.0, 1.0) = 1.0;
uniform vec2 port_uv = vec2(0.0, 0.88);
uniform float glass_opacity : hint_range(0.0, 1.0) = 0.62;
void fragment() {
    vec2 p = UV * panel_size;
    vec2 far_edge = panel_size - p;
    float d = min(min(p.x, far_edge.x), min(p.y, far_edge.y));
    d = min(d, min(far_edge.x + p.y - 11.0, p.x + far_edge.y - 7.0) * 0.707);
    float border = 1.0 - smoothstep(0.6, 1.7, d);
    float etch = 1.0 - smoothstep(0.0, 1.0, abs(mod(p.y, 6.0) - 3.0));
    float ticks = step(0.5, fract(p.x / 6.0)) * step(p.y, 4.0) * step(far_edge.x, 65.0);
    vec3 base = mix(vec3(0.056, 0.063, 0.075), vec3(0.105, 0.119, 0.136), 1.0 - UV.y);
    vec3 rgb = base + vec3(0.49, 0.56, 0.61) * (0.009 * etch + 0.55 * border + 0.47 * ticks);
    float opacity = mix(glass_opacity, 0.82, max(border, ticks));
    if (deploy < 1.0) {
        float x = abs(UV.x - port_uv.x);
        float rail = 1.0 - pow(1.0 - smoothstep(0.15, 0.47, deploy), 2.0);
        // Four folded leaves open off the rail in a short travelling cascade.
        // Each face is initially a narrow wedge, not a scaled copy of the final pane.
        float strip = min(floor(x * 4.0), 3.0);
        float hinge = fract(x * 4.0);
        float lag = 0.045 * strip + 0.060 * hinge;
        float aperture = smoothstep(0.29 + lag, 0.59 + lag, deploy);
        float dy = p.y - port_uv.y * panel_size.y;
        float reach = dy < 0.0 ? port_uv.y * panel_size.y : (1.0 - port_uv.y) * panel_size.y;
        float lip = mix(1.25, reach + 2.0, aperture) - abs(dy);
        float mask = smoothstep(-0.8, 0.8, lip) * smoothstep(-0.6, 0.6, (rail - x) * panel_size.x);
        float crease = (1.0 - smoothstep(0.0, 1.4, min(hinge, 1.0 - hinge) * panel_size.x * 0.25));
        float edge = (1.0 - smoothstep(0.0, 2.8, abs(lip))) + 0.50 * crease + 0.19 * hinge;
        float transient = (1.0 - smoothstep(0.75, 1.0, deploy));
        float beam = exp(-abs(dy) * 0.55) * (1.0 - smoothstep(0.39, 0.70, deploy));
        rgb += vec3(0.58, 0.70, 0.77) * (edge * transient + beam);
        opacity = mix(opacity, 0.93, clamp(edge * transient + beam, 0.0, 1.0)) * mask * smoothstep(0.13, 0.19, deploy);
    }
    COLOR *= vec4(rgb, opacity * smoothstep(-0.6, 0.6, d));
}
