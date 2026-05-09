import re

with open('.godot/editor/SkillCard.tscn-editstate-ea9e2f58640b4c1629e8a259277a4163.cfg', 'rb') as f:
    content = f.read()

try:
    text = content.decode('utf-8')
except:
    text = content.decode('utf-16-le', errors='replace')

paths = re.findall(r'NodePath\("([^"]+)"\)', text)
for p in paths:
    if 'ArtFrame' in p or 'SkillPicture' in p or 'SkillIcon' in p:
        print(p)
