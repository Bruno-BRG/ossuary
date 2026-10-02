<# Full desktop validation: engine, frontend and Rust shell, with their tests. #>
& (Join-Path $PSScriptRoot 'desktop.ps1') test
exit $LASTEXITCODE
