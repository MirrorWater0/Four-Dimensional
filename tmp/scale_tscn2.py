import re

with open('battle/UIScene/Reward/SkillCard.tscn', 'r') as f:
    content = f.read()

# Scale factor
scale = 2.0

# Scale polygon PackedVector2Array
poly_pattern = re.compile(r'(polygon\s*=\s*PackedVector2Array\()([^)]+)(\))')

def scale_polygon(match):
    prefix = match.group(1)
    values_str = match.group(2)
    suffix = match.group(3)
    
    # Parse all numbers
    nums = re.findall(r'[0-9.]+', values_str)
    scaled = []
    for num in nums:
        val = float(num)
        new_val = val * scale
        if new_val == int(new_val):
            scaled.append(str(int(new_val)))
        else:
            scaled.append(str(new_val))
    
    # Reconstruct with commas
    result = prefix + ', '.join(scaled) + suffix
    return result

content = poly_pattern.sub(scale_polygon, content)

# Scale font sizes
font_pattern = re.compile(r'(font_size\s*=\s*)([0-9.]+)')

def scale_font(match):
    prefix = match.group(1)
    val = float(match.group(2))
    scaled = val * scale
    if scaled == int(scaled):
        return prefix + str(int(scaled))
    else:
        return prefix + str(scaled)

content = font_pattern.sub(scale_font, content)

with open('battle/UIScene/Reward/SkillCard.tscn', 'w') as f:
    f.write(content)

print("Scaled polygons and font sizes by 2.0x")
