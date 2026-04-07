#!/usr/bin/env bash
set -euo pipefail

# ---------------------------------------------------------------------------
# build-and-push.sh — Build and push Publisher and Subscriber Docker images
#                     to Docker Hub (hub.docker.com/repositories/chudinov)
#
# Usage:
#   ./build-and-push.sh [--tag <tag>] [--platform <platform>]
#
# Options:
#   --tag       Image tag (default: latest)
#   --platform  Docker build platform (default: linux/amd64)
#
# Examples:
#   ./build-and-push.sh
#   ./build-and-push.sh --tag 1.0.0
#   ./build-and-push.sh --tag 1.0.0 --platform linux/amd64,linux/arm64
#
# Prerequisites:
#   - docker login (run once before using this script)
#   - Docker Buildx for multi-platform builds
# ---------------------------------------------------------------------------

REGISTRY="chudinov"
PUBLISHER_IMAGE="$REGISTRY/pubsubdapr-pub"
SUBSCRIBER_IMAGE="$REGISTRY/pubsubdapr-sub"
TAG="latest"
PLATFORM="linux/amd64"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --tag)       TAG="$2";      shift 2 ;;
    --platform)  PLATFORM="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

# Resolve script dir = repo root (script is in the repo root)
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== PubSubDapr Docker Build & Push ==="
echo "  Registry:   $REGISTRY (Docker Hub)"
echo "  Tag:        $TAG"
echo "  Platform:   $PLATFORM"
echo "  Repo root:  $REPO_ROOT"
echo ""

# Ensure logged in to Docker Hub
if ! docker info --format '{{.RegistryConfig.IndexConfigs}}' 2>/dev/null | grep -q "docker.io"; then
  echo "--- Logging in to Docker Hub..."
  docker login
fi

# Publisher
echo "--- Building Publisher ($PUBLISHER_IMAGE:$TAG)..."
docker buildx build \
  --platform "$PLATFORM" \
  --file "$REPO_ROOT/Pub/Dockerfile" \
  --tag "$PUBLISHER_IMAGE:$TAG" \
  --push \
  "$REPO_ROOT"

echo "    Pushed: $PUBLISHER_IMAGE:$TAG"

# Subscriber
echo "--- Building Subscriber ($SUBSCRIBER_IMAGE:$TAG)..."
docker buildx build \
  --platform "$PLATFORM" \
  --file "$REPO_ROOT/Sub/Dockerfile" \
  --tag "$SUBSCRIBER_IMAGE:$TAG" \
  --push \
  "$REPO_ROOT"

echo "    Pushed: $SUBSCRIBER_IMAGE:$TAG"

echo ""
echo "=== Done ==="
echo "  Publisher:  $PUBLISHER_IMAGE:$TAG"
echo "  Subscriber: $SUBSCRIBER_IMAGE:$TAG"
echo ""
echo "Use these image names when deploying:"
echo "  ./infra/deploy.sh \\"
echo "    --publisher-image $PUBLISHER_IMAGE:$TAG \\"
echo "    --subscriber-image $SUBSCRIBER_IMAGE:$TAG \\"
echo "    ..."
