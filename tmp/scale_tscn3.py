import re

with open('battle/UIScene/Reward/SkillCard.tscn', 'r') as f:
    content = f.read()

scale = 2.0

# Scale border_width
content = re.sub(
    r'(border_width_\w+\s*=\s*)([0-9.]+)',
    lambda m: f"{m.group(1)}{int(float(m.group(2)) * scale)}",
    content
)

# Scale content_margin
content = re.sub(
    r'(content_margin_\w+\s*=\s*)([0-9.]+)',
    lambda m: f"{m.group(1)}{float(m.group(2)) * scale}",
    content
)

# Scale outline_size
content = re.sub(
    r'(outline_size\s*=\s*)([0-9.]+)',
    lambda m: f"{m.group(1)}{int(float(m.group(2)) * scale)}",
    content
)

with open('battle/UIScene/Reward/SkillCard.tscn', 'w') as f:
    f.write(content)

print("Scaled border widths, content margins, and outline sizes")
