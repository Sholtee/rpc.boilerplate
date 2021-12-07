# prep
FROM mcr.microsoft.com/powershell:lts-windowsservercore-2004 as prep
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

WORKDIR /download
RUN \
  Invoke-WebRequest https://dev.mysql.com/get/Downloads/MySQL-8.0/mysql-8.0.25-winx64.zip -OutFile './mysql-8.0.25-winx64.zip';\
  Expand-Archive -Path './mysql-8.0.25-winx64.zip' -DestinationPath '.';\
  Invoke-WebRequest https://aka.ms/vs/16/release/vc_redist.x64.exe -OutFile './vc_redist.x64.exe';

# mysql
FROM mcr.microsoft.com/powershell:lts-windowsservercore-2004
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

WORKDIR /vc_redist.x64/
COPY --from=prep /download/vc_redist.x64.exe .
RUN \
  $ErrorActionPreference='Stop';\
  Write-Host 'Installing Microsoft Visual C++ Redistributable...';\
  Start-Process './vc_redist.x64.exe' -ArgumentList '/install /passive /norestart /log vc_out.log' -Wait -NoNewWindow;\
  Get-Content -Path './vc_out.log';

WORKDIR /mysql-8.0.25-winx64
COPY --from=prep /download/mysql-8.0.25-winx64 .

WORKDIR /mysql-8.0.25-winx64/bin
ARG MYSQL_PWD=secret
ENV MYSQL_PWD=$MYSQL_PWD

RUN \
  Write-Host 'Installing MySQL...';\
  Start-Process './mysqld.exe' -ArgumentList '--initialize-insecure' -Wait -NoNewWindow;\  
  Start-Process './mysqld.exe' -ArgumentList '--install' -Wait -NoNewWindow;\
  Start-Service MySQL;\
  Get-Service MySQL;\
  Start-Process './mysqld.exe' -ArgumentList '--skip-name-resolve=ON' -Wait -NoNewWindow;\
  Start-Process './mysqladmin.exe' -ArgumentList ('--user=root --skip-password password \"{0}\"' -f $ENV:MYSQL_PWD) -Wait -NoNewWindow;

ENTRYPOINT \
  for() {\
    Start-Process './mysqladmin.exe' -ArgumentList '--user=root ping' -Wait -NoNewWindow;\
    Start-Sleep 1\
  }

EXPOSE 3306