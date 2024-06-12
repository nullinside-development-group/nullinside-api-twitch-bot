docker build -t nullinside-api-twitch-bot:latest .
docker container stop nullinside-api-twitch-bot
docker container prune -f
docker run -d --log-driver=loki --log-opt loki-url="http://192.168.1.4:3100/loki/api/v1/push" --name=nullinside-api-twitch-bot -e TWITCH_BOT_CLIENT_ID=$TWITCH_BOT_CLIENT_ID -e TWITCH_BOT_CLIENT_SECRET=$TWITCH_BOT_CLIENT_SECRET -e TWITCH_BOT_CLIENT_REDIRECT=$TWITCH_BOT_CLIENT_REDIRECT -e MYSQL_SERVER=$MYSQL_SERVER -e MYSQL_USERNAME=$MYSQL_USERNAME -e MYSQL_PASSWORD=$MYSQL_PASSWORD -p 8086:8080 -p 8085:8081 --restart unless-stopped nullinside-api-twitch-bot:latest
