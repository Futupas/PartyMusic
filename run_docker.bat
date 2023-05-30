
docker stop partymusic
docker rm partymusic

docker build -t party-music:v1 .\

docker run -p 0.0.0.0:82:82 --name partymusic -v %cd%\src:/app party-music:v1
