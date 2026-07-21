param(
    [switch]$Watch,
    [int]$DebounceMs = 350
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$OutRoot = Join-Path $ProjectRoot "data\skill_tuning"
$EnemyOutRoot = Join-Path $ProjectRoot "data\enemy_tuning"

function Convert-ToSnakeCase {
    param([string]$Value)
    return [regex]::Replace($Value, "(?<!^)(?=[A-Z])", "_").ToLowerInvariant()
}

function Read-JsonFile {
    param([string]$Path)
    if (!(Test-Path -LiteralPath $Path)) {
        return @{}
    }

    $json = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    return ConvertTo-Hashtable ($json | ConvertFrom-Json)
}

function ConvertTo-Hashtable {
    param($Value)

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [System.Collections.IEnumerable] -and $Value -isnot [string] -and $Value -isnot [pscustomobject]) {
        $array = @()
        foreach ($item in $Value) {
            $array += ConvertTo-Hashtable $item
        }
        return $array
    }

    if ($Value -is [pscustomobject]) {
        $hash = @{}
        foreach ($property in $Value.PSObject.Properties) {
            $hash[$property.Name] = ConvertTo-Hashtable $property.Value
        }
        return $hash
    }

    return $Value
}

function Get-SkillIdMetadata {
    $path = Join-Path $ProjectRoot "character\SkillBase\SkillID.Generated.cs"
    $metadata = @{}
    $attributes = @()

    foreach ($line in Get-Content -LiteralPath $path -Encoding UTF8) {
        if ($line.Trim() -match "^\[(.+)\]$") {
            $attributes += $Matches[1]
            continue
        }

        if ($line -notmatch "^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*-?\d+") {
            continue
        }

        $skillId = $Matches[1]
        $character = $null
        $colorless = $false
        foreach ($attribute in $attributes) {
            if ($attribute -match "PlayerSkill\(PlayerCharacterKey\.([A-Za-z]+)\)") {
                $character = $Matches[1]
            }
            if ($attribute -like "*ColorlessSkill*") {
                $colorless = $true
            }
        }

        $metadata[$skillId] = @{
            Player = $character
            Colorless = $colorless
        }
        $attributes = @()
    }

    return $metadata
}

function Get-TypeNameOverrides {
    $path = Join-Path $ProjectRoot "character\SkillBase\Skill.Registry.cs"
    $text = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    $overrides = @{}
    foreach ($match in [regex]::Matches($text, '\["([A-Za-z_][A-Za-z0-9_]*)"\]\s*=\s*SkillID\.([A-Za-z_][A-Za-z0-9_]*)')) {
        $overrides[$match.Groups[1].Value] = $match.Groups[2].Value
    }
    return $overrides
}

function Resolve-SkillId {
    param(
        [string]$ClassName,
        [hashtable]$SkillMeta,
        [hashtable]$Overrides
    )

    if ($Overrides.ContainsKey($ClassName)) {
        return $Overrides[$ClassName]
    }
    if ($SkillMeta.ContainsKey($ClassName)) {
        return $ClassName
    }
    if ($ClassName.EndsWith("Skill")) {
        $trimmed = $ClassName.Substring(0, $ClassName.Length - 5)
        if ($SkillMeta.ContainsKey($trimmed)) {
            return $trimmed
        }
    }

    return $null
}

function Find-ClassBodyEnd {
    param(
        [string]$Text,
        [int]$OpenBraceIndex
    )

    $depth = 0
    for ($i = $OpenBraceIndex; $i -lt $Text.Length; $i++) {
        if ($Text[$i] -eq "{") {
            $depth++
        } elseif ($Text[$i] -eq "}") {
            $depth--
            if ($depth -eq 0) {
                return $i + 1
            }
        }
    }

    return $Text.Length
}

function Get-SourceFiles {
    $roots = @(
        "character\SkillBase",
        "character\PlayerCharacter",
        "character\EnemyCharacter"
    )
    $skip = @(
        "Skill.SSOT.cs",
        "Skill.cs",
        "Skill.Tuning.cs",
        "SkillTuning.cs",
        "Skill.Registry.cs",
        "SkillID.Generated.cs"
    )

    foreach ($root in $roots) {
        Get-ChildItem -LiteralPath (Join-Path $ProjectRoot $root) -Recurse -Filter *.cs |
            Where-Object { $skip -notcontains $_.Name }
    }
}

