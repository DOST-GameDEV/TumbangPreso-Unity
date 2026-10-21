param(
    [Parameter(Mandatory = $true)][string]$AssemblyPath,
    [Parameter(Mandatory = $true)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$results = foreach ($distance in @(0, 1000, 1656, 1700, 1800, 1801)) {
    $record = [TumbangPreso.Core.MatchRecord]::new()
    $record.MatchId = 'travel-contract'
    $record.Rounds = 1
    $record.DurationSeconds = 30
    $line = [TumbangPreso.Core.PlayerMatchStats]::new()
    $line.Slot = 0
    $line.PlayerId = 'local-fixture'
    $line.Score = 0
    $line.Placement = 1
    $line.DistanceTravelled = $distance
    $record.Players = [TumbangPreso.Core.PlayerMatchStats[]]@($line)
    [pscustomobject]@{ distance = $distance; fault = [TumbangPreso.Core.IntegrityRules]::Check($record).ToString() }
}
$results | ConvertTo-Json | Set-Content -LiteralPath $OutputPath -Encoding utf8
