# load-env.ps1
# Loads environment variables from a .env file (KEY=VALUE format) into the current session.
# Usage: . .\load-env.ps1 [path-to-env-file]
#        . .\load-env.ps1          (defaults to .env in current directory)

param(
    [string]$EnvFile = ".env"
)

if (-not (Test-Path $EnvFile)) {
    Write-Error "Env file not found: $EnvFile"
    return
}

$loaded = 0
foreach ($line in Get-Content $EnvFile) {
    # Skip blank lines and comments
    if ($line -match '^\s*$' -or $line -match '^\s*#') { continue }

    # Support KEY=VALUE format
    if ($line -match '^\s*([^=]+?)\s*=\s*"?([^"]*)"?\s*$') {
        $key   = $Matches[1].Trim()
        $value = $Matches[2].Trim()
        [System.Environment]::SetEnvironmentVariable($key, $value, 'Process')
        Write-Host "  Loaded: $key" -ForegroundColor Green
        $loaded++
    } else {
        Write-Warning "Skipped unrecognized line: $line"
    }
}

Write-Host "`n$loaded variable(s) loaded from '$EnvFile'." -ForegroundColor Cyan
