# Docker Build and Publish Setup

This repository supports two methods for building and publishing Docker images to Docker Hub:

## Method 1: Visual Studio Publish (Current Method)

You can continue using Visual Studio to publish Docker images as you currently do:

1. Right-click the `node-api` project
2. Select **Publish**
3. Choose the **registry.hub.docker.com_m0lte** profile
4. Click **Publish**

The Visual Studio publish profile is located at:
- `node-api/Properties/PublishProfiles/registry.hub.docker.com_m0lte.pubxml`

## Method 2: GitHub Actions (New)

The repository now includes a GitHub Actions workflow that automatically builds and pushes Docker images.

### Setup Required

To enable GitHub Actions to push to Docker Hub, you need to add the following secrets to your GitHub repository:

1. Go to your repository on GitHub
2. Navigate to **Settings** ? **Secrets and variables** ? **Actions**
3. Add the following repository secrets:

   - **`DOCKERHUB_USERNAME`**: Your Docker Hub username (`m0lte`)
   - **`DOCKERHUB_TOKEN`**: A Docker Hub access token (see below)

#### Creating a Docker Hub Access Token

1. Log in to [Docker Hub](https://hub.docker.com/)
2. Click on your username ? **Account Settings**
3. Go to **Security** ? **Access Tokens**
4. Click **New Access Token**
5. Give it a description (e.g., "GitHub Actions for node-api")
6. Set permissions to **Read & Write**
7. Click **Generate**
8. Copy the token and add it as `DOCKERHUB_TOKEN` in GitHub secrets

### How the Workflow Works

The workflow (`.github/workflows/docker-publish.yml`) triggers on:

- **Push to `master` branch**: Builds and pushes with tags `latest` and `master-<sha>`
- **Git tags matching `v*.*.*`**: Builds and pushes with semantic version tags (e.g., `v1.0.0`, `1.0`, `1`)
- **Pull requests**: Builds only (does not push to verify the build works)
- **Manual trigger**: Via GitHub Actions UI (workflow_dispatch)

### Tagging Strategy

The workflow automatically creates multiple tags:

| Event | Tags Created | Example |
|-------|-------------|---------|
| Push to master | `latest`, `master-<sha>` | `latest`, `master-abc123` |
| Tag `v1.2.3` | `1.2.3`, `1.2`, `1`, `latest` | Semantic versions |
| Pull request | No tags (build only) | N/A |

### Running Tests

The workflow runs all unit tests in the `Tests/` project before building the Docker image. If tests fail, the image will not be built or pushed.

### Manual Workflow Trigger

You can manually trigger the workflow from the GitHub Actions tab:

1. Go to **Actions** tab in your repository
2. Select **Build and Push Docker Image** workflow
3. Click **Run workflow**
4. Select the branch and click **Run workflow**

## Comparison of Methods

| Feature | Visual Studio | GitHub Actions |
|---------|--------------|----------------|
| Ease of use | ? GUI-based, one-click | ?? Automated |
| Local machine | ? Runs locally | ? Runs on GitHub |
| Runs tests | ? No | ? Yes |
| Tagging | Manual | ? Automatic semantic versioning |
| CI/CD | ? No | ? Yes |
| Requires setup | Minimal | One-time secret configuration |

## Command Line Publishing (Alternative)

You can also publish from the command line using the same settings as Visual Studio:

```bash
cd node-api
dotnet publish --configuration Release /p:PublishProfile=registry.hub.docker.com_m0lte
```

Or manually build and push:

```bash
cd node-api
dotnet publish --configuration Release --runtime linux-x64 /t:PublishContainer
```

This uses the container settings from `node-api.csproj`:
- Base Image: `mcr.microsoft.com/dotnet/aspnet:9.0`
- Repository: `m0lte/node-api`
- Registry: `docker.io`
- Tags: `latest`

## Troubleshooting

### GitHub Actions: Authentication Failed

If you see authentication errors in GitHub Actions:
1. Verify `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secrets are set correctly
2. Ensure the Docker Hub token has **Read & Write** permissions
3. Check that the token hasn't expired

### Visual Studio: Authentication Issues

If Visual Studio prompts for credentials:
1. Go to **Tools** ? **Options** ? **Container Tools**
2. Check Docker Hub authentication settings
3. You may need to re-authenticate with Docker Hub

### Command Line: Login Required

If publishing from command line fails with authentication error:

```bash
docker login docker.io
# Enter your Docker Hub username and password/token
```

## Best Practices

- **For development**: Use Visual Studio publish for quick iterations
- **For releases**: Use GitHub Actions with git tags for versioned releases
- **For CI/CD**: Configure GitHub Actions to automatically deploy on successful builds
- **For testing**: Both methods will build the same Docker image configuration

## Image Information

- **Registry**: Docker Hub (`docker.io`)
- **Repository**: `m0lte/node-api`
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **Runtime**: `linux-x64`
- **Default Tags**: `latest`

## Next Steps

1. Add the required secrets to your GitHub repository
2. Push a commit to master or create a tag to trigger the workflow
3. Monitor the workflow execution in the **Actions** tab
4. Verify the image appears on [Docker Hub](https://hub.docker.com/r/m0lte/node-api)
