shader_type canvas_item;

uniform vec4 main_color : source_color = vec4(0.24, 0.82, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.96, 0.98, 1.0, 1.0);
uniform vec4 pulse_color : source_color = vec4(0.98, 0.90, 0.46, 1.0);
uniform float flow_speed : hint_range(0.0, 6.0) = 1.8;
uniform float stream_width : hint_range(0.005, 0.08) = 0.022;
uniform float glow_strength : hint_range(0.0, 2.0) = 0.28;

float sd_segment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float sd_box(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0);
}

float stroke(float d, float width, float aa) {
    return 1.0 - smoothstep(width, width + aa, d);
}

float fill_box(vec2 p, vec2 b, float aa) {
    return 1.0 - smoothstep(0.0, aa, sd_box(p, b));
}

float line_pulse(vec2 p, vec2 a, vec2 b, float phase, float width, float aa) {
    vec2 ba = b - a;
    float len2 = max(dot(ba, ba), 0.0001);
    float t = clamp(dot(p - a, ba) / len2, 0.0, 1.0);
    float line = stroke(sd_segment(p, a, b), width, aa);
    float head = fract(TIME * flow_speed + phase);
    float pulse = exp(-pow(abs(t - head) / 0.11, 2.0));
    float tail = smoothstep(0.42, 0.0, fract(head - t + 1.0));
    return line * (pulse + tail * 0.55);
}

void fragment() {
    vec2 uv = (UV - 0.5) * 2.0;
    uv.x *= 0.96;
    float aa = max(fwidth(uv.x), fwidth(uv.y)) * 1.7;

    float pulse = 0.5 + 0.5 * sin(TIME * 3.0);

    vec2 sender_p = uv + vec2(0.50, 0.04);
    float sender_core = fill_box(sender_p, vec2(0.14, 0.14), aa);
    float sender_ring = stroke(abs(length(sender_p) - 0.22), 0.030, aa) * 0.88;

    vec2 receiver_p = uv - vec2(0.42, -0.02);
    float receiver_frame = stroke(sd_box(receiver_p, vec2(0.20, 0.28)), 0.028, aa);
    float receiver_inner = stroke(sd_box(receiver_p + vec2(0.0, -0.02), vec2(0.12, 0.19)), 0.020, aa) * 0.72;
    float receiver_slot = stroke(sd_segment(receiver_p, vec2(-0.09, -0.07), vec2(0.09, -0.07)), 0.018, aa) * 0.90;

    float stream_top = stroke(sd_segment(uv, vec2(-0.30, -0.22), vec2(0.18, -0.18)), stream_width, aa);
    float stream_mid = stroke(sd_segment(uv, vec2(-0.34, 0.00), vec2(0.16, 0.00)), stream_width, aa);
    float stream_low = stroke(sd_segment(uv, vec2(-0.28, 0.22), vec2(0.20, 0.18)), stream_width, aa);
    float streams = max(stream_top, max(stream_mid, stream_low));

    float stream_pulses = 0.0;
    stream_pulses += line_pulse(uv, vec2(-0.30, -0.22), vec2(0.18, -0.18), 0.00, stream_width * 0.85, aa);
    stream_pulses += line_pulse(uv, vec2(-0.34, 0.00), vec2(0.16, 0.00), 0.33, stream_width * 0.85, aa);
    stream_pulses += line_pulse(uv, vec2(-0.28, 0.22), vec2(0.20, 0.18), 0.66, stream_width * 0.85, aa);

    float data_shards = 0.0;
    data_shards += fill_box(uv - vec2(-0.04 + fract(TIME * flow_speed) * 0.28, -0.18), vec2(0.028, 0.028), aa);
    data_shards += fill_box(uv - vec2(-0.10 + fract(TIME * flow_speed + 0.33) * 0.32, 0.00), vec2(0.024, 0.024), aa);
    data_shards += fill_box(uv - vec2(-0.02 + fract(TIME * flow_speed + 0.66) * 0.24, 0.18), vec2(0.022, 0.022), aa);
    data_shards *= 0.70 + pulse * 0.30;

    float connector = stroke(sd_segment(uv, vec2(0.28, 0.04), vec2(0.10, 0.04)), 0.026, aa);
    connector += stroke(sd_segment(uv, vec2(-0.18, 0.02), vec2(-0.34, 0.02)), 0.024, aa) * 0.75;

    float glow = smoothstep(1.05, 0.06, length(uv)) * glow_strength;

    vec3 color = main_color.rgb * (0.62 + 0.18 * pulse);
    color = mix(color, accent_color.rgb, clamp(receiver_frame + receiver_inner + receiver_slot, 0.0, 1.0) * 0.72);
    color = mix(color, accent_color.rgb, clamp(sender_core + sender_ring, 0.0, 1.0) * 0.65);
    color = mix(color, pulse_color.rgb, clamp(stream_pulses + data_shards * 0.7, 0.0, 1.0) * 0.92);
    color += main_color.rgb * glow;

    float alpha = 0.0;
    alpha += sender_core * 0.95 + sender_ring * 0.68;
    alpha += receiver_frame * 0.96 + receiver_inner * 0.52 + receiver_slot * 0.64;
    alpha += streams * 0.54 + stream_pulses * 0.76;
    alpha += data_shards * 0.70 + connector * 0.52 + glow * 0.25;
    alpha = clamp(alpha, 0.0, 1.0);

    COLOR *= vec4(color, alpha);
}
