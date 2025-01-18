# Documentation

We use [DocFX](https://github.com/dotnet/docfx) for our documentation. The easiest way to contribute to the documentation is by looking at existing docs, duplicating pages, and updating the content accordingly. You can find helpful information on using DocFX and writing Markdown [here](https://dotnet.github.io/docfx/docs/markdown.html). We will also assist you in getting started with the documentation.

Enter the following command to install the latest docfx

```
dotnet tool install -g docfx
```

Locate the docs folder on your computer

```
stride-community-toolkit\docs\

```

Run the following command to build the documentation

```
run.bat
```

You should be able to access the documentation at `http://localhost:8080/`

You can make changes to the documentation by editing the markdown files in the `docs` folder. Once you are done, re-run the `run.bat` file to see the changes.

 Once you are happy with your results and have tested the changes, you can submit a pull request to the `main` branch.