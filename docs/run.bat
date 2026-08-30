REM Regenerate the example pages, landing pages and toc from the examples' metadata blocks.
REM The generator's default paths are relative to the current directory and assume it is run from
REM its own project folder, so every path is passed explicitly here (this script runs from docs\).
dotnet run --no-launch-profile --project ..\tools\Stride.CommunityToolkit.Examples.MetadataGenerator\Stride.CommunityToolkit.Examples.MetadataGenerator.csproj -- docs ..\examples\code-only --docs-path manual\code-only\examples --media-path manual\code-only\examples\media

REM Delete all .yml files in the api directory
del /Q /S "api\*.yml"

REM Delete all .yml files in the api directory
del /Q /S "_site\api\*.*"
del /Q /S "_site\manual\*.*"

REM Delete the .manifest file in the api directory
del /Q "api\.manifest"

REM --maxParallelism 1

docfx docfx.json --maxParallelism 1 --serve