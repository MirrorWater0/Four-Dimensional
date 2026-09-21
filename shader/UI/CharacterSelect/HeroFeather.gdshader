shader_type canvas_item;

// Broad, seamless organic falloff to eliminate any rectangular window or harsh edges.
uniform float softness : hint_range(0.1, 0.9) = 0.55;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    
    // Normalized distance from center (0.0 at center, 1.0 at border)
    vec2 d = abs(UV - vec2(0.5, 0.5)) * 2.0; // 0 at center, 1 at edge
    
    // Standard GLSL ascending edges: edge0 < edge1
    float edge0 = 1.0 - softness;
    float edge1 = 1.0;
    float fx = 1.0 - smoothstep(edge0, edge1, d.x);
    float fy = 1.0 - smoothstep(edge0, edge1, d.y);
    
    // Radial corner softening with ascending edges
    float corner = 1.0 - smoothstep(0.85, 1.25, length(d));
    
    float alpha_mask = fx * fy * corner;
    
    COLOR = vec4(tex.rgb, tex.a * alpha_mask * COLOR.a);
}
