shader_type canvas_item;

uniform vec4 bg_color_top : source_color = vec4(0.08, 0.09, 0.16, 1.0);
uniform vec4 bg_color_bottom : source_color = vec4(0.02, 0.02, 0.05, 1.0);
uniform vec4 triangle_color : source_color = vec4(0.3, 0.4, 0.6, 0.3);
uniform float triangle_size_near : hint_range(0.02, 0.8) = 0.26;
uniform float triangle_size_far : hint_range(0.02, 0.8) = 0.10;
uniform float triangle_flatten_min : hint_range(0.2, 1.0) = 0.52;
uniform float triangle_flatten_max : hint_range(0.6, 1.8) = 1.18;
uniform float triangle_drift : hint_range(0.0, 0.5) = 0.14;
uniform float triangle_perspective : hint_range(0.0, 0.8) = 0.18;
uniform float edge_softness : hint_range(0.001, 0.03) = 0.006;

// 2D Rotation
mat2 rot(float a) {
    float c = cos(a);
    float s = sin(a);
    return mat2(vec2(c, -s), vec2(s, c));
}

// Equilateral Triangle SDF (dist < 0 inside)
float tri_sdf(vec2 p, float r) {
    const float k = sqrt(3.0);
    p.x = abs(p.x) - r;
    p.y = p.y + r/k;
    if( p.x+k*p.y>0.0 ) p = vec2(p.x-k*p.y,-k*p.x-p.y)/2.0;
    p.x -= clamp( p.x, -2.0*r, 0.0 );
    return -length(p)*sign(p.y);
}

float random(float seed) {
    return fract(sin(seed) * 43758.5453);
}

void fragment() {
    vec2 uv = UV;
    // Aspect ratio correction approximation
    vec2 aspect_uv = (uv - 0.5) * vec2(1.77, 1.0);
    
    vec4 color = mix(bg_color_top, bg_color_bottom, uv.y);
    
    // Floating Shards
    for (float i = 0.0; i < 8.0; i++) {
        float r1 = random(i + 1.123);
        float r2 = random(i + 2.234);
        float r3 = random(i + 3.345);
        
        // Initial Position & Movement
        float speed = 0.05 + r3 * 0.08;
        vec2 dir = vec2(cos(r1 * 6.28), sin(r1 * 6.28));
        
        // Calculate position with wrap-around
        vec2 pos = vec2(0.0);
        pos.x = mod(r1 * 3.0 + dir.x * TIME * speed, 3.5) - 1.75; // Range [-1.75, 1.75]
        pos.y = mod(r2 * 2.0 + dir.y * TIME * speed, 2.5) - 1.25; // Range [-1.25, 1.25]

        // A slow depth phase drives size / flattening like drifting in 3D space.
        float depth_phase = 0.5 + 0.5 * sin(TIME * (0.25 + r2 * 0.35) + r1 * 6.28);
        float drift_strength = mix(0.45, 1.0, depth_phase);
        pos += vec2(
            sin(TIME * (0.32 + r1 * 0.24) + r2 * 6.28),
            cos(TIME * (0.28 + r3 * 0.20) + r1 * 6.28)
        ) * triangle_drift * drift_strength;
        
        vec2 p = aspect_uv - pos;
        
        // Rotation
        float rot_speed = (r2 - 0.5) * 0.4;
        p = rot(TIME * rot_speed + r3 * 6.28 + depth_phase * 0.9) * p;

        // Width variation.
        p.x *= 0.72 + r1 * 0.95;

        // Flatten / expand vertically to fake a shard pitching in 3D.
        float flatten = mix(triangle_flatten_min, triangle_flatten_max, depth_phase);
        p.y /= max(flatten, 0.001);

        // Small skew makes the triangle feel less planar while drifting.
        float skew = (r2 - 0.5) * triangle_perspective * (depth_phase * 2.0 - 1.0);
        p.x += p.y * skew;

        // Size shifts with depth phase, so shards look near/far over time.
        float size = mix(triangle_size_far, triangle_size_near, depth_phase) * (0.85 + r2 * 0.75);
        
        float d = tri_sdf(p, size);
        
        // Render
        // Soft edges
        float alpha = smoothstep(edge_softness, -edge_softness, d);
        
        // Varying opacity and color tint
        float opacity = mix(0.06, 0.24, depth_phase) * (0.7 + r3 * 0.6);
        vec4 shard_col = triangle_color;

        // Near shards are a bit brighter.
        shard_col.rgb += depth_phase * 0.08;

        // Make some shards slightly brighter/whiter
        if (r1 > 0.7) {
            shard_col += vec4(0.1, 0.1, 0.1, 0.0);
        }
        
        color = mix(color, shard_col, alpha * opacity);
    }
    
    COLOR *= color;
}
