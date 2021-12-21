$ErrorActionPreference = 'Stop'

function InvokeServer([Parameter(Position = 0, Mandatory = $false)][string]$argList) {
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

function InvokeServer-RedirectInput() {
  $pinfo=New-Object System.Diagnostics.ProcessStartInfo
  $pinfo.FileName="./BIN/MyApp.Server.exe"
  $pinfo.RedirectStandardInput=$true
  $pinfo.UseShellExecute=$false
  $p=New-Object System.Diagnostics.Process
  $p.StartInfo=$pinfo
  $p.Start() | Out-Null
  return $p
}

do {
  $status=InvokeServer "status"

  if ($status.StartsWith("not installed", [StringComparison]::OrdinalIgnoreCase)) {
    Write-Host "Installing the app..."
    InvokeServer "install -User '$($Env:APP_ROOT)' -PasswordVariable APP_PWD"
  } elseif ($status.StartsWith("installed", [StringComparison]::OrdinalIgnoreCase)) {
    Write-Host "Running the migration scripts..."
    InvokeServer "migrate"
  } else {
    Write-Warning "Unknown status: $($status)"
    Exit -1
  }

  Write-Host "Starting the server..."

  if (!(Test-Path "./Dev")) {  
    InvokeServer
    return
  }

  $proc=InvokeServer-RedirectInput
  ./watch.ps1 "./Dev/SRC/MyApp.sln" -BinFolder "./Dev/BIN"
  $proc.StandardInput.Write([char]3)
  $proc.WaitForExit()
  Remove-Item "./BIN" -recurse -force
  Copy-Item -Path "./Dev/BIN/net5.0/*" -Destination "./Bin" -Recurse   
} while($true)