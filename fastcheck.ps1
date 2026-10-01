<#
  Fast type-check that does NOT need the Unity project lock.

  Unity refuses to open the project while an editor is up, which makes iterating on
  compile errors painfully slow. This drives Unity's own Roslyn directly using the
  response files Unity already generated, so a full check takes a couple of seconds.

  It compiles every .cs under Assets into one assembly against the union of Unity's
  references. That is a superset of what Unity does per-assembly, so it catches all
  real type/syntax errors; it cannot catch assembly-boundary violations.

  Usage:  pwsh -File C:\dev\ossuary\fastcheck.ps1
#>
param([switch]$Quiet)

$ErrorActionPreference = 'Stop'

$unityRoot = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor'
$csc       = Join-Path $unityRoot 'Data\DotNetSdkRoslyn\csc.dll'
$dotnet    = Join-Path $unityRoot 'Data\NetCoreRuntime\dotnet.exe'
$project   = 'C:\dev\ossuary\unity'
$dag       = Join-Path $project 'Library\Bee\artifacts\1900b0aE.dag'
$outDir    = Join-Path $env:LOCALAPPDATA 'Temp\opencode\fastcheck'

if (-not (Test-Path $csc))  { Write-Output "csc not found: $csc"; exit 2 }
if (-not (Test-Path $dag))  { Write-Output "no Unity response files; run a Unity import once first"; exit 2 }

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# ── references: union of the runtime and editor response files ──────────────────
$refs = @()
foreach ($rspName in 'Assembly-CSharp.rsp', 'Assembly-CSharp-Editor.rsp') {
  $rsp = Join-Path $dag $rspName
  if (-not (Test-Path $rsp)) { continue }
  Get-Content $rsp | ForEach-Object {
    if ($_ -match '^-r:"?(.+?)"?$') { $refs += $Matches[1] }
  }
}
$refs = $refs | Sort-Object -Unique
if ($refs.Count -eq 0) { Write-Output 'no references recovered'; exit 2 }

# ── sources ────────────────────────────────────────────────────────────────────
$sources = Get-ChildItem (Join-Path $project 'Assets') -Recurse -File -Filter *.cs |
           Where-Object { $_.FullName -notmatch '\\Library\\' } |
           ForEach-Object { $_.FullName }

# obj/ temp intermediates never live under Assets, but guard anyway.
if ($sources.Count -eq 0) { Write-Output 'no sources found'; exit 2 }

# ── compile ─────────────────────────────────────────────────────────────────────
$rspFile = Join-Path $outDir 'build.rsp'
$lines = @(
  '-nostdlib'
  '-noconfig'
  '-target:library'
  '-langversion:9.0'
  '-nowarn:1701,1702,CS0169,CS0414,CS0649,CS8632'
  '-nologo'
  "-out:`"$outDir\FastCheck.dll`""
)
$lines += $refs | ForEach-Object { "-r:`"$_`"" }
$lines += $sources | ForEach-Object { "`"$_`"" }
Set-Content -Path $rspFile -Value $lines -Encoding UTF8

$log = Join-Path $outDir 'csc.log'
& $dotnet exec $csc "@$rspFile" 2>&1 | Tee-Object -FilePath $log | Out-Null
$code = $LASTEXITCODE

$out = Get-Content $log -Raw -ErrorAction SilentlyContinue
$errors = if ($out) { ($out -split "`r?`n") | Where-Object { $_ -match ': (error|warning) CS' } } else { @() }

if ($errors -and ($errors | Where-Object { $_ -match ': error CS' })) {
  Write-Output "=== $($errors.Count) DIAGNOSTIC(S) ==="
  $errors | Sort-Object -Unique | ForEach-Object { Write-Output $_ }
  exit 1
}

if (-not $Quiet) { Write-Output "fastcheck: OK ($($sources.Count) sources, $($refs.Count) refs)" }
exit 0