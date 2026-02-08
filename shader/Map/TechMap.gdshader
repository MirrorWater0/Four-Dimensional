shader_type canvas_item;

uniform vec4 land_color : source_color = vec4(0.9, 0.85, 0.8, 1.0); // Light/Paper color for Land
uniform vec4 ocean_color : source_color = vec4(0.2, 0.25, 0.3, 1.0); // Darker color for Ocean/Details
uniform float threshold : hint_range(0.0, 1.0) = 0.5; // Threshold to separate Land/Ocean
uniform float softness : hint_range(0.0, 0.2) = 0.02; // Edge smoothness

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    
    // 1. Convert to Grayscale
    float brightness = dot(tex.rgb, vec3(0.299, 0.587, 0.114));
    
    // 2. Simplify Shape via Threshold
    // Everything brighter than threshold becomes Land, darker becomes Ocean
    // This removes texture noise and small details
    float mask = smoothstep(threshold - softness, threshold + softness, brightness);
    
    // 3. Simplify Colors
    // Output only two colors based on the mask
    COLOR = mix(ocean_color, land_color, mask);
}
