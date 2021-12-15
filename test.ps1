param([Parameter(Position = 0, Mandatory = $false)][string]$target)
$ErrorActionPreference = 'Stop';

$remoteCommand='./BUILD/test/test.ps1';
if ($target -ne '') {
    $remoteCommand+=" '$target'"
}

$proc = Start-Process docker-compose -ArgumentList "run test_env pwsh -command $remoteCommand" -WorkingDirectory './BUILD/test' -Wait -NoNewWindow -PassThru;
Write-Host "Docker returned $($proc.ExitCode)";
Start-Process docker-compose -ArgumentList 'down' -WorkingDirectory './BUILD/test' -Wait -NoNewWindow;