# PowerShell script to trigger remote deployment
# Usage: .\deploy\Deploy-Remote.ps1 -HostName your-server.com -UserName youruser

param(
    [Parameter(Mandatory=$false)]
    [string]$HostName = "node-api.packet.oarc.uk",
    
    [Parameter(Mandatory=$false)]
    [string]$UserName = "debian",
    
    [Parameter(Mandatory=$false)]
    [string]$ScriptPath = "/opt/node-api/update-service.sh"
)

Write-Host "=== Remote Deployment to node-api ===" -ForegroundColor Cyan
Write-Host "Target: $UserName@$HostName" -ForegroundColor Yellow
Write-Host ""

# Execute the update script via SSH
Write-Host "Triggering remote update..." -ForegroundColor Green
ssh "$UserName@$HostName" "bash $ScriptPath"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "? Deployment successful!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "? Deployment failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
