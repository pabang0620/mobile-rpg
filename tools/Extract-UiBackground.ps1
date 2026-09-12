param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [string]$PythonPath = 'python'
)

$scriptPath = Join-Path $PSScriptRoot 'extract_ui_background.py'
& $PythonPath $scriptPath (Resolve-Path -LiteralPath $InputPath).Path ([IO.Path]::GetFullPath($OutputPath))
if ($LASTEXITCODE -ne 0) { throw "UI background extraction failed with exit code $LASTEXITCODE" }
