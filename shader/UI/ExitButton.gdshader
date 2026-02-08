shader_type canvas_item;

uniform vec4 icon_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// Segment distance function
float sdSegment(in vec2 p, in vec2 a, in vec2 b) {
    vec2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

void fragment() {
    vec2 uv = UV - 0.5;
    
    // Config
    float thickness = 0.05;
    float door_w = 0.15; // Half width of door box
    float door_h = 0.25; // Half height
    
    // Door Shape (Right Bracket ']')
    // We construct it from a hollow box with the left side removed
    float d_box_outer = sdBox(uv - vec2(0.05, 0.0), vec2(door_w + thickness, door_h + thickness));
    float d_box_inner = sdBox(uv - vec2(0.05, 0.0), vec2(door_w, door_h));
    
    // Frame is Outer - Inner
    float frame = max(d_box_outer, -d_box_inner);
    
    // Cut the left side
    // We define a box that covers the left wall and subtract it
    // Left wall is at x = 0.05 - door_w - thickness ~ -0.1 - 0.05 = -0.15
    float d_cut = sdBox(uv - vec2(-0.15, 0.0), vec2(thickness * 2.0, door_h + thickness * 2.0));
    
    float door = max(frame, -d_cut);
    
    // Arrow Shape (Pointing Left)
    // Shaft
    float d_shaft = sdBox(uv - vec2(0.05, 0.0), vec2(0.12, 0.04));
    
    // Triangle Head (Pointing Left)
    // Center of triangle head at x = -0.15 approx
    vec2 tp = uv - vec2(-0.25, 0.0); // Translate so tip is at roughly (0,0) relative to tp? 
    // Let's define triangle vertices explicitly
    vec2 v1 = vec2(-0.25, 0.0);   // Tip
    vec2 v2 = vec2(-0.05, 0.15);  // Top Right
    vec2 v3 = vec2(-0.05, -0.15); // Bot Right
    
    // Signed distance to triangle
    // Simplified: Just 3 segments? No, need filled.
    // Let's use 3 edge planes.
    // Edge 1: v1-v2
    // Edge 2: v2-v3
    // Edge 3: v3-v1
    
    // Use segment distance for outline, but we want filled.
    // Math for filled triangle SDF is complex to write from scratch quickly without errors.
    // Approximation: Intersection of 3 half-planes.
    // Edge 1 normal (v1->v2 is (0.2, 0.15)). Normal (-0.15, 0.2).
    // ...
    
    // Let's use sdTriangle isosceles logic
    // Rotate uv 90 deg to point up?
    // p pointing left (-x).
    vec2 q = uv;
    q.x = -q.x; // Point right
    q -= vec2(0.25, 0.0); // Move to tip position (tip was at -0.25, now at 0.25)
    // Actually simpler:
    // Tip at -0.25. Base at -0.05.
    // Vector p from tip.
    vec2 p_tri = uv - vec2(-0.25, 0.0);
    // x goes from 0 to 0.2.
    // y goes from 0 to x * slope.
    float slope = 0.15 / 0.2;
    
    float d_tri = 0.0;
    if (p_tri.x >= 0.0 && p_tri.x <= 0.2 && abs(p_tri.y) <= p_tri.x * (0.15/0.2)) {
         d_tri = -1.0; // Inside
    } else {
         // Dist to segments
         float s1 = sdSegment(uv, v1, v2);
         float s2 = sdSegment(uv, v2, v3);
         float s3 = sdSegment(uv, v3, v1);
         d_tri = min(min(s1, s2), s3);
    }
    
    // Combine Arrow parts
    float arrow = min(d_shaft, d_tri);
    
    // Combine All
    float dist = min(door, arrow);
    
    // Render
    float alpha = 1.0 - smoothstep(0.0, 0.015, dist);
    
    COLOR = icon_color;
    COLOR.a *= alpha;
}
