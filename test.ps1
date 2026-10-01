<#
  Runs the headless test suite through the Unity CLI.

  Several agents may be driving this project at once, and Unity refuses to open a
  project twice. This script waits for the lock, then runs; it retries because a
  competing run can steal the lock between the check and the launch.

  Usage:  pwsh -File C:\dev\ossuary\test.ps1
#>
param(
  [string]$Method = 'Ossuary.Tests.TestRunner.RunAll',
  [int]$WaitSec = 900,
  [int]$Attempts = 3
)

$ErrorActionPreference = 'Stop'

$unity   = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe'
$project = 'C:\dev\ossuary\unity'
$lock    = Join-Path $project 'Temp\UnityLockfile'
$logDir  = Join-Path $env:LOCALAPPDATA 'Temp\opencode'
$log     = Join-Path $logDir 'ossuary_test.log'

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

function Get-UnityBusy {
  # A live editor owns the project; a stale lockfile from a crashed run does not.
  if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { return $true }
  return $false
}

function Wait-UnityFree {
  param([int]$Seconds)
  $deadline = (Get-Date).AddSeconds($Seconds)
  while ((Get-Date) -lt $deadline) {
    if (-not (Get-UnityBusy)) { return $true }
    Start-Sleep -Seconds 5
  }
  return $false
}

for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
  Write-Output ("-- attempt {0}: waiting for the project lock --" -f $attempt)
  if (-not (Wait-UnityFree -Seconds $WaitSec)) {
    Write-Output "timed out waiting for the lock"
    exit 2
  }

  # Only clear the lockfile once nothing is running, otherwise we corrupt a live run.
  if (Test-Path $lock) { Remove-Item $lock -Force -ErrorAction SilentlyContinue }
  Start-Sleep -Milliseconds 500

  if (Test-Path $log) { Remove-Item $log -Force }
  & $unity -batchmode -nographics -quit -projectPath $project -executeMethod $Method -logFile $log 2>&1 |
    Tee-Object -FilePath (Join-Path $logDir 'ossuary_test.stdout.txt') | Out-Null
  $code = $LASTEXITCODE

  if (Test-Path $log) {
    $results = Select-String -Path $log -Pattern '^  ok |^  FAIL|^====' | ForEach-Object { $_.Line }
    if ($results) {
      $results | ForEach-Object { Write-Output $_ }
      Write-Output "exit=$code"
      exit $code
    }
  }

  # No results in the log means the run never started (lock stolen, or a crash).
  Write-Output "run produced no test output; retrying"
  Start-Sleep -Seconds 5
}

Write-Output "could not get a clean test run"
exit 2