# Documentation

The documentation is built with [DocFX](https://github.com/dotnet/docfx) from the `docs` folder. To add a page, copy a similar existing page and update its content. For DocFX and Markdown syntax, see the [official guide](https://dotnet.github.io/docfx/docs/markdown.html).

## Installing DocFX

Install or update DocFX as a global tool, the same command the GitHub Pages workflow runs:

```bash
dotnet tool update -g docfx
```

## Building the documentation

1. Navigate to the docs folder:
   ```
   stride-community-toolkit\docs\
   ```
2. Run `run.bat`. It regenerates the example pages from the examples' metadata blocks, then builds and serves the site:
   ```
   run.bat
   ```
3. When complete, open the site in your browser:
   ```
   http://localhost:8080/
   ```

## Editing the documentation

- Edit the Markdown files under `docs`, then re-run `run.bat` to rebuild and preview the site.
- Do not edit a page whose front matter has `generated: true`: the example generator overwrites it. See [Editing a generated page](../examples/index.md#editing-a-generated-page). The API pages are generated from the XML comments in `src`, so fix those instead.

## Submitting changes

Submit a pull request to the `main` branch for review.