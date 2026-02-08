shader_type canvas_item;

uniform vec4 bg_color_top : source_color = vec4(0.08, 0.09, 0.16, 1.0);
uniform vec4 bg_color_bottom : source_color = vec4(0.02, 0.02, 0.05, 1.0);
uniform vec4 triangle_color : source_color = vec4(0.3, 0.4, 0.6, 0.3);

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
        // Use time to drive position linearly
        float speed = 0.05 + r3 * 0.08;
        vec2 dir = vec2(cos(r1 * 6.28), sin(r1 * 6.28));
        
        // Calculate position with wrap-around
        vec2 pos = vec2(0.0);
        pos.x = mod(r1 * 3.0 + dir.x * TIME * speed, 3.5) - 1.75; // Range [-1.75, 1.75]
        pos.y = mod(r2 * 2.0 + dir.y * TIME * speed, 2.5) - 1.25; // Range [-1.25, 1.25]
        
        vec2 p = aspect_uv - pos;
        
        // Rotation
        float rot_speed = (r2 - 0.5) * 0.4;
        p = rot(TIME * rot_speed + r3 * 6.28) * p;
        
        // Distortion for "Shard" look (stretch one axis)
        p *= vec2(0.6 + r1 * 0.8, 1.0); 
        
        // Size
        float size = 0.15 + r2 * 0.35; // Large shards
        
        float d = tri_sdf(p, size);
        
        // Render
        // Soft edges
        float alpha = smoothstep(0.005, -0.005, d);
        
        // Varying opacity and color tint
        float opacity = 0.1 + r3 * 0.2;
        vec4 shard_col = triangle_color;
        // Make some shards slightly brighter/whiter
        if (r1 > 0.7) {
            shard_col += vec4(0.1, 0.1, 0.1, 0.0);
        }
        
        color = mix(color, shard_col, alpha * opacity);
    }
    
    COLOR = color;
}
