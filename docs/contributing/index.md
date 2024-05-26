# Contributing

## Welcome Contributors

We appreciate your interest in contributing to the Stride Community Toolkit. This section provides all the information you need to get started, including guidelines, best practices, and resources to help you make meaningful contributions to the project. Whether you're fixing bugs, adding new features, or improving documentation, your efforts are valuable and greatly appreciated.

## Getting Started

Since the toolkit is currently in preview, we don't have extensive instructions for contributors. However, you can follow these general guidelines to get started:

- Feel free to make Pull Requests (PRs) directly to speed up your process. Optionally, create an issue on our [GitHub Issues](https://github.com/stride3d/stride-community-toolkit/issues) page, or work on an existing issue. Alternatively, you can start a discussion on our [GitHub Discussions](https://github.com/stride3d/stride-community-toolkit/discussions) page.
- For quick communication with the community, join our Discord server and participate in the #toolkit channel [here](https://discord.com/channels/500285081265635328/1179562410655363132).

## Documentation

We use DocFX for our documentation. The easiest way to contribute to the documentation is by looking at existing docs, duplicating pages, and updating the content accordingly. You can find helpful information on using DocFX and writing Markdown [here](https://dotnet.github.io/docfx/docs/markdown.html). We will also assist you in getting started with the documentation.

## Major Release Workflow

When preparing for a major release, such as upgrading from **.NET 8** to **.NET 9**, there are several key steps and pages that need to be updated. This section will list the necessary instructions to ensure a smooth transition. 

### Steps for Major Release:

1. Update .NET reference on the Home page's `index.md`
1. Update .NET reference in `manual\gettings-started.md`
1. Update .NET reference in `manual\code-only\create-project.md`
1. Update `TargetFramework` in all `.csproj` files
1. Test all examples and ensure they work as expected
 
By following these steps, you can help ensure that each major release is well-documented and thoroughly tested, providing a smooth experience for all users of the toolkit.

## Additional Resources

- [Stride Community Toolkit GitHub Repository](https://github.com/stride3d/stride-community-toolkit)
- [Stride Game Engine GitHub Repository](https://github.com/stride3d/stride)
- [Stride Docs](https://doc.stride3d.net/)

Thank you for contributing to the Stride Community Toolkit 🙂.