function Get-SkillEntries {
    $zh = Read-JsonFile (Join-Path $ProjectRoot "localization\zh_CN.json")
    $nameOverrides = Get-NameOverrides
    $skillMeta = Get-SkillIdMetadata
    $overrides = Get-TypeNameOverrides
    $entries = @{}
    $sources = @{}

    foreach ($file in Get-SourceFiles) {
        $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        foreach ($match in [regex]::Matches($text, '(?:public|private|internal)?\s*(?:partial\s+)?class\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*Skill\b')) {
            $className = $match.Groups[1].Value
            $skillId = Resolve-SkillId $className $skillMeta $overrides
            if ([string]::IsNullOrWhiteSpace($skillId) -or $skillId -eq "None") {
                continue
            }

            $braceIndex = $text.IndexOf("{", $match.Index + $match.Length)
            if ($braceIndex -lt 0) {
                continue
            }

            $endIndex = Find-ClassBodyEnd $text $braceIndex
            $body = $text.Substring($braceIndex, $endIndex - $braceIndex)
            $nameKeyClass = "skill.$(Convert-ToSnakeCase $className).name"
            $nameKeyId = "skill.$(Convert-ToSnakeCase $skillId).name"
            $fallbackName = $className
            $skillNameMatch = [regex]::Match($body, 'SkillName\s*\{[^}]*\}\s*=\s*"([^"]+)"')
            $trMatch = [regex]::Match($body, 'I18n\.Tr\(\s*"[^"]+"\s*,\s*"([^"]+)"\s*\)')
            if ($skillNameMatch.Success) {
                $fallbackName = $skillNameMatch.Groups[1].Value
            } elseif ($trMatch.Success) {
                $fallbackName = $trMatch.Groups[1].Value
            }
            if (!$skillNameMatch.Success -and $zh.ContainsKey($nameKeyClass)) {
                $fallbackName = $zh[$nameKeyClass]
            } elseif (!$skillNameMatch.Success -and $zh.ContainsKey($nameKeyId)) {
                $fallbackName = $zh[$nameKeyId]
            }
            if (!$skillNameMatch.Success -and $nameOverrides.ContainsKey($skillId)) {
                $fallbackName = $nameOverrides[$skillId]
            }

            $entry = [ordered]@{ Name = $fallbackName }

            foreach ($constMatch in [regex]::Matches($body, '\bconst\s+int\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*;')) {
                $entry[$constMatch.Groups[1].Value] = [int]$constMatch.Groups[2].Value
            }
            foreach ($valueMatch in [regex]::Matches($body, 'V\(\s*"([A-Za-z_][A-Za-z0-9_]*)"\s*,\s*(-?\d+)\s*\)')) {
                $entry[$valueMatch.Groups[1].Value] = [int]$valueMatch.Groups[2].Value
            }
            $energyMatch = [regex]::Match($body, 'public\s+override\s+int\s+EnergyCost\s*=>\s*V\(nameof\(EnergyCost\),\s*(-?\d+)\s*\)\s*;')
            if ($energyMatch.Success) {
                $entry["EnergyCost"] = [int]$energyMatch.Groups[1].Value
            }
            $cooldownMatch = [regex]::Match($body, 'public\s+override\s+int\s+EnemySpecialIntentionCooldown\s*=>\s*(-?\d+)\s*;')
            if ($cooldownMatch.Success) {
                $entry["EnemySpecialIntentionCooldown"] = [int]$cooldownMatch.Groups[1].Value
            }

            $entries[$skillId] = $entry
            $sources[$skillId] = Resolve-Path -LiteralPath $file.FullName -Relative
        }
    }

    foreach ($skillId in $skillMeta.Keys) {
        if ($skillId -eq "None" -or $entries.ContainsKey($skillId)) {
            continue
        }

        $nameKey = "skill.$(Convert-ToSnakeCase $skillId).name"
        $fallbackName = if ($zh.ContainsKey($nameKey)) { $zh[$nameKey] } else { $skillId }
        if ($nameOverrides.ContainsKey($skillId)) {
            $fallbackName = $nameOverrides[$skillId]
        }
        $entries[$skillId] = [ordered]@{
            Name = $fallbackName
        }
        $sources[$skillId] = ""
    }

    return @{
        Entries = $entries
        Sources = $sources
        SkillMeta = $skillMeta
    }
}

function Get-EnemyEntries {
    $entries = @{}
    $sources = @{}

    foreach ($file in Get-SourceFiles) {
        $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        foreach ($match in [regex]::Matches($text, '(?:public|private|internal)?\s*(?:partial\s+)?class\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*EnemyRegedit\b')) {
            $className = $match.Groups[1].Value
            $enemyKey = $className
            if ($enemyKey.EndsWith("Regedit")) {
                $enemyKey = $enemyKey.Substring(0, $enemyKey.Length - "Regedit".Length)
            }

            $braceIndex = $text.IndexOf("{", $match.Index + $match.Length)
            if ($braceIndex -lt 0) {
                continue
            }

            $endIndex = Find-ClassBodyEnd $text $braceIndex
            $body = $text.Substring($braceIndex, $endIndex - $braceIndex)
            $name = $enemyKey
            $nameMatch = [regex]::Match($body, 'CharacterName\s*=\s*"([^"]+)"\s*;')
            if ($nameMatch.Success) {
                $name = $nameMatch.Groups[1].Value
            }

            $entry = [ordered]@{ Name = $name }
            $maxLifeMatch = [regex]::Match($body, '\bMaxLife\s*=\s*(-?\d+)\s*;')
            if ($maxLifeMatch.Success) {
                $entry["MaxLife"] = [int]$maxLifeMatch.Groups[1].Value
            }

            $entries[$enemyKey] = $entry
            $sources[$enemyKey] = Resolve-Path -LiteralPath $file.FullName -Relative
        }
    }

    return @{
        Entries = $entries
        Sources = $sources
    }
}

