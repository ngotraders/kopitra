# Offline Bicep Builds in Restricted Networks

Azure CLI attempts to download the latest Bicep binary from `aka.ms` every time you invoke `az bicep ...`. In environments where outbound
HTTPS is blocked or the SSL chain cannot be validated, commands such as `az bicep build --file infra/main.bicep --stdout` fail with
errors similar to:

```
HTTPSConnectionPool(host='aka.ms', port=443): Max retries exceeded with url: /BicepLatestRelease
```

The CLI already ships with a standalone `bicep` executable, so you can bypass the download step entirely by telling Azure CLI to use the
binary that is already on your `PATH`.

## One-time configuration

1. Verify the standalone Bicep CLI is available:
   ```bash
   bicep --version
   ```
2. Instruct Azure CLI to reuse the local binary instead of reaching out to `aka.ms`:
   ```bash
   az config set bicep.use_binary_from_path=true
   ```
   This writes a setting to `~/.azure/config` so every future `az bicep` invocation skips the update check.
3. Confirm the CLI now resolves the bundled binary:
   ```bash
   az bicep version
   ```

With the configuration in place, infrastructure commands work offline:

```bash
az bicep build --file infra/main.bicep --stdout
```

If you ever need to restore the default behaviour (allowing Azure CLI to download updates automatically), run:

```bash
az config unset bicep.use_binary_from_path
```
