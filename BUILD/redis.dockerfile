# prep
FROM mcr.microsoft.com/powershell:lts-windowsservercore-1809 as prep
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

WORKDIR /download
RUN \
  Invoke-WebRequest -Method Get -Uri 'https://github.com/MicrosoftArchive/redis/releases/download/win-3.2.100/Redis-x64-3.2.100.zip' -OutFile 'redis.zip';\
  Expand-Archive -Path 'redis.zip' -DestinationPath 'redis-bin/';

# app
FROM mcr.microsoft.com/windows/nanoserver:1809

WORKDIR /redis
COPY --from=prep /download/redis-bin .

ENTRYPOINT redis-server.exe --protected-mode no