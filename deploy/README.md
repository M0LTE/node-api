# Remote Deployment Scripts

This directory contains scripts for deploying updates to the remote Debian server.

## Files

- **`update-service.sh`** - Script that runs on the Debian server to restart the systemd service (which pulls and restarts)
- **`Deploy-Remote.ps1`** - PowerShell script for triggering remote deployment from your local machine
- **`deploy-remote.sh`** - Bash equivalent for WSL/Git Bash
- **`README.md`** - This file

## Important: Line Endings

The `.sh` files in this directory **must have Unix (LF) line endings**, not Windows (CRLF). The `.gitattributes` file is configured to enforce this, but if you're experiencing issues:

**Quick fix on the server:**
```bash
ssh debian@node-api.packet.oarc.uk "sed -i 's/\r$//' /opt/node-api/update-service.sh"
```

**To fix locally before committing:**
```bash
# In Git Bash or WSL
dos2unix deploy/*.sh

# Or using sed
sed -i 's/\r$//' deploy/update-service.sh deploy/deploy-remote.sh
```

## Setup on Debian Server

**Important:** Root SSH login is not permitted. Use the `debian` user account.

1. Create the deployment directory and set permissions:
   ```bash
   ssh debian@node-api.packet.oarc.uk "sudo mkdir -p /opt/node-api && sudo chown \$USER:\$USER /opt/node-api"
   ```

2. Copy `update-service.sh` to the Debian server:
   ```bash
   scp deploy/update-service.sh debian@node-api.packet.oarc.uk:/opt/node-api/
   ```

3. Fix line endings and make it executable:
   ```bash
   ssh debian@node-api.packet.oarc.uk "sed -i 's/\r$//' /opt/node-api/update-service.sh && chmod +x /opt/node-api/update-service.sh"
   ```

4. Configure passwordless sudo for systemctl (required for automated deployment):
   ```bash
   ssh debian@node-api.packet.oarc.uk
   sudo visudo -f /etc/sudoers.d/node-api-deploy
   ```
   
   Add this line:
   ```
   debian ALL=(ALL) NOPASSWD: /bin/systemctl restart node-api, /bin/systemctl status node-api
   ```

## Setup SSH Key Authentication (for GitHub Actions)

For automated deployment, you need to set up SSH key authentication:

1. **Generate an SSH key:**

   **On Windows (PowerShell):**
   ```powershell
   ssh-keygen -t ed25519 -C "github-actions-node-api" -f $env:USERPROFILE\.ssh\node-api-deploy
   ```

   **On Linux/Mac/WSL:**
   ```bash
   ssh-keygen -t ed25519 -C "github-actions-node-api" -f ~/.ssh/node-api-deploy
   ```

2. **Copy the public key to the Debian server:**

   **On Windows (PowerShell):**
   ```powershell
   type $env:USERPROFILE\.ssh\node-api-deploy.pub | ssh debian@node-api.packet.oarc.uk "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
   ```

   **On Linux/Mac/WSL:**
   ```bash
   ssh-copy-id -i ~/.ssh/node-api-deploy.pub debian@node-api.packet.oarc.uk
   ```

3. **Test the key:**

   **On Windows (PowerShell):**
   ```powershell
   ssh -i $env:USERPROFILE\.ssh\node-api-deploy debian@node-api.packet.oarc.uk "echo 'Success'"
   ```

   **On Linux/Mac/WSL:**
   ```bash
   ssh -i ~/.ssh/node-api-deploy debian@node-api.packet.oarc.uk "echo 'Success'"
   ```

4. **For GitHub Actions**, add secrets to your repository:
   - Go to repository Settings ? Secrets and variables ? Actions
   - Add `DEPLOY_SSH_KEY` - paste the **private key** content:
     - **Windows**: `Get-Content $env:USERPROFILE\.ssh\node-api-deploy | clip` (copies to clipboard)
     - **Linux/Mac**: `cat ~/.ssh/node-api-deploy`
   - Add `DEPLOY_HOST` - value: `node-api.packet.oarc.uk`
   - Add `DEPLOY_USER` - value: `debian`
   - Add `DEPLOY_SCRIPT_PATH` - value: `/opt/node-api/update-service.sh`

## Local Deployment

From your local machine (Windows):

```powershell
.\deploy\Deploy-Remote.ps1
```

Or with custom parameters:

```powershell
.\deploy\Deploy-Remote.ps1 -HostName node-api.packet.oarc.uk -UserName debian -ScriptPath /opt/node-api/update-service.sh
```

From WSL/Git Bash:

```bash
./deploy/deploy-remote.sh
```

## GitHub Actions Deployment

The deployment happens automatically after a successful Docker image push on the `master` branch.

**Note:** The GitHub Actions workflow automatically fixes line endings and copies the latest version of `update-service.sh` before executing it.

To deploy manually:
1. Go to Actions tab in GitHub
2. Select "Build and Push Docker Image" workflow
3. Click "Run workflow"
4. Select the branch and click "Run workflow"

The deployment step will:
1. Wait for the Docker image to be pushed
2. Fix line endings in the update script
3. Copy the script to the server via SCP
4. SSH into the Debian server and execute the script
5. The systemd service pulls the latest image and runs compose up

## Troubleshooting

### Line ending errors (`$'\r': command not found`)
This means the script has Windows line endings. Fix it:
```bash
ssh debian@node-api.packet.oarc.uk "sed -i 's/\r$//' /opt/node-api/update-service.sh"
```

### SSH Connection Issues
```bash
# Test SSH connection
ssh debian@node-api.packet.oarc.uk "echo 'Connection successful'"
```

### Check Service Status Remotely
```bash
ssh debian@node-api.packet.oarc.uk "sudo systemctl status node-api"
```

### View Recent Logs
```bash
ssh debian@node-api.packet.oarc.uk "sudo journalctl -u node-api -n 50 --no-pager"
```

### Manual Service Restart
```bash
ssh debian@node-api.packet.oarc.uk "sudo systemctl restart node-api"
```

### Sudo Password Prompts
If the deployment hangs because sudo requires a password, ensure you've configured passwordless sudo (see setup step 4 above).
