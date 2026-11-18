shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float tile_size = 0.03125;
uniform float gather = 0.0;
uniform float echo_radius = 54.0;
uniform vec2 flight_origin;
uniform vec2 plate_origin;
uniform vec2 plate_x;
uniform vec2 plate_y;
varying vec4 tint;
varying vec2 tile_uv;
varying vec2 local_uv;
varying float assembled;
varying float heat;

vec2 path_at(float t, vec2 target, vec2 offset, float a, float b, vec2 echo_dir) {
    vec2 delta = target - flight_origin;
    float dist = length(delta);
    vec2 forward = dist > 0.1 ? delta / dist : vec2(1.0, 0.0);
    vec2 side = vec2(-forward.y, forward.x);
    float angle = TAU * b;
    vec2 radial = vec2(cos(angle), sin(angle));
    // Blow out, hang briefly, then fire three packets down braided paths.
    float burst = 1.0 - pow(1.0 - clamp(t / 0.14, 0.0, 1.0), 3.0);
    vec2 recoil = radial * mix(25.0, 88.0, a) - forward * 16.0;
    float lane = min(2.0, floor(b * 3.0));
    float u = clamp((t - 0.115 - lane * 0.024 - a * 0.035) / 0.42, 0.0, 1.0);
    float travel = smoothstep(0.0, 1.0, u);
    float envelope = sin(PI * u);
    float bow = min(158.0, 30.0 + dist * 0.115);
    float bend = (lane - 1.0) * 0.82 + 0.42 * sin(TAU * u + lane * 1.8);
    vec2 plume = flight_origin + recoil * burst * (1.0 - smoothstep(0.0, 0.58, u));
    return mix(plume, target + echo_dir * echo_radius * (1.0 - gather), travel) + side * envelope * bow * bend
        + offset * smoothstep(0.65, 1.0, u);
}

void vertex() {
    tint = COLOR;
    tile_uv = INSTANCE_CUSTOM.rg;
    local_uv = UV;
    float a = INSTANCE_CUSTOM.b;
    float b = INSTANCE_CUSTOM.a;
    vec2 dest = plate_origin + 0.5 * (plate_x + plate_y);
    vec2 offset = plate_x * (tile_uv.x - 0.5) + plate_y * (tile_uv.y - 0.5);
    // Each sector carries a different piece of the real icon. Approach its echo
    // from the source, then keep moving inward; never split after reaching the dock.
    vec2 radial_uv = tile_uv - 0.5;
    float sector_angle = floor(atan(radial_uv.y, radial_uv.x) * 6.0 / TAU + 0.5) * TAU / 6.0;
    vec2 echo_dir = vec2(cos(sector_angle), sin(sector_angle));
    vec2 head = path_at(progress, dest, offset, a, b, echo_dir);
    vec2 tail = path_at(max(0.0, progress - mix(0.014, 0.036, a)), dest, offset, a, b, echo_dir);
    vec2 velocity = head - tail;
    float speed = length(velocity);
    vec2 axis = speed > 0.01 ? velocity / speed : vec2(1.0, 0.0);
    float length_px = min(speed, 95.0) + mix(3.0, 7.0, b);
    assembled = smoothstep(0.51 + 0.05 * a, 0.628, progress);
    heat = clamp(speed / 65.0, 0.0, 1.0);
    vec2 p = UV - 0.5;
    vec2 streak = axis * (UV.x - 0.95) * length_px
        + vec2(-axis.y, axis.x) * p.y * mix(2.0, 5.4, a);
    vec2 patch_offset = tile_size * (plate_x * p.x + plate_y * p.y);
    VERTEX = head + mix(streak, patch_offset, assembled);
}

void fragment() {
    vec4 seed = texture(TEXTURE, tile_uv);
    vec4 tile = texture(TEXTURE, tile_uv + (local_uv - 0.5) * tile_size);
    float head = smoothstep(0.0, 0.88, local_uv.x);
    float width = abs(2.0 * local_uv.y - 1.0) / mix(0.22, 1.0, head);
    float taper = (1.0 - smoothstep(0.32, 1.0, width)) * head;
    float cap = 1.0 - smoothstep(0.90, 1.0, local_uv.x);
    vec3 hot = mix(seed.rgb, vec3(1.0, 0.97, 0.88), 0.25 + 0.57 * heat * head);
    float fusion = smoothstep(0.74, 0.83, progress) * (1.0 - smoothstep(0.83, 0.90, progress));
    vec3 face = mix(tile.rgb, vec3(1.0, 0.98, 0.92), fusion * 0.30);
    COLOR = vec4(mix(hot, face, assembled), mix(seed.a * taper * cap, tile.a, assembled)) * tint;
}
