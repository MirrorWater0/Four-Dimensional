import re

with open('battle/UIScene/Reward/SkillCard.tscn', 'r') as f:
    lines = f.read().splitlines()

current_node = None
results = []

for i, line in enumerate(lines):
    node_match = re.search(r'\[node name="([^"]+)"', line)
    if node_match:
        current_node = node_match.group(1)
    
    m = re.search(r'offset_(left|top|right|bottom)\s*=\s*([0-9.]+)', line)
    if m and current_node:
        axis = m.group(1)
        val = float(m.group(2))
        results.append((current_node, axis, val))

# Print all unique (node, axis) with their values
seen = set()
for node, axis, val in results:
    key = (node, axis)
    if key not in seen:
        seen.add(key)
        print(f"{node}.{axis} = {val}")
