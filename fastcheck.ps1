<# Quick type-check of the independent C# engine and desktop frontend. #>
$ErrorActionPreference = 'Stop'
$localDotnet = Join-Path $PSScriptRoot '.tools\dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
& $dotnet build (Join-Path $PSScriptRoot 'engine\Ossuary.Desktop') -c Release --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Push-Location (Join-Path $PSScriptRoot 'desktop')
try { & .\node_modules\.bin\tsc.cmd --noEmit; $result = $LASTEXITCODE }
finally { Pop-Location }
exit $result
