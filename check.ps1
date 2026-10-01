<#
  Compile-checks the Ossuary Unity project from the CLI and prints only the
  C# errors. This is the project's single source of truth for "does it build".

  Usage:  pwsh -File C:\dev\ossuary\check.ps1
#>
param(
  [int]$TimeoutSec = 900,
  [string]$LogName = 'check'
)

$ErrorActionPreference = 'Stop'

$unity   = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe'
$project = 'C:\dev\ossuary\unity'
$logDir  = Join-Path $env:LOCALAPPDATA 'Temp\opencode'
$log     = Join-Path $logDir "$LogName.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
if (Test-Path $log) { Remove-Item $log -Force }

# A crashed run leaves a lock that makes the next invocation exit before logging.
$lock = Join-Path $project 'Temp\UnityLockfile'
if (Test-Path $lock) {
  if (-not (Get-Process -Name Unity -ErrorAction SilentlyContinue)) {
    Remove-Item $lock -Force
  }
}

& $unity -batchmode -nographics -quit -projectPath $project -logFile $log | Out-Null
$code = $LASTEXITCODE

if (-not (Test-Path $log)) {
  Write-Output "FATAL: Unity produced no log at $log (exit $code)"
  exit 2
}

$errors = Select-String -Path $log -Pattern ': error CS' | ForEach-Object { $_.Line } | Sort-Object -Unique
$warns  = Select-String -Path $log -Pattern ': warning CS' | ForEach-Object { $_.Line } | Sort-Object -Unique

if ($errors) {
  Write-Output "=== $($errors.Count) COMPILE ERROR(S) ==="
  $errors | ForEach-Object { Write-Output $_ }
  exit 1
}

Write-Output "=== BUILD OK ==="
if ($warns) {
  Write-Output "--- $($warns.Count) warning(s) ---"
  $warns | Select-Object -First 20 | ForEach-Object { Write-Output $_ }
}
exit 0
