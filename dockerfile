# prep
FROM mcr.microsoft.com/powershell:lts-windowsservercore-2004 as prep
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

WORKDIR /download
RUN \
  Invoke-WebRequest https://dev.mysql.com/get/Downloads/MySQL-8.0/mysql-8.0.25-winx64.zip -OutFile './mysql-8.0.25-winx64.zip';\
  Expand-Archive -Path './mysql-8.0.25-winx64.zip' -DestinationPath '.';\
  Invoke-WebRequest https://aka.ms/vs/16/release/vc_redist.x64.exe -OutFile './vc_redist.x64.exe';

WORKDIR /src
COPY . .
RUN \
  Get-ChildItem './*' -Include ('*.sln', '*.csproj', '*.targets') -Recurse | ForEach {\
    $rel=Resolve-Path $_.FullName -Relativ;\
    Write-Host $rel;\
    $dst=Join-Path './../project_files' $rel;\
    New-Item -ItemType 'File' -Path $dst -Force | Out-Null;\
    Copy-Item $_.FullName -Destination $dst;\
  }

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
    ConnectionString='Server=localhost;Port=3306;Database=myapp;UId=root;PWd=\"%MYSQL_PW%\"';\
    Redis=@{\
      Host='localhost';\
      Port=6379;\
    }\
  } | ConvertTo-JSON | Set-Content -Path './SRC/Services/Config/config.live.json';"
RUN dotnet publish ./SRC/MyApp.sln -c Release
#RUN pwsh -command "Get-Content -Path ./BIN/net5.0/publish/config.json"

# app
FROM mcr.microsoft.com/dotnet/runtime:5.0-windowsservercore-ltsc2019

WORKDIR /vc_redist.x64/
COPY --from=prep /download/vc_redist.x64.exe .
RUN powershell -command "\
  $ErrorActionPreference='Stop';\
  Write-Host 'Install Microsoft Visual C++ Redistributable...';\
  Start-Process './vc_redist.x64.exe' -ArgumentList '/install /passive /norestart /log vc_out.log' -Wait -NoNewWindow;\
  Get-Content -Path './vc_out.log';"

WORKDIR /mysql-8.0.25-winx64
COPY --from=prep /download/mysql-8.0.25-winx64 .

WORKDIR /mysql-8.0.25-winx64/bin
ARG MYSQL_PW
ARG CUSTOM_SQL=false

RUN powershell -command "\
  $ErrorActionPreference='Stop';\
  Write-Host 'Install MySQL...';\
  Start-Process './mysqld.exe' -ArgumentList '--initialize-insecure' -Wait -NoNewWindow;\
  Start-Process './mysqld.exe' -ArgumentList '--install' -Wait -NoNewWindow;\
  Start-Service MySQL;\
  Get-Service MySQL;\
  Start-Process './mysqladmin.exe' -ArgumentList '--user=root password \"%MYSQL_PW%\"' -Wait -NoNewWindow;\
  Start-Process './mysqladmin.exe' -ArgumentList 'create myapp --user=root --password=\"%MYSQL_PW%\"' -Wait -NoNewWindow;\
  $CustomSql=\"%CUSTOM_SQL%\";\
  if ($CustomSql -ne 'false') {\
    Write-Host 'Executing custom SQL...';\
    Start-Process './mysql.exe' -ArgumentList ('--user=root --password=\"%MYSQL_PW%\" --execute=\"{0}\"' -f $CustomSql) -Wait -NoNewWindow;\
  }"

ARG CERT_PATH
ARG CERT_PW
ARG ROOT_PW

# Required for "MyApp.Server.exe -install"
ENV USER_INTERACTIVE=true

WORKDIR /app
COPY $CERT_PATH ./certificate.p12
RUN powershell -command "\
  $ErrorActionPreference='Stop';\
  Write-Host 'Register certificate...';\
  $hash=(Import-PfxCertificate -FilePath './certificate.p12' Cert:\LocalMachine\My -Password (ConvertTo-SecureString -String '%CERT_PW%' -Force -AsPlainText)).Thumbprint;\
  $appId=(New-Guid).ToString('B');\
  netsh http add sslcert ipport=127.0.0.1:1986 certhash=$hash appid=$appId;\
  Remove-Item -Path './certificate.p12';"

COPY --from=build /app/BIN/net5.0/publish .
RUN powershell -command "\
  $ErrorActionPreference='Stop';\
  Write-Host 'Install MyApp...';\
  Start-Process './MyApp.Server.exe' -ArgumentList '-install -noservice -user root@root.hu -password %ROOT_PW%' -Wait -NoNewWindow;"

ENTRYPOINT MyApp.Server.exe

EXPOSE 1986
EXPOSE 3306