import re

with open('battle/UIScene/Reward/SkillCard.tscn', 'r') as f:
    content = f.read()

scale = 2.0

# Scale pivot_offset Vector2 values
content = re.sub(
    r'(pivot_offset\s*=\s*Vector2)\(([0-9.]+),\s*([0-9.]+)\)',
    lambda m: f"{m.group(1)}({int(float(m.group(2)) * scale)}, {int(float(m.group(3)) * scale)})",
    content
)

with open('battle/UIScene/Reward/SkillCard.tscn', 'w') as f:
    f.write(content)

print("Scaled pivot_offset values")
