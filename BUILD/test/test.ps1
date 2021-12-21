param ([Parameter(Position = 0, Mandatory = $false)][string]$target)

$ErrorActionPreference = "Stop"

$root="." | Resolve-Path
$binfolder=[System.IO.Path]::Combine($root, "BIN")

if (Test-Path $binfolder) {
  Remove-Item $binfolder -recurse -force
}

Get-ChildItem -path $root -filter "*.sln" -recurse | foreach { 
  dotnet build $_.FullName --configuration:Debug
}

$logfolder=[System.IO.Path]::Combine($root, "Artifacts")

if ($target -ne "") {
  Write-Host "If the console is idle attach the debugger to container/testhost.exe"
  $filter=$target
  $Env:TARGET=$target
}
else { $filter="*.Tests.csproj" }

Get-ChildItem -path $root -filter $filter -recurse | foreach { 
  $logfile=[System.IO.Path]::ChangeExtension($_.name, 'xml')
  dotnet test $_.FullName --no-build --test-adapter-path:. --logger:"nunit;LogFilePath=$([System.IO.Path]::Combine($root, $logfolder, $logfile))"
}