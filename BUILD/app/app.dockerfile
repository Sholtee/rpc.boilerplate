# build
FROM mcr.microsoft.com/dotnet/sdk:6.0-windowsservercore-ltsc2022 as prep
SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

WORKDIR /download
RUN\
  Invoke-WebRequest https://kumisystems.dl.sourceforge.net/project/mcwin32/mcwin32-build222-setup.exe -OutFile './mcwin32-build222-setup.exe';\
  #required for "watch"
  Invoke-WebRequest https://github.com/PowerShell/PowerShell/releases/download/v7.2.1/PowerShell-7.2.1-win-x64.msi -OutFile './PowerShell-7.2.1-win-x64.msi';

WORKDIR /app
COPY ./SRC ./SRC
COPY ./BUILD ./BUILD

ARG CONFIGURATION=Debug-NoTests

RUN dotnet build ./SRC/MyApp.sln -c $Env:CONFIGURATION

# app
FROM mcr.microsoft.com/dotnet/sdk:6.0-windowsservercore-ltsc2022
SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

COPY --from=prep download/mcwin32-build222-setup.exe ./mcwin32-build222-setup.exe

RUN\
  Write-Host 'Installing Midnight Commander...';\
  Start-Process './mcwin32-build222-setup.exe' -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /TASKS=\"modifypath\"' -Wait -NoNewWindow;\
  Remove-Item -Path './mcwin32-build222-setup.exe' -Force;

COPY --from=prep download/PowerShell-7.2.1-win-x64.msi ./PowerShell-7.2.1-win-x64.msi

RUN\
  Write-Host 'Installing PowerShell 7...';\
  Start-Process 'msiexec' -ArgumentList '/package PowerShell-7.2.1-win-x64.msi /quiet' -Wait -NoNewWindow;\
  Remove-Item -Path './PowerShell-7.2.1-win-x64.msi' -Force;

ARG\
  CERT_PATH\
  CERT_PWD

WORKDIR /app
COPY $CERT_PATH ./certificate.p12

RUN\
  Write-Host 'Registering certificate...';\
  $hash=(Import-PfxCertificate -FilePath './certificate.p12' Cert:\LocalMachine\My -Password (ConvertTo-SecureString -String $Env:CERT_PWD -Force -AsPlainText)).Thumbprint;\
  netsh http add sslcert ipport=0.0.0.0:1986 certhash=$hash appid=$((New-Guid).ToString('B'));\
  Remove-Item -Path './certificate.p12' -Force;

COPY --from=prep app/BIN/net6.0/ ./Bin
#COPY --from=prep app/BUILD/app/*.ps1 .
COPY --from=prep app/BUILD/app/run.ps1 .
COPY --from=prep app/BUILD/app/watch.ps1 .

ENV\
  APP_ROOT=root@root.hu\
  APP_PWD=secret\
  #for watch builds
  CONFIGURATION=Debug-NoTests

ENTRYPOINT ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';", "./run.ps1"]

EXPOSE 1986