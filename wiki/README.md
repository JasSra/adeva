# Wiki README

This directory contains the comprehensive wiki documentation for the Debt Management Platform.

## Wiki Structure

The wiki is organized into the following pages:

1. **[Home.md](Home.md)** - Main wiki landing page with project overview and quick links
2. **[Getting-Started.md](Getting-Started.md)** - Setup and installation guide for developers
3. **[Architecture.md](Architecture.md)** - Technical architecture, design patterns, and system structure
4. **[User-Guides.md](User-Guides.md)** - Role-based workflow documentation for Debtors, Clients, and Admins
5. **[API-Reference.md](API-Reference.md)** - Integration documentation for external services
6. **[Development-Guide.md](Development-Guide.md)** - Development workflows, coding standards, and best practices
7. **[Deployment.md](Deployment.md)** - Deployment procedures and infrastructure setup
8. **[FAQ.md](FAQ.md)** - Frequently asked questions and troubleshooting

## Viewing the Wiki

### Local Viewing

Any Markdown viewer can be used to read the wiki locally:

- **Visual Studio Code** - Preview with `Ctrl+Shift+V` (Windows/Linux) or `Cmd+Shift+V` (Mac)
- **GitHub Desktop** - Preview in any Markdown editor
- **Browser Extensions** - Markdown viewer extensions for Chrome, Firefox, etc.

### GitHub Wiki

To publish this wiki to GitHub Wiki:

1. Navigate to your repository's Wiki tab on GitHub
2. Create pages matching the file names (without `.md` extension)
3. Copy content from each file to the corresponding wiki page
4. Update internal links to use GitHub Wiki link format: `[[Page Name]]`

### Recommended Reading Order

For new developers:
1. [Home](Home.md) - Overview
2. [Getting Started](Getting-Started.md) - Setup
3. [Architecture](Architecture.md) - Understand the system
4. [Development Guide](Development-Guide.md) - Coding practices
5. [User Guides](User-Guides.md) - Understand workflows
6. [FAQ](FAQ.md) - Common questions

For operations/DevOps:
1. [Home](Home.md) - Overview
2. [Architecture](Architecture.md) - System design
3. [Deployment](Deployment.md) - Infrastructure setup
4. [API Reference](API-Reference.md) - Integration details
5. [FAQ](FAQ.md) - Troubleshooting

For business users:
1. [Home](Home.md) - Overview
2. [User Guides](User-Guides.md) - Workflows
3. [FAQ](FAQ.md) - Common questions

## Maintaining the Wiki

### Updating Documentation

- Keep documentation in sync with code changes
- Update version numbers when releasing
- Add new FAQ entries for common support questions
- Include screenshots for UI changes

### Documentation Standards

- Use clear, concise language
- Include code examples where appropriate
- Keep formatting consistent across pages
- Test all command-line examples
- Update last modified dates when making changes

### Contributing

When updating the wiki:

1. Make changes to the `.md` files in this directory
2. Preview changes locally
3. Commit changes with descriptive messages: `docs: Update API reference with new webhook events`
4. Create pull request for review

## Wiki Coverage

The wiki covers:

- ✅ Project overview and features
- ✅ Development environment setup
- ✅ Technical architecture and design
- ✅ User workflows for all roles
- ✅ Authentication and security
- ✅ Payment processing integration
- ✅ Multi-tenancy and branding
- ✅ Background jobs and scheduling
- ✅ Database migrations
- ✅ Testing strategies
- ✅ Deployment procedures
- ✅ Monitoring and observability
- ✅ Troubleshooting and FAQ

## Quick Reference

### Common Commands

**Build:**
```bash
dotnet build
```

**Run:**
```bash
dotnet run --project src/DebtManager.Web
```

**Test:**
```bash
dotnet test
```

**Migrations:**
```bash
dotnet ef migrations add MigrationName -p src/DebtManager.Infrastructure -s src/DebtManager.Web
dotnet ef database update -p src/DebtManager.Infrastructure -s src/DebtManager.Web
```

### Important URLs

- **Application:** https://localhost:5001
- **Admin Portal:** https://localhost:5001/Admin
- **Backoffice:** https://localhost:5001/Backoffice
- **Hangfire:** https://localhost:5001/hangfire
- **Health Check:** https://localhost:5001/health/live

### Support Contacts

For questions or issues:
- Check the [FAQ](FAQ.md) first
- Review relevant wiki pages
- Contact the development team
- Submit a GitHub issue (if applicable)

---

**Last Updated:** 2024  
**Wiki Version:** 1.0
