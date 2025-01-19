# Documentation

We use [DocFX](https://github.com/dotnet/docfx) to generate our documentation. Contributing to the documentation is straightforward: explore the existing docs, duplicate relevant pages, and update the content as needed. For more information on DocFX and Markdown syntax, refer to the [official guide](https://dotnet.github.io/docfx/docs/markdown.html). If you're new to the process, we're happy to assist you in getting started.

## Installing DocFX

To install the latest version of DocFX, use the following command:

```bash
dotnet tool install -g docfx
```

## Building the Documentation

1. Navigate to the docs folder in the project directory:
   ```
   stride-community-toolkit\docs\
   ```
1. Run the `run.bat`` file to build the documentation:
   ```
   run.bat
   ```
1. Once the process completes, you can access the documentation in your browser at:
   ```
   http://localhost:8080/
   ```

## Editing the Documentation

- All documentation files are located in the `docs` folder. You can edit these Markdown files to update or add content.
- After making changes, re-run the `run.bat` command to rebuild the documentation and preview your updates in your browser.

## Submitting Changes

- Ensure you’re satisfied with the results.
- Submit a pull request to the `main` branch for review.