#!/bin/bash

docker build -t party-music:v1 .
# docker run -p 82:82 party-music:v1
docker run -p 82:82 --device /dev/snd:/dev/snd -e DISPLAY=$DISPLAY -v /tmp/.X11-unix:/tmp/.X11-unix party-music:v1

# docker exec <container_id> printenv VLC_DIR
