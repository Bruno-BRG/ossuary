<# Compatibility entry point for the independent test suite. #>
& (Join-Path $PSScriptRoot 'test.ps1')
exit $LASTEXITCODE
