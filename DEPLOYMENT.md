# Quick Deployment Guide

## One-Time Setup

### 1. On the Debian Server

```bash
# SSH into the server
ssh debian@node-api.packet.oarc.uk

# Create directory for deployment scripts
sudo mkdir -p /opt/node-api

# Make your user the owner so you can copy files directly
sudo chown $USER:$USER /opt/node-api

# Exit the SSH session
exit
```

### 2. Copy the Update Script

```bash
# From your local machine
scp deploy/update-service.sh debian@node-api.packet.oarc.uk:/opt/node-api/
ssh debian@node-api.packet.oarc.uk "chmod +x /opt/node-api/update-service.sh"
```

**Important:** If you're on Windows, the file may have Windows line endings. Fix them on the server:

```bash
ssh debian@node-api.packet.oarc.uk "dos2unix /opt/node-api/update-service.sh"
```

Or if `dos2unix` is not installed:

```bash
ssh debian@node-api.packet.oarc.uk "sed -i 's/\r$//' /opt/node-api/update-service.sh"
```

### 3. Test the Script

```bash
ssh debian@node-api.packet.oarc.uk "bash /opt/node-api/update-service.sh"
```

### 4. Setup GitHub Actions (for automated deployment)

Generate an SSH key for GitHub Actions:

**On Windows (PowerShell):**
```powershell
ssh-keygen -t ed25519 -C "github-actions-node-api" -f $env:USERPROFILE\.ssh\node-api-deploy
```

**On Linux/Mac/WSL:**
```bash
ssh-keygen -t ed25519 -C "github-actions-node-api" -f ~/.ssh/node-api-deploy
```

Copy the public key to the server:

**On Windows (PowerShell):**
```powershell
# Read the public key and copy it to the server
type $env:USERPROFILE\.ssh\node-api-deploy.pub | ssh debian@node-api.packet.oarc.uk "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
```

**On Linux/Mac/WSL:**
```bash
ssh-copy-id -i ~/.ssh/node-api-deploy.pub debian@node-api.packet.oarc.uk
```

Test it works:

**On Windows (PowerShell):**
```powershell
ssh -i $env:USERPROFILE\.ssh\node-api-deploy debian@node-api.packet.oarc.uk "echo 'Success'"
```

**On Linux/Mac/WSL:**
```bash
ssh -i ~/.ssh/node-api-deploy debian@node-api.packet.oarc.uk "echo 'Success'"
```

Add these secrets to your GitHub repository (Settings ? Secrets and variables ? Actions ? New repository secret):

| Secret Name | Value |
|-------------|-------|
| `DEPLOY_SSH_KEY` | Contents of `~/.ssh/node-api-deploy` (the **private** key)<br/>**Windows**: `C:\Users\YourUsername\.ssh\node-api-deploy`<br/>**Linux/Mac**: `~/.ssh/node-api-deploy` |
| `DEPLOY_HOST` | `node-api.packet.oarc.uk` |
| `DEPLOY_USER` | `debian` |
| `DEPLOY_SCRIPT_PATH` | `/opt/node-api/update-service.sh` |

**To get the private key content on Windows:**
```powershell
Get-Content $env:USERPROFILE\.ssh\node-api-deploy | clip
# The private key is now in your clipboard - paste it into GitHub secrets
```

**To get the private key content on Linux/Mac:**
```bash
cat ~/.ssh/node-api-deploy | pbcopy   # macOS
cat ~/.ssh/node-api-deploy | xclip    # Linux (requires xclip)
# Or just: cat ~/.ssh/node-api-deploy  # and copy manually
```

## Daily Usage

### Deploy Locally (from Windows)

**Option 1: Using the batch file (easiest - just double-click it)**
```
deploy\Deploy.bat
```

**Option 2: From PowerShell**
```powershell
# From the repo root in a PowerShell terminal
.\deploy\Deploy-Remote.ps1

# Or with custom parameters:
.\deploy\Deploy-Remote.ps1 -HostName node-api.packet.oarc.uk -UserName debian
```

**Option 3: Right-click the PowerShell file**
- Right-click `deploy\Deploy-Remote.ps1` in File Explorer
- Select "Run with PowerShell"

### Deploy via GitHub Actions

**Option 1: Push to master (automatic)**
```bash
git push origin master
```

**Option 2: Manual trigger**
1. Go to https://github.com/M0LTE/node-api/actions
2. Click "Build and Push Docker Image"
3. Click "Run workflow"
4. Select branch and click "Run workflow"

### Check Remote Service Status

```powershell
ssh debian@node-api.packet.oarc.uk "systemctl status node-api"
```

### View Remote Logs

```powershell
ssh debian@node-api.packet.oarc.uk "journalctl -u node-api -f"
```

## Troubleshooting

### Line ending issues (Windows)
If you see errors like `$'\r': command not found`, the script has Windows line endings:

```bash
# Fix it on the server
ssh debian@node-api.packet.oarc.uk "sed -i 's/\r$//' /opt/node-api/update-service.sh"
```

### Deployment fails with "Permission denied"
- Ensure SSH key is added to GitHub secrets
- Verify the public key is in `~/.ssh/authorized_keys` on the Debian server
- Check that the `debian` user has sudo privileges without password for systemctl commands

### Service doesn't restart
- Check the systemd service name matches: `node-api`
- Verify the docker-compose file is in the correct location
- Check systemd service logs: `ssh debian@node-api.packet.oarc.uk "journalctl -u node-api -n 50"`
- Ensure the `debian` user can run `sudo systemctl restart node-api` without password prompt

### Docker pull fails
- Ensure the server can reach Docker Hub
- Check if the image name is correct: `m0lte/node-api:latest`
- Test manually: `ssh debian@node-api.packet.oarc.uk "docker pull m0lte/node-api:latest"`

### Sudo password prompts
If the script hangs because sudo requires a password, you need to configure passwordless sudo for systemctl:

```bash
# On the Debian server
sudo visudo -f /etc/sudoers.d/node-api-deploy
```

Add this line:

```
debian ALL=(ALL) NOPASSWD: /bin/systemctl restart node-api, /bin/systemctl status node-api
```

Save and exit, then test:

```bash
sudo systemctl status node-api
# Should not prompt for password
