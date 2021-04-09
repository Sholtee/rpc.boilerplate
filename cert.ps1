$ErrorActionPreference = "Stop"

function Create-SelfSignedCertificate([Parameter(Mandatory = $true)][String] $outputDir, [Parameter(Mandatory = $true)][String] $password) {
	New-Item -ItemType "directory" -Path $OutputDir | Out-Null

	choco install OpenSSL.Light -y --version 1.1.1 --install-arguments="'/DIR=.\Vendor'"
	try {
		$keyPem = (JOIN-PATH $outputDir "key.pem")
		$certPem = (JOIN-PATH $outputDir "certificate.pem")

		openssl req -newkey rsa:2048 -nodes -keyout $keyPem -x509 -days 365 -out $certPem -batch
		openssl pkcs12 -inkey $keyPem -in $certPem -export -out (JOIN-PATH $outputDir "certificate.p12") -password "pass:$($password)"
	} finally {
		choco uninstall OpenSSL.Light -y --version 1.1.1
	}
}