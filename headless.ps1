<#
  Builds and runs Ossuary.Core headlessly, without Unity and without the project lock.

  Ossuary.Core (the simulation plus the whole ASCII interface) has no UnityEngine
  references, so it can be compiled against Unity's bundled .NET 6 runtime and run
  directly. That makes the test suite and the frame dumps available even while the
  editor has the project open.

  Usage:
    pwsh -File C:\dev\ossuary\headless.ps1 test
    pwsh -File C:\dev\ossuary\headless.ps1 dump
    pwsh -File C:\dev\ossuary\headless.ps1 soak 50 500
#>
param(
  [string]$Mode = 'test',
  [switch]$Rebuild,
  [string[]]$Rest = @()
)

$ErrorActionPreference = 'Stop'

$editor   = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor'
$dotnet   = Join-Path $editor 'Data\NetCoreRuntime\dotnet.exe'
$fxDir    = Join-Path $editor 'Data\NetCoreRuntime\shared\Microsoft.NETCore.App'
$csc      = Join-Path $editor 'Data\DotNetSdkRoslyn\csc.dll'
$project  = 'C:\dev\ossuary\unity'
$outDir   = Join-Path $env:LOCALAPPDATA 'Temp\opencode\headless'
$exe      = Join-Path $outDir 'ossuary.exe'
$dll      = Join-Path $outDir 'ossuary.dll'

if (-not (Test-Path $dotnet)) { Write-Output "dotnet not found: $dotnet"; exit 2 }

# Pick the single installed shared framework version.
$fx = Get-ChildItem $fxDir -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
if (-not $fx) { Write-Output "no shared framework under $fxDir"; exit 2 }

$needsBuild = $Rebuild -or -not (Test-Path $exe)
if ($needsBuild) {
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null

  $sources = @()
  $sources += Get-ChildItem (Join-Path $project 'Assets\Scripts\Core') -Recurse -File -Filter *.cs | ForEach-Object { $_.FullName }
  $sources += Get-ChildItem (Join-Path $project 'Assets\Tests') -File -Filter *.cs | ForEach-Object { $_.FullName }
  $sources += Join-Path $PSScriptRoot 'tools\HeadlessMain.cs'

  # The shared framework directory also holds native images (clrjit, coreclr, the
  # api-ms-win-crt shims). csc cannot reference those, so keep only real assemblies.
  $refs = @()
  Get-ChildItem $fx.FullName -Filter *.dll | ForEach-Object {
    try { [void][System.Reflection.AssemblyName]::GetAssemblyName($_.FullName); $refs += $_.FullName } catch { }
  }
  if ($refs.Count -eq 0) { Write-Output 'no managed assemblies found in the shared framework'; exit 2 }

  $rspFile = Join-Path $outDir 'build.rsp'
  $lines = @(
    '-nostdlib', '-noconfig', '-target:exe', '-langversion:9.0', '-nologo',
    '-define:NETSTANDARD2_1', '-define:NETSTANDARD',
    '-nowarn:1701,1702,CS0169,CS0414,CS0649',
    "-out:`"$dll`""
  )
  $lines += $refs | ForEach-Object { "-r:`"$_`"" }
  $lines += $sources | ForEach-Object { "`"$_`"" }
  Set-Content -Path $rspFile -Value $lines -Encoding UTF8

  $log = Join-Path $outDir 'csc.log'
  & $dotnet exec $csc "@$rspFile" 2>&1 | Tee-Object -FilePath $log | Out-Null
  if ($LASTEXITCODE -ne 0) {
    Write-Output '=== BUILD FAILED ==='
    Get-Content $log | Where-Object { $_ -match ': (error|warning) CS' } | Sort-Object -Unique | ForEach-Object { Write-Output $_ }
    exit 1
  }
  Write-Output "-- built ($($sources.Count) sources, runtime $(($fx.Name))) --"
}

# A runtimeconfig is what turns the dll into something dotnet can execute.
$rc = @"
{
  "runtimeOptions": {
    "tfm": "net6.0",
    "framework": { "name": "Microsoft.NETCore.App", "version": "$($fx.Name)" }
  }
}
"@
Set-Content -Path (Join-Path $outDir 'ossuary.runtimeconfig.json') -Value $rc -Encoding UTF8
Copy-Item $dll $exe -Force -ErrorAction SilentlyContinue

$args = @($dll, $Mode) + $Rest
& $dotnet $args
exit $LASTEXITCODE