# Runs the headless test suite through the Unity CLI.
#
# Unity refuses to open a project another instance holds, so a crashed or
# timed-out batch run blocks every later one. Two things guard against that:
# leftovers are killed first, and the Unity lock is waited out rather than
# raced. The script also treats "Unity never actually ran the method" as a
# failure, because a clean-looking exit code there means nothing was tested.
param(
    [string]$Method = 'Ossuary.Tests.TestRunner.RunAll',
    [int]$TimeoutSeconds = 1500
)

$ErrorActionPreference = 'Stop'
$project = 'C:\dev\ossuary\unity'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe'
$log = 'C:\Users\Chari\AppData\Local\Temp\opencode\ossuary_test.log'
$lock = Join-Path $project 'Temp\UnityLockfile'

Write-Host "== clearing stale Unity processes =="
foreach ($name in 'Unity', 'Unity.ILPP.Runner', 'UnityAutoQuitter', 'UnityPackageManager', 'UnityCrashHandler64') {
    Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "   killing $($_.Name) ($($_.Id))"
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
}

# Wait for the lock to actually go away. Deleting it while a dying process still
# holds the project just moves the "another instance" error to the next run.
$deadline = (Get-Date).AddSeconds(90)
while ((Test-Path $lock) -and ((Get-Date) -lt $deadline)) {
    Start-Sleep -Seconds 2
    if (-not (Get-Process -Name Unity -ErrorAction SilentlyContinue)) {
        Remove-Item $lock -Force -ErrorAction SilentlyContinue
    }
}
if (Test-Path $lock) {
    Remove-Item $lock -Force -ErrorAction SilentlyContinue
}
Remove-Item (Join-Path $project 'Temp') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $log -Force -ErrorAction SilentlyContinue

Write-Host "== running $Method =="
$proc = Start-Process -FilePath $unity -PassThru -NoNewWindow -ArgumentList `
    '-batchmode', '-nographics', '-projectPath', $project, '-quit', `
    '-executeMethod', $Method, '-logFile', $log

if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
    Write-Host "== TIMEOUT after $TimeoutSeconds s; killing =="
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Get-Content $log -Tail 60
    exit 2
}
$code = $proc.ExitCode
Write-Host "== exit code $code =="

if (-not (Test-Path $log)) { Write-Host 'no log written'; exit 3 }

if (Select-String -Path $log -Pattern 'error CS' -Quiet) {
    Write-Host '-- compiler errors --'
    Select-String -Path $log -Pattern 'error CS' | ForEach-Object { $_.Line } | Sort-Object -Unique
    exit 3
}

if (Select-String -Path $log -Pattern 'another Unity instance is running' -Quiet) {
    Write-Host '-- project was still locked --'
    exit 4
}

$results = Select-String -Path $log -Pattern '^\s+(ok|FAIL)\s|====|^\s+\(|^\s+at Ossuary' |
    ForEach-Object { $_.Line }
if (-not $results) {
    Write-Host '-- no test output: the method probably never ran --'
    Get-Content $log -Tail 40
    exit 5
}
$results | ForEach-Object { Write-Host $_ }

$summary = Select-String -Path $log -Pattern '==== (PASS|FAIL):' | Select-Object -Last 1
if (-not $summary) { Write-Host '-- no summary line --'; exit 6 }
if ($summary.Line -match 'FAIL') { exit 1 }
exit 0