$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$serverRoot = Join-Path $projectRoot 'Server'
$nginxExe = Join-Path $PSScriptRoot 'nginx-1.30.6\nginx.exe'
if (-not (Test-Path -LiteralPath $nginxExe)) { throw "nginx is missing: $nginxExe" }
New-Item -ItemType Directory -Path (Join-Path $serverRoot 'logs'),(Join-Path $serverRoot 'temp') -Force | Out-Null
$nginxPrefix = $serverRoot.Replace('\', '/') + '/'
$nginxPidFile = Join-Path $serverRoot 'logs\nginx.pid'
if (Test-Path -LiteralPath $nginxPidFile) {
    $nginxPid = 0
    $pidValid = [int]::TryParse([string](Get-Content -LiteralPath $nginxPidFile -Raw), [ref]$nginxPid)
    $existing = if ($pidValid -and $nginxPid -gt 0) { Get-Process -Id $nginxPid -ErrorAction SilentlyContinue }
    if ($existing -and $existing.Path -eq $nginxExe) {
        Write-Output "This project's nginx is already running (PID $nginxPid)."
        return
    }
}
$portOwner = Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue
if ($portOwner) { throw "Port 8080 is occupied. No existing process was stopped." }
& $nginxExe -p $nginxPrefix -c nginx.conf -t
if ($LASTEXITCODE -ne 0) { throw 'nginx configuration check failed.' }
$nginxArguments = '-p "{0}" -c nginx.conf' -f $nginxPrefix
Start-Process -FilePath $nginxExe -ArgumentList $nginxArguments -WorkingDirectory $serverRoot -WindowStyle Hidden
Start-Sleep -Milliseconds 500
$health = Invoke-WebRequest -Uri 'http://127.0.0.1:8080/health.txt' -TimeoutSec 5
Write-Output "nginx HTTP $($health.StatusCode): http://127.0.0.1:8080/CDN/PC/v1.0/"
