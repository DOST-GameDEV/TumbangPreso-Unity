$ErrorActionPreference = 'Stop'
$run = Join-Path $PSScriptRoot 'pc-laptop-hostloss1007a-host'
$launch = Get-Content -LiteralPath (Join-Path $run 'launch.json') -Raw | ConvertFrom-Json
if ($launch.pid -ne 40372 -or $launch.preparation.profile -ne 'pc-hostloss-cb5f1007a') { throw 'Unexpected retained host.' }
$readyMarker = '[NetAuto] READY submitted'
$roundMarker = '[Slice] round 1 begins'
while ($true) {
    $proc = Get-CimInstance Win32_Process -Filter 'ProcessId = 40372'
    if ($null -eq $proc) { throw 'Host ended before paired live stop.' }
    if ($proc.Name -ne 'TumbangPreso.exe' -or $proc.CommandLine -notlike '*pc-hostloss-cb5f1007a*' -or $proc.CommandLine -notlike '*49157*') { throw 'Exact host identity mismatch.' }
    $hostLog = Get-Content -LiteralPath (Join-Path $run 'player.log') -Raw
    if ($hostLog.Contains('[Slice] match over')) { throw 'Host completed; cannot claim active host loss.' }
    try {
        $clientResponse = Invoke-WebRequest -Uri 'http://192.168.1.144:18053/player.log' -UseBasicParsing
        $clientLog = if ($clientResponse.Content -is [byte[]]) { [System.Text.Encoding]::UTF8.GetString($clientResponse.Content) } else { [string]$clientResponse.Content }
    } catch { Start-Sleep -Milliseconds 500; continue }
    if ($clientLog.Contains('[Slice] match over') -or $clientLog.Contains('[Abandon]')) { throw 'Client ended or abandoned before controlled stop.' }
    if ($hostLog.Contains($readyMarker) -and $hostLog.Contains($roundMarker) -and $clientLog.Contains($readyMarker) -and $clientLog.Contains($roundMarker)) {
        $clientLaunch = (Invoke-WebRequest -Uri 'http://192.168.1.144:18053/launch.json' -UseBasicParsing).Content | ConvertFrom-Json
        if ($clientLaunch.preparation.profile -ne 'laptop-hostloss-cb5f1007a' -or $clientLaunch.runtimeSha256 -ne $launch.runtimeSha256 -or $clientLaunch.artifactManifestSha256 -ne $launch.artifactManifestSha256) { throw 'Paired artifact/profile identity mismatch.' }
        $observed = [DateTime]::UtcNow.ToString('o')
        [System.IO.File]::WriteAllText((Join-Path $run 'before-stop-host.log'), $hostLog)
        [System.IO.File]::WriteAllText((Join-Path $run 'before-stop-client.log'), $clientLog)
        $clientLaunch | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $run 'paired-client-launch.json')
        $receipt = @{hostPid=40372; clientPid=$clientLaunch.pid; hostStartedAtUtc=$launch.playerStartedAtUtc; clientStartedAtUtc=$clientLaunch.playerStartedAtUtc; observedBeforeStopUtc=$observed; bothReady=$true; bothActualRound1=$true; neitherMatchOver=$true; exactHostCommand=$proc.CommandLine; runtimeSha256=$launch.runtimeSha256; manifestSha256=$launch.artifactManifestSha256; purpose='Intentional active-match host loss; no normal host-completion PASS'}
        $receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $run 'live-stop.json')
        $verify = Get-CimInstance Win32_Process -Filter 'ProcessId = 40372'
        if ($null -eq $verify -or $verify.CreationDate -ne $proc.CreationDate -or $verify.CommandLine -ne $proc.CommandLine) { throw 'Host process changed before stop.' }
        Stop-Process -Id 40372
        $receipt.stoppedAtUtc = [DateTime]::UtcNow.ToString('o')
        $receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $run 'live-stop.json')
        $receipt | ConvertTo-Json -Compress
        break
    }
    Start-Sleep -Milliseconds 500
}
