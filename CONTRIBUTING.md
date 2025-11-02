# Contributing to node-api

Thank you for your interest in contributing to **node-api**! This document provides guidelines and information to help you contribute effectively.

## 🎯 Quick Start for Contributors

1. **Review the coding standards**: [Copilot Instructions](.github/copilot-instructions.md)
2. **Understand the architecture**: [Main README](README.md)
3. **Set up your development environment**: See [Quick Start](README.md#-quick-start)
4. **Run the tests**: `dotnet test`
5. **Make your changes**: Follow the guidelines below
6. **Submit a pull request**: See [Pull Request Process](#pull-request-process)

## 📋 Before You Start

### Prerequisites

- **.NET 9.0 SDK** or later
- **MySQL 8.0+** or MariaDB (for integration tests)
- **MQTT broker** (e.g., Mosquitto) - optional for local development
- **Git** for version control
- Familiarity with **C#**, **ASP.NET Core**, and **FluentValidation**

### Read the Documentation

- **[Copilot Instructions](.github/copilot-instructions.md)** - Comprehensive coding standards and guidelines
- **[Documentation Index](docs/README.md)** - Complete documentation navigation
- **[Architecture Overview](README.md#-architecture)** - System design and components

## 🛠️ Development Workflow

### 1. Fork and Clone

```bash
# Fork the repository on GitHub, then clone your fork
git clone https://github.com/YOUR_USERNAME/node-api.git
cd node-api

# Add upstream remote
git remote add upstream https://github.com/M0LTE/node-api.git
```

### 2. Create a Feature Branch

```bash
# Update your main branch
git checkout main
git pull upstream main

# Create a feature branch
git checkout -b feature/your-feature-name
```

### 3. Make Your Changes

Follow the coding standards in [Copilot Instructions](.github/copilot-instructions.md):

- Enable nullable reference types
- Use implicit usings
- Follow C# naming conventions (PascalCase for classes/methods, camelCase for parameters)
- Keep methods focused and single-purpose
- Use dependency injection over static methods

### 4. Write Tests

All new features and bug fixes **must** include tests:

- **Unit tests** go in `/Tests` directory
- Use **xUnit** as the testing framework
- Mock repositories using interfaces like `ITraceRepository`, `IEventRepository`
- Follow existing test patterns (see `L2TraceValidatorTests.cs` for examples)
- Aim for comprehensive coverage including edge cases

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~YourTestClassName"
```

### 5. Build and Test Locally

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run the service locally
cd node-api
dotnet run

# Service will be available at:
# - http://localhost:5000
# - OpenAPI docs: http://localhost:5000/scalar
```

### 6. Update Documentation

If your changes affect functionality:

- Update relevant documentation in `/docs`
- Update the main `README.md` if adding new features
- Follow the documentation standards in [docs/README.md](docs/README.md)
- Add code examples where appropriate

## ✅ Pull Request Process

### Before Submitting

- [ ] All tests pass: `dotnet test`
- [ ] Code builds without errors: `dotnet build`
- [ ] You've followed the coding standards in [Copilot Instructions](.github/copilot-instructions.md)
- [ ] You've written tests for new features or bug fixes
- [ ] You've updated documentation for new features
- [ ] Your commits have clear, descriptive messages
- [ ] You've rebased on the latest `main` branch

### Submitting Your PR

1. **Push your branch** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request** on GitHub from your fork to `M0LTE/node-api:main`

3. **Fill out the PR template** with:
   - Clear description of the changes
   - Link to related issues (if any)
   - Testing performed
   - Breaking changes (if any)

4. **Wait for review** - Maintainers will review your PR and may request changes

### PR Review Checklist

Reviewers will check:

- [ ] Code follows project standards (see [Copilot Instructions](.github/copilot-instructions.md))
- [ ] All tests pass
- [ ] New code has adequate test coverage
- [ ] Documentation is updated
- [ ] No breaking changes (or they're justified and documented)
- [ ] Code is maintainable and well-structured
- [ ] Security considerations are addressed

## 🎨 Coding Standards

### General Principles

See [Copilot Instructions](.github/copilot-instructions.md) for comprehensive guidelines. Key points:

- **Nullable reference types**: Always enabled
- **Async/Await**: Use for all I/O operations
- **Dependency Injection**: Register services in `Program.cs` with appropriate lifetimes
- **Validation**: Use FluentValidation for all input models
- **Error Handling**: Use `ILogger<T>` with structured logging
- **Database**: Use Dapper with parameterized queries
- **Testing**: Comprehensive xUnit tests with mocked dependencies

### Code Style

- Follow C# naming conventions
- Use PascalCase for public members, camelCase for parameters
- Keep methods focused and single-purpose
- Add XML documentation comments for public APIs
- Avoid blocking calls - use `await` instead of `.Result` or `.Wait()`

### Adding New Features

#### Adding a New Event Type

1. Create model in `/node-api/Models` with `@type` discriminator property
2. Create FluentValidation validator in `/node-api/Validators`
3. Register validator in `Program.cs` DI container
4. Add to `DatagramValidationService` validation logic
5. Update `UdpNodeInfoJsonDatagramDeserialiser` if needed
6. Add comprehensive tests in `/Tests`

#### Adding a New API Endpoint

1. Create or update controller in `/node-api/Controllers`
2. Inject required services via constructor
3. Add OpenAPI attributes for documentation
4. Return proper status codes and content types
5. Add integration tests in `/Tests`

## 🧪 Testing Guidelines

### Test Organization

- **Unit tests**: `/Tests` - Fast, isolated tests with mocked dependencies
- **Smoke tests**: `/SmokeTests` - End-to-end tests requiring running service

### Test Requirements

- All validators must have comprehensive tests
- All API endpoints must have integration tests
- Test both success and failure scenarios
- Include edge cases and boundary conditions
- Use descriptive test method names

### Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test Tests/

# Run only smoke tests (requires running service)
dotnet test SmokeTests/

# Run with coverage (if configured)
dotnet test /p:CollectCoverage=true
```

## 📝 Commit Message Guidelines

Write clear, descriptive commit messages:

```
feat: add support for new event type XYZ

- Added XyzEvent model with validation
- Created XyzEventValidator with comprehensive rules
- Added tests covering all validation scenarios
- Updated documentation

Closes #123
```

### Commit Message Format

- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **test**: Adding or updating tests
- **refactor**: Code refactoring
- **perf**: Performance improvements
- **chore**: Maintenance tasks

## 🐛 Reporting Bugs

### Before Reporting

- Search existing issues to avoid duplicates
- Verify the bug on the latest version
- Collect relevant logs and error messages

### Bug Report Template

When reporting a bug, include:

1. **Description**: Clear description of the issue
2. **Steps to Reproduce**: Minimal steps to reproduce the bug
3. **Expected Behavior**: What you expected to happen
4. **Actual Behavior**: What actually happened
5. **Environment**: .NET version, OS, MySQL version, etc.
6. **Logs**: Relevant log output or error messages
7. **Configuration**: Any relevant `appsettings.json` configuration

## 💡 Requesting Features

### Feature Request Template

When requesting a feature, include:

1. **Use Case**: Why is this feature needed?
2. **Proposed Solution**: How would you implement it?
3. **Alternatives**: What alternatives have you considered?
4. **Impact**: Who would benefit from this feature?

## 🔒 Security

### Reporting Security Issues

**Do not** report security vulnerabilities through public GitHub issues.

Instead, please contact the maintainers privately. Include:

- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

### Security Guidelines

- Never commit secrets or credentials
- Always parameterize database queries
- Validate all input using FluentValidation
- Follow security best practices in [Copilot Instructions](.github/copilot-instructions.md)

## 📚 Resources

### Documentation

- **[Main README](README.md)** - Project overview and quick start
- **[Copilot Instructions](.github/copilot-instructions.md)** - Comprehensive coding standards
- **[Documentation Index](docs/README.md)** - Complete documentation navigation
- **[Deployment Guide](docs/DEPLOYMENT.md)** - Production deployment
- **[Smoke Tests](SmokeTests/README.md)** - Testing guide

### Technology References

- [.NET 9.0 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [Dapper](https://github.com/DapperLib/Dapper)
- [xUnit](https://xunit.net/)

## ❓ Getting Help

- **Documentation**: Check [docs/](docs/) directory first
- **GitHub Issues**: Browse existing issues or create a new one
- **Discussions**: Use GitHub Discussions for questions and ideas

## 🙏 Code of Conduct

- Be respectful and constructive
- Welcome newcomers and help them get started
- Focus on what's best for the project
- Show empathy towards other community members

## 📄 License

By contributing to node-api, you agree that your contributions will be licensed under the same license as the project.

---

**Thank you for contributing to node-api!** 🎉

Your contributions help make packet radio network monitoring better for the entire amateur radio community.

For detailed coding standards and best practices, see [Copilot Instructions](.github/copilot-instructions.md).
