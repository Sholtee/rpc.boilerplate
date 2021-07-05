param(
  [Parameter(Mandatory=$true)]
  [string]
  $MYSQL_PW,

  [ValidatePattern('^[A-Za-z0-1 .\\/]+\.p12$')]
  [string]
  $CERT_PATH='./cert.self-signed/certificate.p12',

  [string]
  $CERT_PW='cica',

  [Parameter(Mandatory=$true)]
  [string]
  $ROOT_PW,

  [Parameter(Mandatory=$true)]
  [string]
  $HOST_NAME,

  [string]
  $BIN_FOLDER='./BIN'
)

docker build -t myapp . --progress=plain --build-arg MYSQL_PW=$MYSQL_PW --build-arg CERT_PATH=$CERT_PATH --build-arg CERT_PW=$CERT_PW --build-arg ROOT_PW=$ROOT_PW --build-arg CUSTOM_SQL="CREATE USER 'root'@'$($HOST_NAME)' IDENTIFIED BY '$($MYSQL_PW)';GRANT ALL ON *.* TO 'root'@'$($HOST_NAME)';"

if (Test-Path $BIN_FOLDER) {
  Remove-Item $BIN_FOLDER -Recurse -Force
}
New-Item -Path $BIN_FOLDER -Force -ItemType "Directory" | Out-Null

$container_id=docker create myapp
docker cp "$($container_id):/mysql-8.0.25-winx64/data/" "$($BIN_FOLDER)/mysql-data"
docker rm $container_id | Out-Null

docker save -o "$($BIN_FOLDER)/myapp.tar" myapp