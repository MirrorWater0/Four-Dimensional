import re

with open('battle/UIScene/Reward/SkillCard.tscn', 'r') as f:
    lines = f.read().splitlines()

current_node = None
for i, line in enumerate(lines):
    node_match = re.search(r'\[node name="([^"]+)"', line)
    if node_match:
        current_node = node_match.group(1)
    
    m = re.search(r'offset_right\s*=\s*([0-9.]+)', line)
    if m and float(m.group(1)) == 240.0 and current_node:
        print(current_node + ': ' + line.strip())
