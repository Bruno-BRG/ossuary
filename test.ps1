<# Run the simulation and desktop-flow tests. #>
& (Join-Path $PSScriptRoot 'headless.ps1') test
exit $LASTEXITCODE
