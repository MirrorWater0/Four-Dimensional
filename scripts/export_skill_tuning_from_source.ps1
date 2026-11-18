param(
    [switch]$Watch,
    [int]$DebounceMs = 350
)

# Kept as a compatibility entry point; tuning is no longer generated from C#.
Write-Host "[Tuning] 卡牌和敌人数值均已改为 Resource，不再从源码导出。"
Write-Host "请在游戏图鉴中保存，或编辑 data/skill_tuning 与 data/enemy_tuning 下的 .tres。"
