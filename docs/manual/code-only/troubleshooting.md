# Troubleshooting

## Building Project Issues
1. Error - Could not load native library libcore using CPU architecture x64
   - Make sure you installed Visual C++ Redistributable
   ```
    C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\Stride.Core.Assets.CompilerApp.targets(132,5): error MSB3073: The command ""C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\..\tools\net6.0-windows7.0\Stride.Core.Assets.CompilerApp.exe"  --disable-auto- 
    compile --project-configuration "Debug" --platform=Windows --project-configuration=Debug --compile-property:StrideGraphicsApi=Direct3D11 --output-path="C:\Projects\StrideDemo\bin\Debug\net6.0\data" --build-path="C:\Projects\StrideDemo\obj\stride\assetbuild\data" --package-file="C:\Projects\StrideDemo\StrideDemo.csproj" --msbuild-up 
    todatecheck-filebase="C:\Projects\StrideDemo\obj\Debug\net6.0\stride\assetcompiler-uptodatecheck"" exited with code -532462766. [C:\Projects\StrideDemo\StrideDemo.csproj]
   ```
1. Error - Package 'runtime.ubuntu.16.10-x64.runtime.native.System.Security.Cryptography.OpenSsl 4.3.0' from source .. : The repository primary signature's timestamping certificate is not trusted by the trust provider
   - Restore the package CodeCapital.Stride.GameDefaults again ```dotnet restore``` 