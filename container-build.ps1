docker build -t myapp . --progress=plain --build-arg MYSQL_PW=Alma1234 --build-arg CERT_PATH=./cert.self-signed/certificate.p12 --build-arg CERT_PW=cica --build-arg ROOT_PW=Password1
if (!(Test-Path BIN)) {
  New-Item -Path BIN -Force -ItemType "Directory" | Out-Null
}
docker save -o ./BIN/myapp.tar myapp