<#
  Runs an editor method through the Unity CLI, waiting for the project lock.

  Several agents and the interactive editor contend for this project, and Unity
  refuses to open it twice. This waits for the lock, runs, and retries a few times
  because the lock can be stolen between the check and the launch.

  Usage:
    pwsh -File C:\dev\ossuary\unity-run.ps1 -Method Ossuary.EditorTools.OssuaryCli.CreateScene
#>
param(
  [Parameter(Mandatory = $true)][string]$Method,
  [int]$WaitSec = 900,
  [int]$Attempts = 4,
  [string]$Tag = 'run',
  # Screenshot and render passes need a real graphics device, which -nographics
  # suppresses. Everything else is happier (and faster) without one.
  [switch]$Graphics
)

$ErrorActionPreference = 'Stop'

$unity   = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe'
$project = 'C:\dev\ossuary\unity'
$lock    = Join-Path $project 'Temp\UnityLockfile'
$logDir  = Join-Path $env:LOCALAPPDATA 'Temp\opencode'
$log     = Join-Path $logDir "unity_$Tag.log"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
  Write-Output ("-- attempt {0}/{1}: waiting for the project lock --" -f $attempt, $Attempts)

  $deadline = (Get-Date).AddSeconds($WaitSec)
  while ((Get-Date) -lt $deadline) {
    if (-not (Get-Process -Name Unity -ErrorAction SilentlyContinue)) { break }
    Start-Sleep -Seconds 5
  }
  if (Get-Process -Name Unity -ErrorAction SilentlyContinue) {
    Write-Output 'still busy; giving up on this attempt'
    continue
  }

  # Only safe once nothing is running.
  if (Test-Path $lock) { Remove-Item $lock -Force -ErrorAction SilentlyContinue }
  Start-Sleep -Milliseconds 700

  if (Test-Path $log) { Remove-Item $log -Force }

  $unityArgs = @('-batchmode', '-quit', '-projectPath', $project, '-executeMethod', $Method, '-logFile', $log)
  if (-not $Graphics) { $unityArgs = @('-batchmode', '-nographics') + $unityArgs[1..($unityArgs.Count - 1)] }

  & $unity @unityArgs 2>&1 |
    Tee-Object -FilePath (Join-Path $logDir "unity_$Tag.stdout.txt") | Out-Null
  $code = $LASTEXITCODE

  if (Test-Path $log) {
    $lines = Get-Content $log
    $problems = $lines | Where-Object { $_ -match ': error CS|Shader error|^\s*Error:|Exception:|Aborting batchmode' }
    $interesting = $lines | Where-Object { $_ -match '\[cli\]' }

    if ($problems) {
      Write-Output '=== PROBLEMS ==='
      $problems | Sort-Object -Unique | Select-Object -First 30 | ForEach-Object { Write-Output $_ }
    }
    if ($interesting) { $interesting | ForEach-Object { Write-Output $_ } }

    # A real run always logs the compiler/import pipeline past this point.
    if ($lines.Count -gt 60) {
      Write-Output "exit=$code"
      exit $code
    }
  }

  Write-Output 'run did not get far enough; retrying'
  Start-Sleep -Seconds 5
}

Write-Output 'could not get a clean run'
exit 2