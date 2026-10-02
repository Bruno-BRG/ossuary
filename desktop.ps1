<#
  Desktop pipeline: Tauri application, self-contained engine and validation.
  .\desktop.ps1 setup       installs a workspace-local .NET SDK and npm packages
  .\desktop.ps1 prepare     builds and publishes the self-contained engine
  .\desktop.ps1 test        simulation, desktop flow, frontend and Rust tests
  .\desktop.ps1 web         real engine in browser, for fast renderer development
  .\desktop.ps1 dev         native Tauri application
  .\desktop.ps1 build       Windows application plus NSIS installer
#>
param([ValidateSet('setup', 'prepare', 'test', 'web', 'dev', 'build')][string]$Mode = 'dev')
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
$desktop = Join-Path $repo 'desktop'
$cargoBin = Join-Path $env:USERPROFILE '.cargo\bin'
$localSdk = Join-Path $repo '.tools\dotnet'
$env:PATH = "$localSdk;$cargoBin;$env:PATH"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

function Invoke-Checked([string]$Executable, [string[]]$Arguments) {
  & $Executable @Arguments
  if ($LASTEXITCODE -ne 0) { throw "$Executable failed with exit code $LASTEXITCODE" }
}

if ($Mode -eq 'setup') {
  $installedLocalSdks = if (Test-Path (Join-Path $localSdk 'dotnet.exe')) { & (Join-Path $localSdk 'dotnet.exe') --list-sdks } else { @() }
  if (-not ($installedLocalSdks -match '^10\.')) {
    $toolDir = Join-Path $repo '.tools'
    New-Item -ItemType Directory -Force -Path $toolDir | Out-Null
    $installer = Join-Path $toolDir 'dotnet-install.ps1'
    Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
    & $installer -Channel 10.0 -InstallDir $localSdk -NoPath
    if (-not (Test-Path (Join-Path $localSdk 'dotnet.exe'))) { throw 'SDK installation failed.' }
  }
}
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$sdkVersions = & $dotnet --list-sdks
if (-not ($sdkVersions -match '^10\.')) { throw 'No .NET 10 SDK found. Run .\desktop.ps1 setup.' }
$npm = (Get-Command npm.cmd -ErrorAction Stop).Source
Push-Location $desktop
try {
  if ($Mode -eq 'setup' -or -not (Test-Path 'node_modules')) {
    if (Test-Path 'package-lock.json') { Invoke-Checked $npm @('ci') }
    else { Invoke-Checked $npm @('install') }
  }
  if ($Mode -eq 'setup') { Write-Output 'SDK and frontend ready. Run .\desktop.ps1 dev.'; return }

  $project = Join-Path $repo 'engine\Ossuary.Desktop\Ossuary.Desktop.csproj'
  Invoke-Checked $dotnet @('build', $project, '-c', 'Release', '--nologo')
  if ($Mode -eq 'web') { Invoke-Checked $npm @('run', 'dev'); return }

  $rustc = (Get-Command rustc -ErrorAction Stop).Source
  $triple = (& $rustc -vV | Select-String '^host:').ToString().Substring(6).Trim()
  if ($triple -ne 'x86_64-pc-windows-msvc') { throw "This pipeline currently targets Windows x64; found $triple." }
  $outDir = Join-Path $repo 'engine\Ossuary.Desktop\bin\publish\win-x64'
  Invoke-Checked $dotnet @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-o', $outDir, '--nologo')
  $binaries = Join-Path $desktop 'src-tauri\binaries'
  New-Item -ItemType Directory -Force -Path $binaries | Out-Null
  Copy-Item -LiteralPath (Join-Path $outDir 'ossuary-engine.exe') -Destination (Join-Path $binaries "ossuary-engine-$triple.exe") -Force
  if ($Mode -eq 'prepare') { Write-Output 'Self-contained engine ready.'; return }

  if ($Mode -eq 'test') {
    Invoke-Checked $dotnet @('run', '--project', (Join-Path $repo 'engine\Ossuary.Headless'), '-c', 'Release', '--', 'test')
    Invoke-Checked $npm @('test')
    Invoke-Checked $npm @('run', 'build')
    $cargo = (Get-Command cargo -ErrorAction Stop).Source
    Invoke-Checked $cargo @('test', '--manifest-path', (Join-Path $desktop 'src-tauri\Cargo.toml'))
  } elseif ($Mode -eq 'build') {
    Invoke-Checked $npm @('run', 'desktop:build')
    $appPath = Join-Path $desktop 'src-tauri\target\release\ossuary.exe'
    $process = Start-Process -FilePath $appPath -ArgumentList '--smoke-test' -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(30000)) { $process.Kill($true); throw 'Native startup timed out.' }
    if ($process.ExitCode -ne 0) { throw "Native startup failed with exit code $($process.ExitCode)." }
    Write-Output 'Native startup passed: WebView, bitmap font, painted frame and engine IPC.'
  } else {
    Invoke-Checked $npm @('run', 'desktop:dev')
  }
} finally { Pop-Location }
