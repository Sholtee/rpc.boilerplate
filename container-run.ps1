docker load -i BIN/myapp.tar
docker run -it  --mount type=bind,source="$(Resolve-Path ./BIN/mysql-data)",target="c:/mysql-8.0.25-winx64/data/" --rm myapp