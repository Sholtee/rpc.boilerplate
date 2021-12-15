[![Build status](https://ci.appveyor.com/api/projects/status/nbuhmb49fu7mtvfd/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/rpc-boilerplate/branch/master) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/rpc-boilerplate)

## Requirements
- Docker (using [Windows containers](https://docs.docker.com/docker-for-windows/ ))
- PowerShell
## Running tests
1. `./test.ps1`
2. test results are placed in the `./Artifacts` folder
## Debugging tests
1. `./test.ps1 "name_of_asm_to_debug.dll"` (e.g.:"MyApp.Services.Cache.Tests.dll")
2. Attach the debugger to `container/testhost.exe` when the console is idle (tests won't run until a debugger is attached)