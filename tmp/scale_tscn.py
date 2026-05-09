import re

with open('battle/UIScene/Reward/SkillCard.tscn', 'r') as f:
    content = f.read()

# Scale factor
scale = 2.0

def scale_offset(match):
    prefix = match.group(1)
    val = float(match.group(2))
    # Only scale values that are actual pixel positions (not scale vectors, not size vectors)
    scaled = val * scale
    # Keep integer-like values as integers
    if scaled == int(scaled):
        return f"{prefix} = {int(scaled)}.0"
    else:
        return f"{prefix} = {scaled}"

# Scale offset_left, offset_top, offset_right, offset_bottom
content = re.sub(r'(offset_(?:left|top|right|bottom))\s*=\s*([0-9.]+)', scale_offset, content)

# Scale custom_minimum_size Vector2
content = re.sub(
    r'(custom_minimum_size\s*=\s*Vector2)\(([0-9.]+),\s*([0-9.]+)\)',
    lambda m: f"{m.group(1)}({float(m.group(2))*scale}, {float(m.group(3))*scale})",
    content
)

# Scale size Vector2i for SubViewport
content = re.sub(
    r'(size\s*=\s*Vector2i)\(([0-9.]+),\s*([0-9.]+)\)',
    lambda m: f"{m.group(1)}({int(float(m.group(2))*scale)}, {int(float(m.group(3))*scale)})",
    content
)

with open('battle/UIScene/Reward/SkillCard.tscn', 'w') as f:
    f.write(content)

print("Scaled by 2.0x")
