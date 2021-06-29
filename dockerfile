# prep
FROM mcr.microsoft.com/powershell:lts-nanoserver-2004 as prep
WORKDIR /src
COPY . .
RUN pwsh -command "\
  $ErrorActionPreference='Continue';\
  Get-ChildItem './*' -Include ('*.sln', '*.csproj', '*.targets') -Recurse | ForEach {\
    $rel=[System.IO.Path]::GetRelativePath((Get-Location), $_.FullName);\
    Write-Host $rel;\
    $dst=Join-Path './../project_files' $rel;\
    New-Item -ItemType 'File' -Path $dst -Force | Out-Null;\
    Copy-Item $_.FullName -Destination $dst;\
  }"

# build
FROM mcr.microsoft.com/dotnet/sdk:5.0.301-nanoserver-2004 as build
WORKDIR /app
COPY --from=prep /project_files .
RUN dotnet restore ./SRC/MyApp.sln
ARG MYSQL_PW
COPY --from=prep /src .
RUN pwsh -command "\
  $ErrorActionPreference='Stop';\
  @{\
    Server=@{\
      Host='https://localhost:1986/api/';\
      SessionTimeoutInMinutes=10;\
      AllowedOrigins=@('http://localhost:8080');\
    };\
    ConnectionString='Server=localhost;Port=3306;Database=myapp;UId=root;PWd=%MYSQL_PW%';\
    Redis=@{\
      Host='localhost';\
      Port=6379;\
    }\
  } | ConvertTo-JSON | Set-Content -Path './SRC/Services/Config/config.live.json';"
RUN dotnet publish ./SRC/MyApp.sln -c Release
#RUN pwsh -command "Get-Content -Path ./BIN/net5.0/publish/config.json"

# app
FROM mcr.microsoft.com/dotnet/runtime:5.0-windowsservercore-ltsc2019
ARG CERT_PATH
ARG CERT_PW
ARG ROOT_PW
ENV USER_INTERACTIVE=true
WORKDIR /app
COPY --from=build /app/BIN/net5.0/publish .
COPY $CERT_PATH ./certificate.p12
RUN powershell -command "\
  $ErrorActionPreference='Stop';\
  $hash=(Import-PfxCertificate -FilePath './certificate.p12' Cert:\LocalMachine\My -Password (ConvertTo-SecureString -String '%CERT_PW%' -Force -AsPlainText)).Thumbprint;\
  $appId=(New-Guid).ToString('B');\
  netsh http add sslcert ipport=127.0.0.1:1986 certhash=$hash appid=$appId;\
  Remove-Item -Path './certificate.p12';"
RUN MyApp.Server.exe -install -noservice -user root@root.hu -password %ROOT_PW%
EXPOSE 1986