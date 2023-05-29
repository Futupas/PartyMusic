#!/bin/bash

docker build -t myapp:v1 .
docker run -p 82:82 myapp:v1

# docker exec <container_id> printenv VLC_DIR
