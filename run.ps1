param([Parameter(Mandatory = $false)][switch]$forceRebuild)
$ErrorActionPreference = 'Stop'
$wd='./BUILD/app'
if ($forceRebuild) {
  Start-Process docker-compose -ArgumentList "build --force-rm app" -WorkingDirectory $wd -Wait -NoNewWindow
}
Start-Process docker-compose -ArgumentList "up --remove-orphans" -WorkingDirectory $wd -Wait -NoNewWindow