# Stride Community Toolkit - Examples Launcher

An Avalonia-based launcher application for browsing and running Stride Community Toolkit examples.

## Features

- **Browse Examples**: View all available examples organized by category (Basic, Advanced, Other)
- **Search**: Filter examples by title, ID, or category
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
cd src/Stride.CommunityToolkit.Examples.Launcher
dotnet run
```

### Controls

- **Search Box**: Type to filter examples by name, ID, or category
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

The launcher reuses the example discovery logic from `Stride.CommunityToolkit.Examples` by linking the Core files:

- `Constants.cs` - Shared constants
- `Example.cs` - Example model
- `ExampleProjectMeta.cs` - Project metadata
- `ExampleProvider.cs` - Example discovery logic
- `ColorHelper.cs` - Color utilities
- `ProjectFileHelper.cs` - Project file parsing

Examples are discovered by scanning `examples/code-only/` for projects with `<ExampleTitle>` metadata.

## Technical Details

- **Framework**: .NET 8
- **UI**: Avalonia 11.3.7
- **Pattern**: MVVM-lite (no formal framework)
- **Process Management**: Spawns examples as separate processes
- **Output Filtering**: Regex-based warning suppression

## Future Enhancements

Potential improvements:

- Visual preview/screenshots for examples
- Favorites/pinning
- Category grouping in UI
- Example descriptions
- Launch configuration (custom args, env vars)
- Recent examples list
- Multiple example launch
- Integrated terminal
