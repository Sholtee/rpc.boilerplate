param ($root=".")

$root=$root | Resolve-Path
$binfolder=[System.IO.Path]::Combine($root, "BIN")
$logfolder=[System.IO.Path]::Combine($root, "Artifacts")

if (Test-Path($binfolder)) {
  Remove-Item $binfolder -recurse
}

Get-ChildItem -path $root -filter "*.sln" -recurse | foreach { 
  dotnet build $_.FullName --configuration:Debug
}

Get-ChildItem -path $root -filter "*.Tests.csproj" -recurse | foreach { 
  $logfile=[System.IO.Path]::ChangeExtension($_.name, 'xml')
  dotnet test $_.FullName --no-build --test-adapter-path:. --logger:"nunit;LogFilePath=$([System.IO.Path]::Combine($root, $logfolder, $logfile))"
}