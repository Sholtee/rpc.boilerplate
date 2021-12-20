# build
FROM mcr.microsoft.com/dotnet/sdk:5.0.301-nanoserver-2004 as prep
SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

WORKDIR /download
RUN \
  Invoke-WebRequest https://netix.dl.sourceforge.net/project/mcwin32/mcwin32-build204-bin.zip -OutFile './mcwin32-build204-bin.zip';\
  Expand-Archive -Path './mcwin32-build204-bin.zip' -DestinationPath './mcwin32-build204/';

WORKDIR /app
COPY . .

ARG CONFIGURATION=Debug-NoTests

RUN dotnet publish ./SRC/MyApp.sln -c $Env:CONFIGURATION

# app
FROM mcr.microsoft.com/dotnet/runtime:5.0.13-windowsservercore-ltsc2022
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

COPY --from=prep download/mcwin32-build204 ./mcwin32-build204
RUN [Environment]::SetEnvironmentVariable('Path', $Env:Path+';C:\mcwin32-build204\', [System.EnvironmentVariableTarget]::Machine)

ARG\
  CERT_PATH\
  CERT_PWD

WORKDIR /app
COPY $CERT_PATH ./certificate.p12

RUN\
  Write-Host 'Registering certificate...';\
  $hash=(Import-PfxCertificate -FilePath './certificate.p12' Cert:\LocalMachine\My -Password (ConvertTo-SecureString -String $Env:CERT_PWD -Force -AsPlainText)).Thumbprint;\
  $appId=(New-Guid).ToString('B');\
  netsh http add sslcert ipport=127.0.0.1:1986 certhash=$hash appid=$appId;\
  Remove-Item -Path './certificate.p12' -Force;

COPY --from=prep app/BIN/net5.0/publish ./Bin
COPY --from=prep app/Migration ./Migration
COPY --from=prep app/BUILD/app/run.ps1 .

ENV\
  APP_ROOT=root@root.hu\
  APP_PWD=secret

ENTRYPOINT ./run.ps1

EXPOSE 1986