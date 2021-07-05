param(
  [int]
  $MYSQL_PORT=13306,

  [string]
  $BIN_FOLDER='./BIN'
)

docker load -i BIN/myapp.tar
docker run -it -p "$($MYSQL_PORT):3306" --mount "type=bind,source=$(Resolve-Path $BIN_FOLDER/mysql-data),target=c:/mysql-8.0.25-winx64/data/" --rm myapp