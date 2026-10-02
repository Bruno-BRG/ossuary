<# Tests, ASCII dumps and soak runs using the independent .NET engine. No desktop window required. #>
param(
  [string]$Mode = 'test',
  [switch]$Rebuild,
  [Parameter(ValueFromRemainingArguments = $true)][string[]]$Rest = @()
)
$ErrorActionPreference = 'Stop'
$localDotnet = Join-Path $PSScriptRoot '.tools\dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$project = Join-Path $PSScriptRoot 'engine\Ossuary.Headless'
# MSBuild is incremental and always notices modified source files.
& $dotnet build $project -c Release --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$dll = Join-Path $project 'bin\Release\net10.0\Ossuary.Headless.dll'
& $dotnet $dll $Mode @Rest
exit $LASTEXITCODE
