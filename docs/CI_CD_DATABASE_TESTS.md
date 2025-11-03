# GitHub Actions Configuration for Database Tests

## Summary

The database integration tests are now **excluded from GitHub Actions CI/CD** to prevent build failures when database credentials are unavailable.

## Changes Made

### 1. GitHub Actions Workflow Update

**File**: `.github/workflows/docker-publish.yml`

**Change**:
```yaml
# Before:
- name: Run tests
  run: dotnet test Tests/ --no-restore --configuration Release --logger "console;verbosity=minimal"

# After:
- name: Run tests
  run: dotnet test Tests/ --no-restore --configuration Release --logger "console;verbosity=minimal" --filter "Category!=DatabaseIntegration"
```

**Effect**: Tests tagged with `[Trait("Category", "DatabaseIntegration")]` are now skipped in CI/CD.

### 2. Documentation Updates

Updated the following files to document this behavior:

- **`CONTRIBUTING.md`**:
  - Added section explaining database integration tests are excluded from CI/CD
  - Documented how to run them manually
  - Clarified they require local database setup

- **`Tests/DATABASE_INTEGRATION_TESTS.md`**:
  - Added note that tests are excluded from GitHub Actions
  - Explained why they're manual-only

## Why This Was Done

### The Problem
- Database integration tests require live MySQL connection with credentials
- GitHub Actions doesn't have access to your database credentials (User Secrets are local-only)
- Tests were failing in CI/CD, blocking deployment

### The Solution
- Tests are still valuable for **local manual validation** before deployment
- CI/CD runs all other tests (unit tests, validators, etc.)
- Developers can run database tests manually when needed

## Running Tests

### In CI/CD (GitHub Actions)
- ? **Automatically runs**: All unit tests, validator tests, integration tests (with mocks)
- ? **Automatically skips**: Database integration tests

### Locally (Developers)
```bash
# Run tests like CI/CD (excludes database tests)
dotnet test --filter "Category!=DatabaseIntegration"

# Run ONLY database integration tests (requires User Secrets)
dotnet test --filter "Category=DatabaseIntegration"

# Run all tests including database tests
dotnet test
```

## When to Run Database Integration Tests

Run these tests **manually** before:
- Deploying to production
- Making schema changes
- Modifying repository code
- Changing database connection logic

## Setup Required for Database Tests

See `Tests/DATABASE_INTEGRATION_TESTS.md` for full setup instructions.

**Quick setup:**
```sh
cd Tests
dotnet user-secrets init
dotnet user-secrets set "DB_HOST" "your-host"
dotnet user-secrets set "DB_PORT" "3306"
dotnet user-secrets set "DB_USER" "your-user"
dotnet user-secrets set "DB_PASSWORD" "your-password"
dotnet user-secrets set "DB_NAME" "your-database"

# Run the tests
dotnet test --filter "Category=DatabaseIntegration"
```

## Benefits

### ? For CI/CD
- **No build failures** due to missing database credentials
- **Faster builds** (database tests are slower)
- **Deployment not blocked** by test failures

### ? For Developers
- **Tests still available** for manual validation
- **Clear documentation** on when and how to run them
- **No changes required** to existing test code

### ? For Project
- **Tests preserved** for their intended purpose (manual pre-deployment validation)
- **CI/CD remains useful** for all other test categories
- **Best practices** - integration tests with external dependencies are optional in CI/CD

## Alternative Approaches Considered

### ? Option 1: Add Database to GitHub Actions
**Rejected because:**
- Requires managing test database in CI
- Slow (spin up MySQL container)
- Not the same as production database
- Defeats the purpose (validating against **deployed** database)

### ? Option 2: Remove Database Tests Entirely
**Rejected because:**
- Tests are valuable for pre-deployment validation
- They've caught real issues with schema/repository changes

### ? Option 3: Mark as Manual-Only (Current Approach)
**Chosen because:**
- Tests remain available when needed
- CI/CD runs fast and doesn't fail
- Matches the intended use case (manual pre-deployment validation)
- Standard practice for integration tests with external dependencies

## Verification

### Check GitHub Actions Configuration
```sh
# View the test command in the workflow
Get-Content .github\workflows\docker-publish.yml | Select-String -Pattern "dotnet test" -Context 1,1
```

Should show:
```yaml
- name: Run tests
  run: dotnet test Tests/ --no-restore --configuration Release --logger "console;verbosity=minimal" --filter "Category!=DatabaseIntegration"
```

### Test the Filter Locally
```sh
# This should exclude database tests (like CI/CD)
dotnet test --filter "Category!=DatabaseIntegration"

# This should run ONLY database tests
dotnet test --filter "Category=DatabaseIntegration"
```

## Impact

- ? **No code changes** to test files
- ? **No behavior changes** for local development
- ? **CI/CD now passes** without database credentials
- ? **Deployment unblocked**
- ? **Documentation updated** for clarity

## Future Considerations

If database tests need to run in CI/CD in the future:
1. Set up a test MySQL database in GitHub Actions
2. Add GitHub secrets for test database credentials
3. Remove the `--filter` parameter from the workflow

For now, manual execution is the appropriate approach for these tests.
