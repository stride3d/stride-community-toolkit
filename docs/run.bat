REM Delete all .yml files in the api directory
del /Q /S "api\*.yml"

REM Delete all .yml files in the api directory
del /Q /S "_site\api\*.*"
del /Q /S "_site\manual\*.*"

REM Delete the .manifest file in the api directory
del /Q "api\.manifest"

REM --maxParallelism 1

docfx docfx.json --serve --maxParallelism 1