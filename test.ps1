param([switch]$noNewWindow)

$ErrorActionPreference = 'Stop';

$proc = Start-Process docker -ArgumentList 'compose run --rm test_env pwsh -command ./BUILD/test/test.ps1' -WorkingDirectory './BUILD/test' -Wait -NoNewWindow:$noNewWindow -PassThru;
Write-Host "Docker returned $($proc.ExitCode)";
Start-Process docker -ArgumentList 'compose down' -WorkingDirectory './BUILD/test' -Wait -NoNewWindow:$noNewWindow;