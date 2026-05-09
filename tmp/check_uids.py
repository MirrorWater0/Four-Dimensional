import re

with open('battle/UIScene/Reward/SkillCard.tscn', 'r') as f:
    content = f.read()

uids = re.findall(r'uid://[^\s"\]]+', content)
print('UID references found:', len(uids))
for u in uids:
    print(' ', u)
