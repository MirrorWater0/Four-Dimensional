shader_type canvas_item;

uniform vec4 color : source_color = vec4(0.5, 0.7, 1.0, 0.2);
uniform float speed = 1.0;
uniform int teeth = 8;
uniform float tooth_length = 0.1; // Relative to radius
uniform float radius_percent = 0.45; // 0.0 to 0.5
uniform float hole_percent = 0.2;

void fragment() {
    vec2 uv = UV - 0.5;
    float angle = atan(uv.y, uv.x);
    float dist = length(uv);
    
    // Rotate
    angle += TIME * speed;
    
    // Gear shape
    // r = radius + tooth_height * f(angle)
    // Square wave for teeth
    float t = cos(angle * float(teeth));
    // Smooth the teeth slightly
    float r = radius_percent + smoothstep(0.4, 0.6, t) * tooth_length;
    
    float gear = 1.0 - smoothstep(r, r + 0.01, dist);
    float hole = smoothstep(hole_percent, hole_percent + 0.01, dist);
    
    float shape = gear * hole;
    
    // Optional: Inner detail ring
    float ring_r = radius_percent * 0.7;
    float ring_w = 0.02;
    float ring = (1.0 - smoothstep(ring_r + ring_w, ring_r + ring_w + 0.005, dist)) * 
                 smoothstep(ring_r, ring_r + 0.005, dist);
    
    COLOR *= color * max(shape, ring);
}
