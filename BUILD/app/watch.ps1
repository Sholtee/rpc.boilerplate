param([Parameter(Position = 0, Mandatory = $true)][string]$solution, [Parameter(Mandatory = $true)][string]$binFolder)

$ErrorActionPreference = 'Stop'

Write-Host "Watching the solution for changes..."

$watcher=New-Object -TypeName IO.FileSystemWatcher -ArgumentList ([IO.Path]::GetDirectoryName($solution) | Resolve-Path) -Property @{
  IncludeSubdirectories = $true
}

try
{
  $watcher.Filters.Add("*.csproj")
  $watcher.Filters.Add("*.cs")
  $watcher.Filters.Add("*.sln")
  $watcher.Filters.Add("*.sql")
  $watcher.Filters.Add("*.targets")

  do {
    $result=$watcher.WaitForChanged([IO.WatcherChangeTypes]::All, 1000) #provide a timeout to allow ctrl+c interupts
    if ($result.TimedOut) {continue}

    Write-Host "Solution has been changed, rebuild..."

    if (Test-Path $binfolder) {
      Remove-Item $binfolder -recurse -force
    }

    dotnet build $solution -c $Env:CONFIGURATION
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Failed to rebuild the solution, watching for further changes..."
      continue
    }

    break
  } while($true)
}
finally { $watcher.Dispose() }