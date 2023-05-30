#!/bin/bash

docker stop partymusic
docker rm partymusic

docker build -t party-music:v1 .
# docker run -p 82:82 party-music:v1
# docker run -p 82:82 --device /dev/snd:/dev/snd -e DISPLAY=$DISPLAY -v /tmp/.X11-unix:/tmp/.X11-unix party-music:v1

# docker exec <container_id> printenv VLC_DIR

docker run -p 0.0.0.0:82:82 --name partymusic -v "$(pwd)"/src:/app party-music:v1

# MOUNT: 

# docker run -d \
#   -it \
#   --name devtest \
#   --mount type=bind,source="$(pwd)"/target,target=/app \
#   nginx:latest

#  docker run -d \
#   -it \
#   --name devtest \
#   -v "$(pwd)"/target:/app \
#   nginx:latest

