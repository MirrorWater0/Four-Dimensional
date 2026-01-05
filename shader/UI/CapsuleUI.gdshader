shader_type canvas_item;

// --- Colors ---
uniform vec4 bg_color : source_color = vec4(0.95, 0.95, 0.95, 1.0);
uniform vec4 outer_rim_color : source_color = vec4(0.95, 0.95, 0.95, 1.0);
uniform vec4 inner_rim_color : source_color = vec4(0.5, 0.5, 0.5, 1.0);

// --- Selection Mask Settings ---
uniform vec4 selected_color : source_color = vec4(1.0, 0.2, 0.4, 1.0);
uniform float selected_fill : hint_range(0.0, 1.0) = 1.0;     // 1.0 = Whole Round
uniform float selected_rotation : hint_range(0.0, 1.0) = 0.0; // Where the highlight starts

// --- INDEPENDENT THICKNESS CONTROLS ---
uniform float outer_thickness : hint_range(0.001, 0.1) = 0.015; // Controls the main border
uniform float inner_thickness : hint_range(0.001, 0.1) = 0.01;  // Controls the groove line
uniform float inner_line_gap : hint_range(0.01, 0.1) = 0.045;  // Distance from edge

// --- Precision ---
const float ASPECT = 2.4; // Fixed for 60x25
uniform float antialiasing : hint_range(0.001, 0.05) = 0.01;

void fragment() {
    // 1. Coordinate setup
    vec2 p = (UV - 0.5) * vec2(ASPECT, 1.0);
    
    // 2. Capsule Geometry
    float radius = 0.48;
    float half_width = (ASPECT * 0.5) - radius;
    float d = length(p - vec2(clamp(p.x, -half_width, half_width), 0.0)) - radius;
    
    // 3. Perimeter Mask logic (The "Whole Round" selection)
    float angle = atan(p.y, p.x); 
    float perimeter_pos = (angle + 3.14159) / 6.28318;
    perimeter_pos = fract(perimeter_pos + selected_rotation);
    float selection_mask = step(perimeter_pos, selected_fill);

    // 4. INDEPENDENT LINE MASKS
    // Outer Line uses outer_thickness
    float outer_line = smoothstep(outer_thickness + antialiasing, outer_thickness, abs(d));
    
    // Inner Line uses inner_thickness and its own gap
    float inner_d = d + inner_line_gap;
    float inner_line = smoothstep(inner_thickness + antialiasing, inner_thickness, abs(inner_d));

    // 5. Apply Selection to ONLY the Outer Rim
    vec3 active_outer_rim = mix(outer_rim_color.rgb, selected_color.rgb, selection_mask);

    // 6. Final Composition
    vec3 color = bg_color.rgb;
    
    // Apply Inner Rim (Static and independent thickness)
    color = mix(color, inner_rim_color.rgb, inner_line);
    
    // Apply Outer Rim (With selection and independent thickness)
    color = mix(color, active_outer_rim, outer_line);

    // 7. Alpha Clipping
    float alpha = smoothstep(antialiasing, 0.0, d);
    COLOR = vec4(color, alpha);
}