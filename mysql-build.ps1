param(
  [Parameter(Mandatory=$true)]
  [string]
  $MYSQL_PWD,

  [Parameter(Mandatory=$true)]
  [string]
  $MYSQL_DB,

  [string]
  $BIN_FOLDER='./BIN'
)

docker build -t mysql . --file mysql.dockerfile --progress=plain --rm --build-arg MYSQL_PWD=$MYSQL_PWD --build-arg MYSQL_DB=$MYSQL_DB

if (Test-Path $BIN_FOLDER) {
  Remove-Item $BIN_FOLDER -Recurse -Force
}
New-Item -Path $BIN_FOLDER -Force -ItemType "Directory" | Out-Null

$container_id=docker create mysql
docker cp "$($container_id):/mysql-8.0.25-winx64/data/" "$($BIN_FOLDER)/mysql-data"
docker rm $container_id | Out-Null

docker save -o "$($BIN_FOLDER)/mysql.tar" mysql