docker build -t myapp . --progress=plain --build-arg MYSQL_PW=Alma1234 --build-arg CERT_PATH=./cert.self-signed/certificate.p12 --build-arg CERT_PW=cica --build-arg ROOT_PW=Password1
#mkdir bin
#docker save -o ./bin/myapp.tar myapp