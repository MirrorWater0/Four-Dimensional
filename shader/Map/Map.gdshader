shader_type canvas_item;

// --- 调整参数 ---
uniform vec4 water_color : source_color = vec4(0.0, 0.05, 0.1, 1.0);
uniform vec4 land_color : source_color = vec4(0.05, 0.3, 0.6, 1.0);
uniform vec4 line_color : source_color = vec4(0.4, 0.8, 1.0, 1.0); // 岸线辉光颜色

uniform float map_scale : hint_range(1.0, 10.0) = 3.0;
uniform float sea_level : hint_range(0.0, 1.0) = 0.5;
uniform float line_thickness : hint_range(0.0, 0.1) = 0.02; // 岸线粗细

// --- 高质量平滑噪声算法 ---
vec2 hash(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

float gradient_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    // 五次曲线插值 (Smoothstep) 消除断裂感
    vec2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    
    return mix(mix(dot(hash(i + vec2(0.0, 0.0)), f - vec2(0.0, 0.0)),
                   dot(hash(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0)), u.x),
               mix(dot(hash(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0)),
                   dot(hash(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0)), u.x), u.y);
}

// 叠加热度，生成自然的大陆形状
float fbm(vec2 p) {
    float v = 0.0;
    float a = 0.5;
    mat2 rot = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));
    for (int i = 0; i < 4; i++) {
        v += a * gradient_noise(p);
        p = rot * p * 2.0;
        a *= 0.5;
    }
    return v * 0.5 + 0.5; // 映射到 0-1 空间
}

void fragment() {
    vec2 uv = UV;
    
    // 1. 获取基础地形高度 (FBM 噪声)
    float height = fbm(uv * map_scale);
    
    // 2. 边缘淡化 (防止地图在贴图边缘被突然截断)
    float mask = smoothstep(0.5, 0.2, distance(uv, vec2(0.5)));
    height *= mask;

    // 3. 计算陆地遮罩 (使用 smoothstep 消除锯齿)
    // 这里的 0.01 是平滑半径，解决了你之前的“切边”问题
    float land = smoothstep(sea_level - 0.01, sea_level + 0.01, height);
    
    // 4. 计算岸线辉光 (关键：提取海平面附近的极窄区域)
    float coast = smoothstep(sea_level - line_thickness, sea_level, height) - 
                  smoothstep(sea_level, sea_level + line_thickness, height);
    coast = clamp(coast, 0.0, 1.0);

    // 5. 最终色彩合成
    // 背景水色 + 陆地填充
    vec3 color = mix(water_color.rgb, land_color.rgb, land * 0.5);
    
    // 叠加辉光岸线 (让边缘亮起来，符合全息风格)
    color += coast * line_color.rgb * 2.0;

    // 6. 添加极其轻微的网格感 (可选，增加电子感)
    float grid = (sin(uv.x * 50.0) * sin(uv.y * 50.0)) * 0.03;
    color += grid * line_color.rgb;

    COLOR = vec4(color, 1.0);
}
