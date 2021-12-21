param([Parameter(Position = 0, Mandatory = $true)][string]$solution, [Parameter(Mandatory = $true)][string]$binFolder)

$ErrorActionPreference = 'Stop'

Write-Host "Watching the solution for changes..."

$watcher=New-Object -TypeName IO.FileSystemWatcher -ArgumentList ([IO.Path]::GetDirectoryName($solution) | Resolve-Path) -Property @{
  IncludeSubdirectories = $true
}

try
{
  if ($watcher.Filters -ne $Null) { #supported in PS6+ only
    $watcher.Filters.Add("*.csproj")
    $watcher.Filters.Add("*.cs")
    $watcher.Filters.Add("*.sln")
    $watcher.Filters.Add("*.sql")
    $watcher.Filters.Add("*.targets")
  }

  do {
    $result=$watcher.WaitForChanged([IO.WatcherChangeTypes]::All, 1000) #provide a timeout to allow ctrl+c interupts
    if ($result.TimedOut) {continue}

    Write-Host "Solution has been changed, rebuild..."

    if (Test-Path $binfolder) {
      Remove-Item $binfolder -recurse -force
    }

    $build=Start-Process dotnet -ArgumentList "build $solution -c $Env:CONFIGURATION" -NoNewWindow -Wait -PassThru
    if ($build.ExitCode -ne 0) {
      Write-Host "Failed to rebuild the solution, watching for further changes..."
      continue
    }

    break
  } while($true)
}
finally { $watcher.Dispose() }