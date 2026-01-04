shader_type canvas_item;
render_mode blend_add;

// Explosion parameters
uniform vec2 explosion_center = vec2(0.5, 0.5);
uniform float explosion_progress : hint_range(0, 1, 0.01) = 0.0;
uniform float explosion_size : hint_range(0.1, 3, 0.01) = 1.0;

// Core parameters
uniform float core_size : hint_range(0, 1, 0.01) = 0.15;
uniform float core_pulse_speed : hint_range(0, 20, 0.1) = 5.0;

// Shockwave ring parameters
uniform int num_rings : hint_range(1, 10, 1) = 3;
uniform float ring_spacing : hint_range(0.05, 0.5, 0.01) = 0.15;
uniform float ring_width : hint_range(0.01, 0.2, 0.01) = 0.03;

// Particle parameters
uniform float particle_density : hint_range(10, 100, 1) = 50.0;
uniform float particle_size : hint_range(0.005, 0.05, 0.001) = 0.01;
uniform float particle_speed : hint_range(0.5, 5, 0.1) = 2.0;

// Noise parameters
uniform float noise_scale : hint_range(10, 100, 1) = 30.0;
uniform float noise_strength : hint_range(0, 1, 0.01) = 0.5;

// Color parameters
uniform vec4 core_color : source_color = vec4(1.0, 1.0, 0.9, 1.0);
uniform vec4 inner_color : source_color = vec4(1.0, 0.8, 0.3, 1.0);
uniform vec4 outer_color : source_color = vec4(1.0, 0.3, 0.1, 1.0);
uniform float intensity : hint_range(0, 1, 0.01) = 1.0;

// Hash function for particles
float hash(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

// Noise function
float noise(vec2 uv) {
	vec2 grid = floor(uv);
	vec2 f = fract(uv);
	
	float a = hash(grid);
	float b = hash(grid + vec2(1.0, 0.0));
	float c = hash(grid + vec2(0.0, 1.0));
	float d = hash(grid + vec2(1.0, 1.0));
	
	vec2 u = f * f * (3.0 - 2.0 * f);
	
	return mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

void fragment() {
	vec2 uv = UV;
	vec2 center_offset = uv - explosion_center;
	float distance = length(center_offset);
	float angle = atan(center_offset.y, center_offset.x);
	
	// Calculate explosion fade based on progress
	float explosion_fade = 1.0 - explosion_progress;
	explosion_fade = pow(explosion_fade, 0.5);
	
	// Scale distance by explosion size and progress
	float scaled_distance = distance / (explosion_size * (0.1 + explosion_progress * 0.9));
	
	// Core effect - bright center that pulses
	float core_pulse = sin(TIME * core_pulse_speed) * 0.3 + 0.7;
	float core = smoothstep(core_size * 1.5, core_size, scaled_distance);
	core *= core_pulse;
	
	// Inner glow
	float inner_glow = smoothstep(core_size * 3.0, core_size * 0.5, scaled_distance);
	inner_glow *= 0.5;
	
	// Shockwave rings
	float rings = 0.0;
	for (int i = 0; i < 10; i++) {
		if (i >= num_rings) break;
		
		float ring_radius = (float(i) + 1.0) * ring_spacing + explosion_progress * ring_spacing * 0.5;
		float ring = smoothstep(ring_radius + ring_width, ring_radius, scaled_distance);
		ring *= smoothstep(ring_radius - ring_width, ring_radius, scaled_distance);
		
		// Fade rings as they expand
		float ring_fade = 1.0 - (float(i) / float(num_rings));
		rings += ring * ring_fade;
	}
	
	// Particle system
	float particles = 0.0;
	vec2 particle_uv = vec2(angle * particle_density, scaled_distance * particle_density);
	float particle_hash = hash(floor(particle_uv));
	
	// Only show particles within explosion radius
	if (scaled_distance < 1.5) {
		// Create particle positions
		float particle_dist = fract(particle_hash * 100.0) * 1.5;
		float particle_angle = fract(particle_hash * 1000.0) * 6.28318;
		
		// Animate particles outward
		float animated_dist = particle_dist + explosion_progress * particle_speed * 0.5;
		if (animated_dist < 1.5) {
			// Calculate particle size
			float particle = smoothstep(particle_size * 2.0, 0.0, 
				length(vec2(distance - animated_dist * explosion_size, 
				angle - particle_angle)));
			
			// Fade particles as they expand
			float particle_fade = 1.0 - animated_dist / 1.5;
			particles += particle * particle_fade;
		}
	}
	
	// Add noise to particles for more organic look
	float particle_noise = noise(uv * noise_scale + TIME * 2.0);
	particles *= (1.0 + particle_noise * noise_strength);
	
	// Debris/spark effect
	float debris = 0.0;
	for (int i = 0; i < 8; i++) {
		float debris_angle = float(i) * 0.785398; // 8 directions
		float debris_dist = explosion_progress * 1.2 * (0.8 + hash(vec2(float(i), 0.0)) * 0.4);
		
		float angle_diff = abs(angle - debris_angle);
		angle_diff = min(angle_diff, 6.28318 - angle_diff);
		
		float spark = smoothstep(0.1, 0.0, angle_diff);
		spark *= smoothstep(0.02, 0.0, abs(distance - debris_dist * explosion_size));
		debris += spark;
	}
	
	// Combine all effects
	vec4 final_color = vec4(0.0);
	
	// Core (white-hot center)
	final_color += core_color * core * 2.0;
	
	// Inner glow (yellow)
	final_color += inner_color * inner_glow;
	
	// Rings (orange/red)
	final_color += outer_color * rings * 0.8;
	
	// Particles (orange)
	final_color += outer_color * particles * 0.6;
	
	// Debris sparks (bright)
	final_color += core_color * debris * 1.5;
	
	// Apply explosion fade and overall intensity
	final_color *= explosion_fade * intensity;
	
	COLOR = final_color;
}
