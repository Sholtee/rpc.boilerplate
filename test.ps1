$ErrorActionPreference = 'Stop';
$ProgressPreference = 'SilentlyContinue';

Start-Process docker -ArgumentList 'compose run --rm test_env pwsh -command ./BUILD/test/test.ps1' -WorkingDirectory './BUILD/test' -Wait -NoNewWindow;
Start-Process docker -ArgumentList 'compose down' -WorkingDirectory './BUILD/test' -Wait -NoNewWindow;