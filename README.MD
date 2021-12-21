[![Build status](https://ci.appveyor.com/api/projects/status/nbuhmb49fu7mtvfd/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/rpc-boilerplate/branch/master) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/rpc-boilerplate)

## Requirements
- Docker (using [Windows containers](https://docs.docker.com/docker-for-windows/ ))
- PowerShell
## Running tests
1. `./test.ps1`
2. test results are placed in the `./Artifacts` folder
## Debugging tests
1. `./test.ps1 "test_project.csproj"` (e.g.:"Cache.Tests.csproj")
2. When the console is idle (tests won't run until a debugger is present), attach the debugger to `testhost.exe` in the `test_env_*` container
## Running the server
`./run.ps1 [-ForceRebuild]`

If any change is made in the `SRC` folder while the container is running the system rebuilds the server and restarts the container.