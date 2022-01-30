param([Parameter(Mandatory = $false)][switch]$forceRebuild)
$ErrorActionPreference = 'Stop'
$wd='./BUILD/app'
if ($forceRebuild) {
  Start-Process docker-compose -ArgumentList "build --force-rm app" -WorkingDirectory $wd -Wait -NoNewWindow
}

$proc = Start-Process docker-compose -ArgumentList "up --remove-orphans" -WorkingDirectory $wd -Wait -NoNewWindow -PassThru
if ($proc.ExitCode -ne 0) {
  # Engine v20.10.12 workaround
  Start-Process docker -ArgumentList "compose up --remove-orphans" -WorkingDirectory $wd -Wait -NoNewWindow
}
