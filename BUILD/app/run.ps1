function invoke-server([Parameter(Position = 0, Mandatory = $false)][string]$argList) {
  $pinfo=New-Object System.Diagnostics.ProcessStartInfo
  $pinfo.FileName="./BIN/MyApp.Server.exe"
  $pinfo.RedirectStandardError=$true
  $pinfo.RedirectStandardOutput=$true
  $pinfo.UseShellExecute=$false
  $pinfo.Arguments=$argList
  $p=New-Object System.Diagnostics.Process
  $p.StartInfo=$pinfo
  $p.Start() | Out-Null
  $p.WaitForExit()

  if ($p.ExitCode -ne 0) {
    Write-Warning $p.StandardError.ReadToEnd()
    Exit $p.ExitCode
  }
  return $p.StandardOutput.ReadToEnd()
}

$status=invoke-server "status"

if ($status.StartsWith("not installed", [StringComparison]::OrdinalIgnoreCase)) {
  Write-Host "Installing the app..."
  invoke-server "install -User '$($Env:APP_ROOT)' -PasswordVariable APP_PWD"
} elseif ($status.StartsWith("installed", [StringComparison]::OrdinalIgnoreCase)) {
  Write-Host "Running the migration scripts..."
  invoke-server "migrate"
} else {
  Write-Warning "Unknown status: $($status)"
  Exit -1
}

Write-Host "Starting the server..."
& "./BIN/MyApp.Server.exe"