function Get-NameOverrides {
    return Read-JsonFile (Join-Path $PSScriptRoot "skill_tuning_name_overrides.json")
}

function Get-BucketName {
    param(
        [string]$SkillId,
        [hashtable]$Entry,
        [string]$Source,
        [hashtable]$Meta
    )

    if ($SkillId.StartsWith("Basic")) {
        return "basic"
    }
    if ($SkillId.EndsWith("Status") -or @("VoidStatus", "WoundStatus", "DazeStatus", "PlagueStatus", "Calmness", "Blade", "DefenceFocus") -contains $SkillId) {
        return "status"
    }
    if ($Meta.Colorless) {
        return "colorless"
    }
    if (![string]::IsNullOrWhiteSpace($Meta.Player)) {
        return "player\$($Meta.Player.ToLowerInvariant())"
    }
    if ($Source -like "*character\EnemyCharacter\EnemyScript\*") {
        $stem = [IO.Path]::GetFileNameWithoutExtension($Source)
        return "enemy\$(Convert-ToSnakeCase $stem)"
    }

    return "other"
}

function Export-SkillTuning {
    $result = Get-SkillEntries
    $buckets = @{}

    foreach ($skillId in ($result.Entries.Keys | Sort-Object)) {
        $entry = $result.Entries[$skillId]
        $meta = if ($result.SkillMeta.ContainsKey($skillId)) { $result.SkillMeta[$skillId] } else { @{} }
        $bucket = Get-BucketName $skillId $entry $result.Sources[$skillId] $meta
        if (!$buckets.ContainsKey($bucket)) {
            $buckets[$bucket] = [ordered]@{}
        }
        $buckets[$bucket][$skillId] = $entry
    }

    if (Test-Path -LiteralPath $OutRoot) {
        Get-ChildItem -LiteralPath $OutRoot -Recurse -Filter *.json | Remove-Item -Force
    }
    New-Item -ItemType Directory -Path $OutRoot -Force | Out-Null

    foreach ($bucket in $buckets.Keys) {
        $outPath = Join-Path $OutRoot "$bucket.json"
        New-Item -ItemType Directory -Path (Split-Path $outPath -Parent) -Force | Out-Null
        [ordered]@{ skills = $buckets[$bucket] } |
            ConvertTo-Json -Depth 12 |
            Set-Content -LiteralPath $outPath -Encoding UTF8
    }

    Write-Host "[SkillTuningExport] wrote $($buckets.Count) files, $($result.Entries.Count) skill entries."
}

function Export-EnemyTuning {
    $result = Get-EnemyEntries
    $buckets = @{}

    foreach ($enemyKey in ($result.Entries.Keys | Sort-Object)) {
        $source = $result.Sources[$enemyKey]
        $stem = if (![string]::IsNullOrWhiteSpace($source)) {
            [IO.Path]::GetFileNameWithoutExtension($source)
        } else {
            $enemyKey
        }
        $bucket = "enemy\$(Convert-ToSnakeCase $stem)"
        if (!$buckets.ContainsKey($bucket)) {
            $buckets[$bucket] = [ordered]@{}
        }
        $buckets[$bucket][$enemyKey] = $result.Entries[$enemyKey]
    }

    if (Test-Path -LiteralPath $EnemyOutRoot) {
        Get-ChildItem -LiteralPath $EnemyOutRoot -Recurse -Filter *.json | Remove-Item -Force
    }
    New-Item -ItemType Directory -Path $EnemyOutRoot -Force | Out-Null

    foreach ($bucket in $buckets.Keys) {
        $outPath = Join-Path $EnemyOutRoot "$bucket.json"
        New-Item -ItemType Directory -Path (Split-Path $outPath -Parent) -Force | Out-Null
        [ordered]@{ enemies = $buckets[$bucket] } |
            ConvertTo-Json -Depth 12 |
            Set-Content -LiteralPath $outPath -Encoding UTF8
    }

    Write-Host "[EnemyTuningExport] wrote $($buckets.Count) files, $($result.Entries.Count) enemy entries."
}

function Export-AllTuning {
    Export-SkillTuning
    Export-EnemyTuning
}

function Watch-SkillSources {
    Export-AllTuning
    Write-Host "[TuningExport] watching tuning source files. Press Ctrl+C to stop."
    $lastRun = Get-Date
    while ($true) {
        Start-Sleep -Milliseconds 200
        $latest = Get-SourceFiles |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1 -ExpandProperty LastWriteTimeUtc
        if ($latest -gt $lastRun.ToUniversalTime()) {
            Start-Sleep -Milliseconds $DebounceMs
            Export-AllTuning
            $lastRun = Get-Date
        }
    }
}

if ($Watch) {
    Watch-SkillSources
} else {
    Export-AllTuning
}
