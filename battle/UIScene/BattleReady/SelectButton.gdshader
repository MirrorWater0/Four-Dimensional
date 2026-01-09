shader_type canvas_item;

// --- Settings ---
group_uniforms Reveal_Settings;
uniform float progress : hint_range(0.0, 1.0) = 0.5;
uniform float smooth_size : hint_range(0.0, 1.0) = 0.15;

group_uniforms Magic_Effects;
// The noise texture giving the edge its shape
uniform sampler2D noise_tex : repeat_enable;
// How much the noise distorts the straight line
uniform float distortion_strength : hint_range(0.0, 0.5) = 0.1;
// Speed of the shimmering fog effect
uniform float time_scale : hint_range(0.0, 5.0) = 1.0;

group_uniforms Glow_Border;
// The color of the burning edge. Use HDR values (r,g,b > 1) for Glow/Bloom
uniform vec4 edge_color : source_color = vec4(1.5, 2.0, 4.0, 1.0); 
// How thick the glowing line is
uniform float edge_width : hint_range(0.0, 0.2) = 0.05;

void fragment() {
	// 1. Get the base sprite/UI color
	vec4 main_texture = texture(TEXTURE, UV);
	
	// 2. Sample the noise texture
	// We scroll the UVs by TIME to make the smoke/energy move continuously
	float noise_value = texture(noise_tex, UV + vec2(TIME * time_scale * 0.1, TIME * time_scale * 0.05)).r;
	
	// 3. Calculate the Reveal Mask
	// We combine the X coordinate (UV.x) with the noise.
	// We remap 'progress' so it can fully clear the noise distortion at 0.0 and 1.0
	float dist_x = UV.x + (noise_value - 0.5) * distortion_strength;
	float current_progress = (progress * (1.0 + smooth_size + edge_width)) - (smooth_size + edge_width) / 2.0;
	
	// Determine transparency (Alpha) based on progress
	float alpha = 1.0 - smoothstep(current_progress, current_progress + smooth_size, dist_x);
	
	// 4. Calculate the Glowing Edge
	// We create a narrow band where the transition happens
	float edge_intensity = smoothstep(current_progress - edge_width, current_progress, dist_x) * 
						   (1.0 - smoothstep(current_progress, current_progress + 0.01, dist_x));
	
	// 5. Combine everything
	vec4 final_color = main_texture;
	
	// Apply alpha to the main texture
	final_color.a *= alpha;
	
	// Add the glowing edge on top. 
	// We multiply by main_texture.a to ensure the glow doesn't appear outside the UI shape.
	vec4 glow = edge_color * edge_intensity * main_texture.a;
	
	// Additive blending for the glow (makes it look like light)
	COLOR = final_color + glow;
}
