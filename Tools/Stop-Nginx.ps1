$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$serverRoot = Join-Path $projectRoot 'Server'
$nginxExe = Join-Path $PSScriptRoot 'nginx-1.30.6\nginx.exe'
$nginxPidFile = Join-Path $serverRoot 'logs\nginx.pid'
if (-not (Test-Path -LiteralPath $nginxPidFile)) { Write-Output 'This project nginx is not running.'; return }
$nginxPid = 0
if (-not [int]::TryParse([string](Get-Content -LiteralPath $nginxPidFile -Raw), [ref]$nginxPid) -or $nginxPid -le 0) {
    Write-Output 'No active project nginx PID.'
    return
}
$nginxProcess = Get-Process -Id $nginxPid -ErrorAction SilentlyContinue
if (-not $nginxProcess) { Write-Output 'This project nginx is already stopped.'; return }
if ($nginxProcess.Path -ne $nginxExe) { throw 'PID belongs to a different executable; refusing to signal it.' }
& $nginxExe -p ($serverRoot.Replace('\', '/') + '/') -c nginx.conf -s quit
if ($LASTEXITCODE -ne 0) { throw 'nginx quit failed.' }
Write-Output "Graceful quit sent to this project's nginx (PID $nginxPid)."
