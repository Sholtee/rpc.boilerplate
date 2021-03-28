param ($logfolder="Artifacts")
if (test-path("BIN")) {
  remove-item "BIN" -recurse
}
get-childitem -filter "*.Tests.csproj" -recurse | foreach { 
  $logfile=[System.IO.Path]::ChangeExtension($_.name, 'xml')
  dotnet test $_.fullname --configuration:Debug --test-adapter-path:. --logger:"nunit;LogFilePath=$([System.IO.Path]::Combine(('.\' | resolve-path), $logfolder, $logfile))"
}