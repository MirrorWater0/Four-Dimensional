shader_type canvas_item;

// --- Colors ---
uniform vec4 color_back : source_color = vec4(0.0, 0.85, 0.5, 1.0);   // Green
uniform vec4 color_mid : source_color = vec4(0.8, 0.95, 0.2, 1.0);    // Lime/Yellow
uniform vec4 color_front : source_color = vec4(1.0, 0.7, 0.1, 1.0);   // Orange

// --- Shape Settings ---
uniform vec2 card_size = vec2(0.25, 0.35); // Size of one card relative to UV
uniform float corner_radius = 0.02;        // Roundness of corners
uniform vec2 offset_step = vec2(0.12, -0.05); // How far apart the cards are spaced

// Function to draw a rounded box
// Returns 1.0 if inside the box, 0.0 if outside
float rounded_box(vec2 uv, vec2 pos, vec2 size, float radius) {
	vec2 d = abs(uv - pos) - size + radius;
	float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;
	
	// Smoothstep handles Anti-Aliasing (soft edges)
	// Change 0.005 to a lower number for sharper, harder edges
	return 1.0 - smoothstep(0.0, 0.01, dist);
}

void fragment() {
	// Center the UVs so (0,0) is in the middle of the rect
	vec2 uv = UV - 0.5;
	
	// Initialize final color (transparent)
	vec4 final_color = vec4(0.0);
	
	// --- Calculate Positions ---
	// We want 3 cards. Center them based on the offset so the group stays in the middle.
	vec2 pos_1 = -offset_step; // Back (Left/Down)
	vec2 pos_2 = vec2(0.0);    // Mid  (Center)
	vec2 pos_3 = offset_step;  // Front (Right/Up)

	// --- Draw Layers (Painter's Algorithm: Back to Front) ---
	
	// 1. Draw Back Card (Green)
	float box1 = rounded_box(uv, pos_1, card_size, corner_radius);
	final_color = mix(final_color, color_back, box1);
	
	// 2. Draw Middle Card (Lime)
	float box2 = rounded_box(uv, pos_2, card_size, corner_radius);
	final_color = mix(final_color, color_mid, box2);
	
	// 3. Draw Front Card (Orange)
	float box3 = rounded_box(uv, pos_3, card_size, corner_radius);
	final_color = mix(final_color, color_front, box3);

	// Apply to texture
	COLOR *= final_color;
}