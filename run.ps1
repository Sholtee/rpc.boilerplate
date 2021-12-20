$ErrorActionPreference = 'Stop'
Start-Process docker-compose -ArgumentList "up" -WorkingDirectory './BUILD/app' -Wait -NoNewWindow