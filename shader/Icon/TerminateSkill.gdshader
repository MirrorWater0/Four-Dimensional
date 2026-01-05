shader_type canvas_item;

uniform vec4 symbol_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float thickness : hint_range(0.005, 0.1) = 0.015;

// MAIN PETAL CONTROLS (The 6 direct ones)
uniform float main_length : hint_range(0.1, 2.5) = 1.4;
uniform float main_width : hint_range(0.1, 2.0) = 1.1;

// SMALL PETAL CONTROLS (The 6 in-between ones)
uniform float small_length : hint_range(0.1, 2.0) = 0.5;
uniform float small_width : hint_range(0.1, 2.0) = 0.7;

uniform float petal_overall_size : hint_range(0.05, 0.5) = 0.18;
uniform float petal_pinch : hint_range(0.2, 1.0) = 0.5;
uniform float ring_radius : hint_range(0.3, 0.9) = 0.72;
uniform float glow_intensity : hint_range(0.0, 2.0) = 0.6;

// Helper to create the "scooped" concave shape
float sdPinchedStar(vec2 p, float size, float pinch) {
	p = abs(p);
	return pow(p.x, pinch) + pow(p.y, pinch) - size;
}

void fragment() {
	vec4 modualate = COLOR;
	vec2 uv = (UV - 0.5) * 2.5; // Increased scale to fit longer petals
	float d = 10.0; 

	// 1. THE CONNECTING CIRCLE (The Ring)
	float ring = abs(length(uv) - ring_radius) - (thickness * 0.5);
	d = min(d, ring);

	// 2. THE CENTRAL DIAMOND WINDOW
	float center_star = sdPinchedStar(uv, 0.8, 0.45);
	float center_hole = (abs(uv.x) + abs(uv.y)) - 0.18;
	float center_final = max(center_star, -center_hole);
	d = min(d, center_final);

	// 3. THE 12 PETALS (Alternating 6 Main / 6 Small)
	for (int i = 0; i < 12; i++) {
		float angle = float(i) * (TAU / 12.0);
		float c = cos(-angle);
		float s = sin(-angle);
		vec2 p_rot = vec2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
		
		p_rot.x -= ring_radius;
		
		// Logic: If i is even (0, 2, 4, 6, 8, 10), it's a MAIN petal
		bool is_main = (i % 2 == 0);
		
		float current_len = is_main ? main_length : small_length;
		float current_wid = is_main ? main_width : small_width;
		
		// Apply specific scaling for this petal type
		vec2 p_scaled = vec2(p_rot.x / current_len, p_rot.y / (current_wid * 0.5));
		
		float petal = sdPinchedStar(p_scaled, petal_overall_size, petal_pinch);
		
		d = min(d, petal);
	}

	// 4. RENDERING
	float mask = smoothstep(0.02, 0.0, d);
	float glow = exp(-max(0.0, d) * 25.0) * glow_intensity;
	
	vec3 final_rgb = symbol_color.rgb + (mask * 0.2); 
	float alpha = clamp(mask + glow, 0.0, 1.0);

	COLOR = vec4(final_rgb, alpha)*modualate;
}