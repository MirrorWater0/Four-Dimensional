// Shared by the illustrated and skinned bodies. Independent of skill teleports.
uniform float arrival_phase : hint_range(0.0, 1.0) = 1.0;
uniform vec4 arrival_ink : source_color = vec4(0.52, 0.78, 1.0, 1.0);

vec4 arrival_body(vec4 body, float height, float phase) {
    if (phase >= 0.70) { return body; }

    // Height is measured up from the feet. Accelerate the reveal towards impact.
    // Keep the sweep and impact beats aligned with CharacterArrival.gdshader.
    float sweep = pow(clamp((phase - 0.18) / 0.27, 0.0, 1.0), 1.45);
    float behind = clamp(height, 0.0, 1.0) - mix(1.10, -0.10, sweep);
    float revealed = smoothstep(-0.026, 0.037, behind);
    float rim = 1.0 - smoothstep(0.012, 0.077, abs(behind));
    float hit = smoothstep(0.43, 0.45, phase) * (1.0 - smoothstep(0.45, 0.57, phase));
    float restore = smoothstep(0.24, 0.64, phase);
    vec3 silhouette = mix(body.rgb * 0.30 + arrival_ink.rgb * 0.20, body.rgb, restore);
    vec3 hot = mix(arrival_ink.rgb, vec3(1.0, 0.98, 0.94), 0.76);
    float light = max(rim, hit * mix(0.63, 0.24, clamp(height, 0.0, 1.0)));
    return vec4(mix(silhouette, hot, light), body.a * revealed);
}
