param([Parameter(Position = 0, Mandatory = $false)][string]$target, [Parameter(Mandatory = $false)][string]$logFile)
$ErrorActionPreference = 'Stop';

$remoteCommand='./BUILD/test/test.ps1';
if ($target -ne '') {
  $remoteCommand+=" '$target'"
}
if ($logFile -ne '') {
  $remoteCommand+=" | Out-File -path (New-Item -path '$logFile' -force)"
}

$proc = Start-Process docker-compose -ArgumentList "run test_env pwsh -command $remoteCommand" -WorkingDirectory './BUILD/test' -Wait -NoNewWindow -PassThru;
Write-Host "Docker returned $($proc.ExitCode)";
Start-Process docker-compose -ArgumentList 'down' -WorkingDirectory './BUILD/test' -Wait -NoNewWindow;