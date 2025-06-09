docker compose build
if [ "$?" != "0" ]; then exit 1; fi

docker compose down
docker compose up -d
