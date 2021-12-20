param(
  [Parameter(Mandatory = $true)][string]$module,
  [Parameter(Mandatory = $true)][string]$method,
  [Parameter(Mandatory = $false)][string]$sessionId,
  [Parameter(Mandatory = $false)][object[]]$args
)
$ErrorActionPreference = 'Stop';

$uri="https://127.0.0.1:1986?module=$module&method=$method"
if ($sessionId -ne '') {
  $uri+="&sessionid=$sessionId"
}

if ($args -eq $null) {
  $args=@()
}

$response = Invoke-WebRequest -URI $uri -Body ($args | ConvertTo-Json) -Method 'POST' -SkipCertificateCheck