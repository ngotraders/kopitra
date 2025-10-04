# Container App Deployment Troubleshooting Guide

This guide explains how to diagnose Azure Container App deployment failures that originate from the CI/CD workflow.

## 1. Understand the workflow stages

The `CI/CD` workflow defines three relevant jobs:

1. **`infra`** provisions or updates the resource group, Azure Container Registry (ACR), Container Apps environment, and supporting services through Bicep templates.
2. **`gateway`** builds the container image, publishes it to ACR, and updates the Container App revision.
3. **`opsconsole`** deploys the Static Web App assets (not directly related to the gateway runtime).

A failure in the `gateway` job usually means the container image was not published or the Container App could not pull it.

## 2. Inspect workflow logs in GitHub Actions

1. Open the failed run in **Actions** and expand the `gateway` job.
2. Review the "Push image to ACR" and "Update Container App revision" steps for Azure CLI errors.
3. Re-run the workflow with step-debug logging if the failure is unclear:
   - Add the repository secret `ACTIONS_STEP_DEBUG=true`.
   - Re-run the job; GitHub Actions will emit verbose CLI output.

## 3. Validate the container image in Azure Container Registry

Run the following commands from a local terminal (or Cloud Shell) after authenticating with `az login`:

```bash
RESOURCE_GROUP=<your resource group>
ACR_NAME=<workload><environment>acr
IMAGE_REPOSITORY=<workload>-gateway
IMAGE_TAG=<commit SHA or tag>

# Confirm the registry exists
az acr show --name "$ACR_NAME"

# List recent image tags
az acr repository show-tags \
  --name "$ACR_NAME" \
  --repository "$IMAGE_REPOSITORY" \
  --orderby time_desc \
  --top 5

# Confirm the target tag exists
az acr repository show-manifests \
  --name "$ACR_NAME" \
  --repository "$IMAGE_REPOSITORY" \
  --query "[?tags[?@=='$IMAGE_TAG']]" \
  --output table
```

If the expected tag is missing, the GitHub Actions job likely failed before pushing or lacked permission to authenticate against ACR.

## 4. Check Container App state and revisions

Confirm that the Container App exists and inspect recent revisions:

```bash
CONTAINER_APP_NAME=<workload>-gateway-aca-<environment>

# Display the Container App definition
az containerapp show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP"

# Review the latest revisions and their status
az containerapp revision list \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --output table
```

Look for revisions stuck in `Failed` or `CrashLoopBackOff`. The `image` column confirms which container tag each revision attempted to start.

## 5. Tail runtime logs for deployment errors

Fetch application logs from the most recent revision to surface startup failures:

```bash
LATEST_REVISION=$(az containerapp revision list \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "[0].name" -o tsv)

az containerapp logs show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --revision "$LATEST_REVISION" \
  --follow
```

Authentication or pull errors typically appear here when the Container App cannot reach ACR.

## 6. Common remediation steps

- **Missing image**: Re-run the workflow after confirming `Push image to ACR` succeeded, or trigger a manual rebuild with `az acr build`.
- **Unauthorized to pull**: Verify the Container App's managed identity has the `AcrPull` role on the registry (`az role assignment list --assignee <principalId> --scope <ACR resource ID>`).
- **Stale registry binding**: Run `az containerapp registry set --server <loginServer> --identity system` to reapply the managed identity binding.
- **Configuration drift**: Re-run the `infra` job to apply the latest Bicep templates, then redeploy the container.

Following this sequence isolates whether the issue stems from the build, publish, or deployment stage and provides actionable diagnostics for each case.
