REM Delete all .yml files in the api directory
del /Q /S "api\*.yml"

REM Delete all .yml files in the api directory
REM del /Q /S "site\api\*.*"

REM Delete the .manifest file in the api directory
del /Q "api\.manifest"

REM --maxParallelism 1

docfx docfx.json --serve