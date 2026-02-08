shader_type canvas_item;

uniform vec4 main_color : source_color = vec4(0.1, 0.12, 0.15, 1.0); // Dark Blue-Grey Background
uniform vec4 stripe_color_1 : source_color = vec4(0.95, 0.75, 0.05, 1.0); // Caution Yellow
uniform vec4 stripe_color_2 : source_color = vec4(0.1, 0.1, 0.1, 1.0);   // Black
uniform float stripe_speed : hint_range(-2.0, 2.0) = 0.5;
uniform float stripe_density : hint_range(1.0, 50.0) = 20.0;
uniform float border_width : hint_range(0.0, 0.5) = 0.1;

float pixel_step(float v, float n) {
    return floor(v * n) / n;
}

void fragment() {
    vec2 uv = UV;
    
    // Pixelate UV slightly for "chunky" feel?
    // vec2 pixel_uv = floor(uv * 100.0) / 100.0; 
    
    // Hazard Stripes Pattern
    // x + y diagonal
    float stripe_val = (uv.x + uv.y * 0.5) * stripe_density - TIME * stripe_speed;
    float stripe_pattern = step(0.5, fract(stripe_val));
    vec4 border_pattern = mix(stripe_color_1, stripe_color_2, stripe_pattern);
    
    // Frame Logic
    // Box SDF
    vec2 d = abs(uv - 0.5) * 2.0; // 0..1 from center
    // Rectangular mask for border
    // border_width is roughly fraction of size
    // Adjust logic to have fixed width border visually
    
    vec2 border_mask = step(vec2(1.0 - border_width), d);
    float is_border = max(border_mask.x, border_mask.y);
    
    // Inner Background
    vec4 bg = main_color;
    
    // Scanline effect on background
    float scanline = step(0.5, fract(uv.y * 50.0));
    bg = mix(bg, bg * 0.8, scanline * 0.3);
    
    // Corner "Brackets" (ITB style accents)
    // Extra thick corners
    float corner_size = 0.2;
    vec2 corner_d = step(vec2(1.0 - corner_size), d);
    float is_corner = corner_d.x * corner_d.y;
    
    // Combine
    vec4 final_color = mix(bg, border_pattern, is_border);
    
    // Add glowing bracket corners?
    // Let's just make the border solid stripes.
    
    // Industrial Bezel (Inner shadow)
    if (is_border < 0.5) {
        // Inner shadow
        float shadow = smoothstep(1.0 - border_width - 0.05, 1.0 - border_width, max(d.x, d.y));
        final_color = mix(final_color, vec4(0.0,0.0,0.0,1.0), shadow * 0.5);
    }
    
    COLOR = final_color;
}
