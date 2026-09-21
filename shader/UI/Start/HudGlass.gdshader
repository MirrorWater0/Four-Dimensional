shader_type canvas_item;

// Paradox/Stellaris-style console panel:
//   * four chamfered corners matching the skill-card silhouette
//   * 1px outer steel border + a recessed 1px inner rule
//   * short accent tick marks at each corner
//   * vertical gradient fill with faint scanlines
//   * `boot` uniform drives the power-on reveal (border draws around, then fill)

uniform vec2 panel_size = vec2(560.0, 420.0);
uniform float notch = 14.0;
uniform vec4 fill_top : source_color = vec4(0.043, 0.063, 0.086, 0.94);
uniform vec4 fill_bottom : source_color = vec4(0.031, 0.047, 0.071, 0.91);
uniform vec4 border_color : source_color = vec4(0.235, 0.322, 0.404, 1.0);
uniform vec4 tick_color : source_color = vec4(0.29, 0.529, 0.506, 0.55);
uniform float border_width = 1.0;
uniform float inner_inset = 4.0;
uniform float inner_alpha = 0.5;
uniform float tick_length = 22.0;
uniform float scan_intensity = 0.025;
uniform float scan_period = 3.0;
uniform float boot : hint_range(0.0, 1.0) = 1.0;

// Signed distance to the panel silhouette with four 45-degree chamfers.
float panel_sd(vec2 p, vec2 hs, float n) {
    vec2 q = abs(p) - hs;
    float box = max(q.x, q.y);
    float cut = (abs(p.x) + abs(p.y) - (hs.x + hs.y - n)) * 0.7071;
    return max(box, cut);
}

void fragment() {
    vec4 tint = COLOR;
    vec2 hs = panel_size * 0.5;
    vec2 p = (UV - 0.5) * panel_size;
    float sd = panel_sd(p, hs, notch);
    float sd_inner = panel_sd(p, hs - vec2(inner_inset), max(notch - inner_inset, 0.0));

    // --- fill -------------------------------------------------------------
    vec4 fill = mix(fill_bottom, fill_top, 1.0 - UV.y);
    fill.a *= 1.0 + sin(p.y * scan_period) * scan_intensity;
    fill.a *= smoothstep(0.5, 1.0, boot);
    vec4 col = fill * smoothstep(0.0, -1.5, sd);

    // --- borders ----------------------------------------------------------
    // Sweep the outline in as `boot` rises, starting from the top edge.
    float ang = fract(atan(p.y, p.x) / TAU + 0.75);
    float reveal = smoothstep(ang - 0.03, ang + 0.03, boot * 1.06);

    float outer = smoothstep(border_width, border_width - 1.0, abs(sd));
    float inner = smoothstep(border_width, border_width - 1.0, abs(sd_inner));
    float rule = outer + inner * inner_alpha;
    col.rgb = mix(col.rgb, border_color.rgb, clamp(rule * reveal * border_color.a, 0.0, 1.0));
    col.a = max(col.a, rule * reveal * border_color.a);

    // --- corner ticks -----------------------------------------------------
    // Bright stubs that run a short way along both edges at every corner.
    vec2 d = hs - abs(p);
    float on_edge = smoothstep(border_width + 0.5, border_width - 0.5, min(d.x, d.y));
    float near_corner = step(d.x, tick_length) * step(d.y, tick_length);
    float tick = on_edge * near_corner * step(0.0, -sd) * reveal;
    col.rgb = mix(col.rgb, tick_color.rgb, tick * tick_color.a);
    col.a = max(col.a, tick * tick_color.a);

    COLOR = col * tint;
}
