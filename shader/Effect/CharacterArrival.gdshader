shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float phase : hint_range(0.0, 1.0) = 0.0;
uniform vec4 ink : source_color = vec4(0.52, 0.78, 1.0, 1.0);
uniform float facing = 1.0;
uniform vec2 vertical_span = vec2(1.18, 0.22);
uniform bool impact_layer = false;

float stroke(float distance_to_line, float width) {
    float aa = max(fwidth(distance_to_line), 0.002);
    return 1.0 - smoothstep(width, width + aa, abs(distance_to_line));
}

float hash_cell(float n) {
    return fract(sin(n * 71.13 + 9.91) * 43758.5453);
}

vec2 draw_rift(vec2 p, float t) {
    // Fold inward, then tear outwards quickly; the tips stay pinned.
    float fold = smoothstep(0.0, 0.15, t);
    float tear = smoothstep(0.14, 0.29, t) * (1.0 - smoothstep(0.35, 0.61, t));
    float fade = smoothstep(0.0, 0.06, t) * (1.0 - smoothstep(0.40, 0.64, t));
    float y = clamp(p.y, 0.0, 1.10);
    float belly = max(0.0, 1.0 - abs(y - 0.50) / 0.61);
    float jagged = (abs(fract(y * 5.5 + 0.24) * 2.0 - 1.0) - 0.5) * 0.11;
    float seam = facing * ((y - 0.50) * 0.30 + jagged);
    float x = p.x - seam;
    float edge = abs(x) - (0.008 + tear * 0.60) * belly;
    float tip = smoothstep(-0.015, 0.09, p.y) * (1.0 - smoothstep(0.96, 1.12, p.y));
    float edge_light = stroke(edge, 0.011) * 0.74 + exp(-abs(edge) * 21.0) * 0.26;
    float inside = 1.0 - smoothstep(-0.025, 0.0, edge);
    float bands = 0.5 + 0.5 * sin(x * 57.0 + y * 11.0 - t * 24.0);
    float rift = (edge_light + inside * tear * (0.18 + bands * 0.13)) * tip * fade;

    float rail = abs(x) - mix(0.74, 0.055, fold) * mix(0.39, 1.0, belly);
    float charge = stroke(rail, 0.008) * tip
        * smoothstep(0.0, 0.055, t) * (1.0 - smoothstep(0.13, 0.23, t));
    float ignition = smoothstep(0.14, 0.177, t) * (1.0 - smoothstep(0.185, 0.30, t));
    float core = exp(-abs(x) * 17.0) * tip * ignition;
    float crown = exp(-abs(p.y - 0.82) * 70.0 - abs(x) * 5.5) * ignition;

    // Shared beat with the body reveal: the scan starts at .18 and hits at .45.
    float sweep = pow(clamp((t - 0.18) / 0.27, 0.0, 1.0), 1.45);
    float head = mix(1.10, -0.10, sweep);
    float scan = exp(-abs(p.y - max(0.0, head)) * 88.0 - abs(x) * 3.9)
        * smoothstep(0.18, 0.22, t) * (1.0 - smoothstep(0.42, 0.47, t));

    return vec2(rift + charge * 0.52 + core + crown * 0.8 + scan * 0.85,
        fade * tip * stroke(edge, 0.007) * 0.62 + core + crown + scan);
}

vec2 draw_impact(vec2 p, float t) {
    vec2 floor_p = vec2(p.x, p.y * 5.80);
    float dist = length(floor_p);
    float angle = atan(floor_p.y, floor_p.x + 0.00001);
    float ticks = smoothstep(0.43, 0.61, abs(sin(angle * 10.0 - facing * t * 7.0)));
    float charge = smoothstep(0.015, 0.14, t) * (1.0 - smoothstep(0.24, 0.45, t));
    float lock_ring = charge * (stroke(dist - mix(0.55, 0.20, smoothstep(0.04, 0.39, t)), 0.008)
        * (0.20 + ticks * 0.52) + stroke(dist - 0.25, 0.008) * 0.24);

    float burst = smoothstep(0.43, 0.45, t);
    float age = max(0.0, t - 0.45);
    float travel = 1.0 - exp(-age * 8.5);
    float pressure = burst * (1.0 - smoothstep(0.53, 0.91, t));
    float outer = dist - mix(0.13, 0.96, travel);
    float ring = (stroke(outer, mix(0.030, 0.008, travel)) + exp(-abs(outer) * 21.0) * 0.37) * pressure;
    float echo = stroke(dist - mix(0.11, 0.80, travel * travel), 0.015) * pressure * (0.27 + ticks * 0.49);
    float flash = burst * (1.0 - smoothstep(0.455, 0.56, t));
    float flare = exp(-abs(p.y) * 88.0 - abs(p.x) * 3.9) * flash;
    float contact = exp(-length(floor_p) * 5.9) * flash;

    // One shard per angular cell, with a short trail that falls off as it travels.
    vec2 lifted = vec2(p.x, (p.y - 0.026) * 3.0);
    float sector = TAU / 16.0;
    float cell = floor(atan(lifted.y, lifted.x + 0.00001) / sector);
    float seed = hash_cell(cell);
    float heading = (cell + mix(0.29, 0.71, seed)) * sector;
    vec2 ray = vec2(cos(heading), sin(heading));
    float along = dot(lifted, ray);
    float across = dot(lifted, vec2(-ray.y, ray.x));
    float radius = mix(0.12, mix(0.53, 0.93, seed), travel);
    float chip = abs(along - radius) / mix(0.046, 0.025, travel) + abs(across) / mix(0.019, 0.031, seed);
    float shard = (1.0 - smoothstep(0.61, 1.08, chip))
        + exp(-abs(across) * 93.0) * smoothstep(radius - 0.20, radius, along)
        * (1.0 - smoothstep(radius, radius + 0.027, along)) * (1.0 - travel) * 0.7;
    shard *= burst * smoothstep(-0.04, 0.045, p.y)
        * (1.0 - smoothstep(mix(0.62, 0.81, seed), 0.98, t));

    return vec2(lock_ring + ring * 0.85 + echo + flare + contact * 0.72 + shard * 0.84,
        ring * 0.57 + flare + contact + shard * 0.75);
}

void fragment() {
    // The origin is the actor's foot ring; both local quads use the same coordinates.
    vec2 p = vec2((UV.x - 0.5) * 2.0, vertical_span.x - UV.y * (vertical_span.x + vertical_span.y));
    vec2 energy = vec2(0.0);
    // This branch is uniform for the whole draw; derivatives inside each layer are valid.
    if (impact_layer) {
        energy = draw_impact(p, phase);
    } else {
        energy = draw_rift(p, phase);
    }
    float bounds = (1.0 - smoothstep(0.91, 1.0, abs(p.x)))
        * (1.0 - smoothstep(0.92, 1.0, UV.y)) * smoothstep(0.0, 0.05, UV.y);
    vec3 light = mix(ink.rgb, vec3(1.0, 0.98, 0.94), clamp(energy.y, 0.0, 0.94));
    COLOR = vec4(light, clamp(energy.x, 0.0, 1.0) * bounds * COLOR.a);
}
