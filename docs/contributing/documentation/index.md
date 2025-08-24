# Documentation

We use [DocFX](https://github.com/dotnet/docfx) to generate our documentation. Contributing is straightforward: explore the existing docs, duplicate relevant pages, and update the content as needed. For DocFX and Markdown syntax, see the [official guide](https://dotnet.github.io/docfx/docs/markdown.html). If you're new, we're happy to help you get started.

## Installing DocFX

To install the latest DocFX as a global tool:

```bash
dotnet tool install -g docfx
```

## Building the documentation

1. Navigate to the docs folder:
   ```
   stride-community-toolkit\docs\
   ```
2. Run the `run.bat` file to build the docs:
   ```
   run.bat
   ```
3. When complete, open the site in your browser:
   ```
   http://localhost:8080/
   ```

## Editing the documentation

- All documentation files are under `docs`. Edit the Markdown files to update or add content.
- After changes, re-run `run.bat` to rebuild and preview updates in your browser.

## Submitting changes

- Ensure you're satisfied with the result.
- Submit a pull request to the `main` branch for review.