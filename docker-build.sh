#!/bin/bash

# export GHCR_PAT=ghp_xxx
# echo $GHCR_PAT | docker login ghcr.io -u mchudinov --password-stdin

export VERSION=0.1.0

# Build overlege-web
docker build -t overlege-web . -f Web/Dockerfile
docker tag overlege-web ghcr.io/mchudinov/overlege-web:$VERSION
docker push ghcr.io/mchudinov/overlege-web:$VERSION

# # Build overlege-webadmin
# docker build -t overlege-webadmin . -f WebAdmin/Dockerfile
# docker tag overlege-web ghcr.io/mchudinov/overlege-webadmin:$VERSION
# docker push ghcr.io/mchudinov/overlege-webadmin:$VERSION