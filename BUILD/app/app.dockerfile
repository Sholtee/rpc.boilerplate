# build
FROM mcr.microsoft.com/dotnet/sdk:5.0.301-nanoserver-2004 as build

WORKDIR /app
COPY . .

ARG CONFIGURATION=Debug

RUN dotnet publish ./SRC/MyApp.sln -c %CONFIGURATION%

# app
FROM mcr.microsoft.com/dotnet/runtime:5.0-windowsservercore-ltsc2019
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

ARG \
  CERT_PATH\
  CERT_PWD

WORKDIR /app
COPY $CERT_PATH ./certificate.p12

RUN \
  Write-Host 'Registering certificate...';\
  $hash=(Import-PfxCertificate -FilePath './certificate.p12' Cert:\LocalMachine\My -Password (ConvertTo-SecureString -String $ENV:CERT_PWD -Force -AsPlainText)).Thumbprint;\
  $appId=(New-Guid).ToString('B');\
  netsh http add sslcert ipport=127.0.0.1:1986 certhash=$hash appid=$appId;\
  Remove-Item -Path './certificate.p12' -Force;

COPY --from=build app/BIN/net5.0/publish ./Bin
COPY --from=build app/Migration ./Migration
COPY --from=build app/BUILD/app/run.ps1 .

ENTRYPOINT ./run.ps1

EXPOSE 1986