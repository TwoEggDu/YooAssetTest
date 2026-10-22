param(
    [Parameter(Mandatory)]
    [ValidateSet('Configure','BuildInitial','BuildV1','BuildV2','BuildHostPlayer','BuildOfflinePlayer','RebuildPlayers','RunEditorSmoke')]
    [string]$Task,
    [string]$UnityExe = 'D:\UnityEngine\NUnity\Developer\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $UnityExe)) { throw "Unity editor missing: $UnityExe" }
if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp\UnityLockfile')) {
    throw 'Close the editor for this project before running a batch task, or use its YooAsset Test menu.'
}
$logRoot = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$logFile = Join-Path $logRoot ($Task + '.log')
$unityArguments = '-batchmode -nographics -projectPath "{0}" -executeMethod DemoProjectSetup.{1} -logFile "{2}"' -f $projectRoot, $Task, $logFile
if ($Task -eq 'RunEditorSmoke') { $unityArguments += ' -yooSmoke' } else { $unityArguments += ' -quit' }
$unityTask = Start-Process -FilePath $UnityExe -ArgumentList $unityArguments -WindowStyle Hidden -Wait -PassThru
Write-Output "Unity exit=$($unityTask.ExitCode), log=$logFile"
if ($unityTask.ExitCode -ne 0) { throw "Unity task failed; inspect $logFile" }
