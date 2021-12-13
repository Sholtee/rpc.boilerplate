docker compose -f ./BUILD/docker-compose-test.yml run --rm test_env pwsh -command ./BUILD/test.ps1
docker compose -f ./BUILD/docker-compose-test.yml down
docker container prune --force