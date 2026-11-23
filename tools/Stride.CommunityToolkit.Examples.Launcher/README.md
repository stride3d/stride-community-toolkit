# Stride Community Toolkit - Examples Launcher

An Avalonia-based launcher application for browsing and running Stride Community Toolkit examples.

## Features

- **Browse Examples**: Every published example, in manifest order: by language, then level (Getting Started, Beginner, Intermediate, Advanced, Other), then the example's own order
- **Search**: Filter examples by title, slug, project name, level, category or tag
- **Details**: Level, category, complexity, language, description and tags of the selected example
- **One-Click Run**: Launch examples with a single click
- **Live Output**: View real-time stdout/stderr output from running examples
- **Filtered Warnings**: Automatically filters shader/effect compilation warnings (can be disabled)
- **Project Management**:
  - Open example project folder in Explorer
  - Copy `dotnet run` command to clipboard
  - Stop running examples

## Usage

### Running the Launcher

```bash
cd tools/Stride.CommunityToolkit.Examples.Launcher
dotnet run
```

### Controls

- **Search Box**: Type to filter examples by title, slug, project name, level, category or tag
- **▶ Run**: Start the selected example
- **⏹ Stop**: Stop the currently running example
- **📁 Open Folder**: Open the example's project folder
- **📋 Copy Command**: Copy the `dotnet run` command to clipboard
- **Clear**: Clear the output log

### Environment Variables

- `SHOW_WARNINGS=1`: Show all warnings including filtered shader/effect warnings

Example:
```bash
set SHOW_WARNINGS=1
dotnet run
```

## Architecture

The launcher reads `examples-manifest.json`, which `tools/ExamplesManifest.targets` generates before the
build and copies next to the executable. It scans nothing itself: the metadata generator has already
validated, filtered and sorted the examples.

Reading the manifest is shared with the console launcher, `Stride.CommunityToolkit.Examples`, by
linking its Core files into this project:

- `ExampleManifest.cs` - The manifest envelope
- `ManifestExample.cs` - One example as the manifest stores it
- `ManifestLoader.cs` - Finds and reads the manifest, and resolves each example's run target
- `ExampleEntry.cs` - An example ready to display and start
- `Levels.cs` - The level names
- `OutputFilter.cs` - Which output lines are hidden or shown as warnings
- `Constants.cs` - The titles of the console menu's commands

## Technical Details

- **Framework**: .NET 10
- **UI**: Avalonia 12.1.6
- **Pattern**: MVVM-lite (no formal framework)
- **Process Management**: Spawns examples as separate processes
- **Output Filtering**: Regex-based warning suppression

## Future Enhancements

Potential improvements:

- Visual preview/screenshots for examples
- Favorites/pinning
- Level grouping in UI
- Launch configuration (custom args, env vars)
- Recent examples list
- Multiple example launch
- Integrated terminal