[![Build status](https://ci.appveyor.com/api/projects/status/nbuhmb49fu7mtvfd/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/rpc-boilerplate/branch/master) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/rpc-boilerplate)

## Requirements
- Redis (optional) 
- MySql
## Installation
0. Clone this repo, rename `MyApp.sln`, set the `AppName` property in `SRC\Common.targets` and `SRC\Tests.Common.targets`
1. Create the `config.[dev|live].json` file under the `src\Services\Config\` directory (`.dev.json` is used in *Debug* configuration, `.live` in *Release*):
	```json
	{
      "Server": {
        "Host": "http://localhost:1986/api/",
        "SessionTimeoutInMinutes": 10,
        "AllowedOrigins": [ "http://127.0.0.1:8080" ]
      },
	  "ConnectionString": "Server=localhost;Port=3306;Database=myapp;UId=root;PWd=cica",
	  "Redis": {
        "Host": "localhost",
        "Port": 6379
	  }
	}
	```
2. `dotnet build .\SRC\MyApp.sln -c Debug|Release`
3. Call `BIN\net5.0\MyApp.Server.exe -install -user root@root.hu -password Password1` to:
	- Initialize the database
	- Create the root user
	- Install the server as a service (Windows only)
	
	Notes: 
	- The `-user` and `-password` switches can be used together with `-install` only
	- Use the `-noservice` switch if you don't want to install the server as a service
4. To enable SSL:
   ```ps
   PS .(".\cert.ps1")
   PS Create-SelfSignedCertificate
   PS Bind-Certificate
   ```
## Starting the server
- If the server was installed as a service: `sc start MyApp`
- Otherwise: `BIN\net5.0\MyApp.Server.exe`
## Running tests
1. Create the `config.test.json` file under the `src\TestsBase\` directory (should look like [this](#installation))
2. `.\test.ps1`
3. Test results are placed in the `.\Artifacts` folder
## Build with Docker
1. Switch to [Windows containers](https://docs.docker.com/docker-for-windows/ )
2. Replace the './cert.self-signed/certificate.p12' file with your own (it will be used to secure the remote API calls)
3. Execute the `./container-build.ps1` script. It will
   - create and save a new image of your app. The image should be ready to run (includes the initialized database and app) 
   - export the MySQL data directory (`./BIN/mysql-data` by default). You may mount it later
## Running with Docker
Simply run the `./container-run.ps1` script. It loads the app image, binds the MySQL data directory and runs your app. The API will be available at **https://127.0.0.1:1986**. MySQL should be also available from outside the container at **127.0.0.1:13306**.