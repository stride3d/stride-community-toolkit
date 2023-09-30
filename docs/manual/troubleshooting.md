# Troubleshooting

## Building Project Issues

1. **Error - Could not load native library libcore using CPU architecture x64**
   - Missing Microsoft Visual C++ Redistributable
       ```
        C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\Stride.Core.Assets.CompilerApp.targets(132,5): error MSB3073: The command ""C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\..\tools\net6.0-windows7.0\Stride.Core.Assets.CompilerApp.exe"  --disable-auto- 
        compile --project-configuration "Debug" --platform=Windows --project-configuration=Debug --compile-property:StrideGraphicsApi=Direct3D11 --output-path="C:\Projects\StrideDemo\bin\Debug\net6.0\data" --build-path="C:\Projects\StrideDemo\obj\stride\assetbuild\data" --package-file="C:\Projects\StrideDemo\StrideDemo.csproj" --msbuild-up 
        todatecheck-filebase="C:\Projects\StrideDemo\obj\Debug\net6.0\stride\assetcompiler-uptodatecheck"" exited with code -532462766. [C:\Projects\StrideDemo\StrideDemo.csproj]
       ```
2. **Error - Unable to instantiate compiler**
   - Missing Microsoft Visual C++ Redistributable
        ```
        EXEC : error 6.206s: [AssetsCompiler.AttributeBasedRegistry] Unable to instantiate compiler [Stride.A
        ssets.Physics.ColliderShapeAsset, Stride.Assets, Version=4.1.0.1898, Culture=neutral, PublicKeyToken=
        null]. Exception: TargetInvocationException: Exception has been thrown by the target of an invocation
        . [C:\Examples\Example01\Example01.csproj]
            ---> TypeInitializationException: The type initializer for 'Stride.Assets.Physics.ColliderShapeAss
            etCompiler' threw an exception.
            ---> InvalidOperationException: Could not load native library VHACD using CPU architecture x64.
                at Stride.Core.NativeLibraryHelper.PreloadLibrary(String libraryName, Type owner) in C:\BuildAge
            nt\work\b5f46e3c4829a09e\sources\core\Stride.Core\Native\NativeLibraryHelper.cs:line 156
                at Stride.Assets.Physics.ColliderShapeAssetCompiler..cctor() in C:\BuildAgent\work\b5f46e3c4829a
            09e\sources\engine\Stride.Assets\Physics\ColliderShapeAssetCompiler.cs:line 30
                at Stride.Assets.Physics.ColliderShapeAssetCompiler..ctor()
                at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
                at System.RuntimeType.CreateInstanceDefaultCtor(Boolean publicOnly, Boolean wrapExceptions)
                at Stride.Core.Assets.Compiler.AssetCompilerRegistry.ProcessAttribute(AssetCompilerAttribute com
            pilerCompilerAttribute, Type type) in C:\BuildAgent\work\b5f46e3c4829a09e\sources\assets\Stride.Cor
            e.Assets\Compiler\AssetCompilerRegistry.cs:line 161
                at Stride.Core.Assets.Compiler.AssetCompilerRegistry.RegisterCompilersFromAssembly(Assembly asse
            mbly) in C:\BuildAgent\work\b5f46e3c4829a09e\sources\assets\Stride.Core.Assets\Compiler\AssetCompil
            erRegistry.cs:line 198
            System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocati
            on.
        ```