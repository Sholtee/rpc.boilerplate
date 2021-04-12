[![Build status](https://ci.appveyor.com/api/projects/status/nbuhmb49fu7mtvfd/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/rpc-boilerplate/branch/master) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/rpc-boilerplate)

## Requirements
- Redis (optional) 
- MySql

## Installation
0. Clone this repo, rename `MyApp.sln` and set the `AppName` property in `SRC\Common.targets`, `SRC\Tests.Common.targets`
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
3. Call `BIN\net5.0\Server.exe -install -user root@root.hu -password Password1` to:
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
- Otherwise: `BIN\net5.0\Server.exe`

## Running tests
1. Create the `config.test.json` file under the `src\TestsBase\` directory (should look like [this](#installation))
2. `.\test.ps1`
3. Test results are placed in the `.\Artifacts` folder