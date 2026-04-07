#!/usr/bin/env bash
set -euo pipefail

# ---------------------------------------------------------------------------
# deploy.sh — Deploy PubSubDapr infrastructure to Azure
#
# Deploys at subscription scope; the resource group is created by Bicep.
# Resources are named using Azure conventions: rg-<appName>, sb-<appName>,
# cae-<appName>, aca-pub, aca-sub, etc.
#
# Usage:
#   ./infra/deploy.sh \
#     --subscription <subscription-id> \
#     --location <azure-region> \
#     --app-name <application-name> \
#     --publisher-image <image> \
#     --subscriber-image <image>
#
# Example:
#   ./infra/deploy.sh \
#     --subscription 00000000-0000-0000-0000-000000000000 \
#     --location westeurope \
#     --app-name pubsubdapr \
#     --publisher-image myacr.azurecr.io/pub:latest \
#     --subscriber-image myacr.azurecr.io/sub:latest
# ---------------------------------------------------------------------------

SUBSCRIPTION=""
LOCATION="westeurope"
APP_NAME="pubsubdapr"
PUBLISHER_IMAGE="chudinov/pubsubdapr-pub:latest"
SUBSCRIBER_IMAGE="chudinov/pubsubdapr-sub:latest"

# Parse arguments
while [[ $# -gt 0 ]]; do
  case "$1" in
    --subscription)      SUBSCRIPTION="$2";      shift 2 ;;
    --location)          LOCATION="$2";          shift 2 ;;
    --app-name)          APP_NAME="$2";          shift 2 ;;
    --publisher-image)   PUBLISHER_IMAGE="$2";   shift 2 ;;
    --subscriber-image)  SUBSCRIBER_IMAGE="$2";  shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

# Validate required args
missing=()
[[ -z "$SUBSCRIPTION" ]] && missing+=("--subscription")

if [[ ${#missing[@]} -gt 0 ]]; then
  echo "Error: missing required arguments: ${missing[*]}" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== PubSubDapr Infrastructure Deployment ==="
echo "  Subscription:     $SUBSCRIPTION"
echo "  Location:         $LOCATION"
echo "  App name:         $APP_NAME"
echo "  Resource group:   rg-${APP_NAME}  (created by Bicep)"
echo "  Publisher image:  $PUBLISHER_IMAGE"
echo "  Subscriber image: $SUBSCRIBER_IMAGE"
echo ""

# Set active subscription
echo "--- Setting subscription..."
az account set --subscription "$SUBSCRIPTION"

# Validate the Bicep template before deploying
echo "--- Validating Bicep template..."
az deployment sub validate \
  --location "$LOCATION" \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters \
      appName="$APP_NAME" \
      location="$LOCATION" \
      publisherImage="$PUBLISHER_IMAGE" \
      subscriberImage="$SUBSCRIBER_IMAGE" \
  --output none

echo "    Validation passed."

# Deploy at subscription scope
DEPLOYMENT_NAME="${APP_NAME}-deploy-$(date -u +%Y%m%dT%H%M%S)"
echo "--- Deploying '$DEPLOYMENT_NAME'..."
az deployment sub create \
  --name "$DEPLOYMENT_NAME" \
  --location "$LOCATION" \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters \
      appName="$APP_NAME" \
      location="$LOCATION" \
      publisherImage="$PUBLISHER_IMAGE" \
      subscriberImage="$SUBSCRIBER_IMAGE" \
  --output json \
  | tee /tmp/deployment-output.json \
  | jq -r '
      .properties.outputs
      | to_entries[]
      | "  \(.key): \(.value.value)"
    '

echo ""
echo "=== Deployment complete ==="
PUBLISHER_URL=$(jq -r '.properties.outputs.publisherUrl.value' /tmp/deployment-output.json)
SUBSCRIBER_URL=$(jq -r '.properties.outputs.subscriberUrl.value' /tmp/deployment-output.json)
echo "  Publisher:  $PUBLISHER_URL"
echo "  Subscriber: $SUBSCRIBER_URL"
