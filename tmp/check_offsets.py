import re

with open('battle/UIScene/Reward/SkillCard.tscn', 'r') as f:
    lines = f.read().splitlines()

for i, line in enumerate(lines):
    m = re.search(r'offset_right\s*=\s*([0-9.]+)', line)
    if m and float(m.group(1)) > 240:
        for j in range(i, -1, -1):
            n = re.search(r'\[node name="([^"]+)"', lines[j])
            if n:
                print(n.group(1) + ': ' + line.strip())
                break
