shader_type canvas_item;

uniform vec4 color_left_top : source_color = vec4(0.25, 0.25, 0.28, 1.0); // Gunmetal Grey
uniform vec4 color_left_bot : source_color = vec4(0.1, 0.1, 0.12, 1.0);   // Dark Grey
uniform vec4 color_right_top : source_color = vec4(0.05, 0.05, 0.05, 1.0); // Nearly Black
uniform vec4 color_right_bot : source_color = vec4(0.0, 0.0, 0.0, 1.0);    // Black
uniform vec4 border_color : source_color = vec4(0.6, 0.6, 0.65, 1.0);      // Light Grey/Silver
uniform vec4 line_color : source_color = vec4(0.8, 0.8, 0.9, 0.15);        // Faint White

uniform float slant : hint_range(0.0, 1.0) = 0.35;
uniform float split_pos : hint_range(0.0, 1.0) = 0.35;

void fragment() {
    vec2 uv = UV;
    
    // Diagonal Split
    // Equation: x = split_pos + (y - 0.5) * slant
    float split_x = split_pos + (uv.y - 0.5) * slant;
    float is_right = step(split_x, uv.x);
    
    // Background Colors
    vec4 bg_left = mix(color_left_top, color_left_bot, uv.y + (uv.x * 0.5)); // Diagonal gradient
    vec4 bg_right = mix(color_right_top, color_right_bot, uv.y);
    
    vec4 color = mix(bg_left, bg_right, is_right);
    
    // Geometric Patterns
    // 1. Tech Stripes (Modern geometric feel) on the left
    float stripes = step(0.92, fract((uv.x - uv.y * 0.5) * 20.0)); 
    // Fade stripes near edges
    float stripe_mask = smoothstep(0.0, 0.2, uv.x) * smoothstep(split_x, split_x - 0.1, uv.x);
    color = mix(color, color + vec4(0.1, 0.1, 0.15, 0.0), stripes * stripe_mask * 0.5);
    
    // 2. Horizontal Line through middle (Modern line)
    float h_line = 1.0 - step(0.002, abs(uv.y - 0.5));
    // Mask horizontal line to not cross the split abruptly
    color = mix(color, line_color, h_line * 0.5);
    
    // 3. Diagonal Split Line Highlight
    float split_dist = abs(uv.x - split_x);
    float split_line = smoothstep(0.004, 0.0, split_dist);
    color = mix(color, border_color, split_line);
    
    // 4. Secondary Geometric Shape (Small rectangle or accent)
    // Draw a small technical decorative rect near bottom left
    float dec_rect = step(0.05, uv.x) * step(uv.x, 0.15) * step(0.75, uv.y) * step(uv.y, 0.8);
    color = mix(color, line_color * 1.5, dec_rect * 0.5);

    // 5. Border
    float border_width = 0.015;
    vec2 border = step(vec2(border_width), uv) * step(vec2(border_width), 1.0 - uv);
    float is_border = 1.0 - (border.x * border.y);
    
    // Corner cuts for border (Technical look)
    float corner_cut = step(uv.x + uv.y, 0.06) + step((1.0-uv.x) + (1.0-uv.y), 0.06);
    if (corner_cut > 0.5) {
        discard;
    }

    // Border color
    color = mix(color, border_color, is_border);
    
    COLOR = color;
}
