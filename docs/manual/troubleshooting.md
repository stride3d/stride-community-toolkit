# Troubleshooting

## Building Project Issues

1. **Error - Could not load native library libcore using CPU architecture x64**
   - Missing Microsoft Visual C++ Redistributable, make sure you install [prerequisites](getting-started.md)
       ```
        C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\Stride.Core.Assets.CompilerApp.targets(132,5): error MSB3073: The command ""C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\..\tools\net6.0-windows7.0\Stride.Core.Assets.CompilerApp.exe"  --disable-auto- 
        compile --project-configuration "Debug" --platform=Windows --project-configuration=Debug --compile-property:StrideGraphicsApi=Direct3D11 --output-path="C:\Projects\StrideDemo\bin\Debug\net6.0\data" --build-path="C:\Projects\StrideDemo\obj\stride\assetbuild\data" --package-file="C:\Projects\StrideDemo\StrideDemo.csproj" --msbuild-up 
        todatecheck-filebase="C:\Projects\StrideDemo\obj\Debug\net6.0\stride\assetcompiler-uptodatecheck"" exited with code -532462766. [C:\Projects\StrideDemo\StrideDemo.csproj]
       ```
1. **Error - Unable to instantiate compiler**
   - Missing Microsoft Visual C++ Redistributable, make sure you install [prerequisites](getting-started.md)
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
1. **Error - Exited with code 1.**
   - Missing Stride NuGet package dependencies. Use `dotnet restore --runtime win-x64` or `dotnet build --runtime win-x64` to restore the missing dependencies. This error can occur when the build is run for the first time on your computer.
        ```
        C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.2.0.2122\buildTransitive\Stride.Core.Assets.CompilerApp
        .targets(153,5): error MSB3073: The command "C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.2.0.2122\
        buildTransitive\..\lib\net8.0\Stride.Core.Assets.CompilerApp.exe"  --disable-auto-compile --project-configuration "Debu
        g" --platform=Windows --project-configuration=Debug --compile-property:StrideGraphicsApi=Direct3D11 --output-path="D:\P
        rojects\GitHub\Stride Projects\Console01\bin\Debug\net8.0\data" --build-path="D:\Projects\GitHub\Stride Projects\Consol
        e01\obj\stride\assetbuild\data" --package-file="D:\Projects\GitHub\Stride Projects\Console01\Console01.csproj" --msbuil
        d-uptodatecheck-filebase="D:\Projects\GitHub\Stride Projects\Console01\obj\Debug\net8.0\stride\assetcompiler-uptodatech
        eck" exited with code 1. [D:\Projects\GitHub\Stride Projects\Console01\Console01.csproj]
        ```
1. **Error - at Stride.Shaders.Compiler.TaskOrResult.**
   - If this is from code-only, make sure you added `Stride.CommunityToolkit.Windows` NuGet package instead of `Stride.CommunityToolkit`, further instructions can be found [here](code-only/create-project.md)
    ```
     at Stride.Shaders.Compiler.TaskOrResult`1.WaitForResult() in C:\BuildAgent\work\b5f46e3c4829a09e\sources\engine\Stride.Shaders\Compiler\TaskOrResult.cs:line 37
       at Stride.Rendering.DynamicEffectInstance.ChooseEffect(GraphicsDevice graphicsDevice) in C:\BuildAgent\work\b5f46e3c4829a09e\sources\engine\Stride.Rendering\Rendering\DynamicEffectInstance.cs:line 60
       at Stride.Rendering.EffectInstance.UpdateEffect(GraphicsDevice graphicsDevice)
       at Stride.Rendering.Images.ImageEffectShader.DrawCore(RenderDrawContext context) in C:\BuildAgent\work\b5f46e3c4829a09e\sources\engine\Stride.Rendering\Rendering\Images\ImageEffectShader.cs:line 147
       at Stride.Rendering.RendererBase.Draw(RenderDrawContext context) in C:\BuildAgent\work\b5f46e3c4829a09e\sources\engine\Stride.Rendering\Rendering\RendererBase.cs:line 51
       at Stride.Rendering.ComputeEffect.LambertianPrefiltering.LambertianPrefilteringSHNoCompute.DrawCore(RenderDrawContext context) in C:\BuildAgent\work\b5f46e3c4829a09e\sources\engine\Stride.Rendering\Rendering\ComputeEffect\LambertianPrefiltering\LambertianPrefilteringSHNoCompute.cs:line 99
       at Stride.Rendering.RendererBase.Draw(RenderDrawContext context) in C:\BuildAgent\work\b5f46e3c4829a09e\sources\engine\Stride.Rendering\Rendering\RendererBase.cs:line 51
       at Stride.CommunityToolkit.Skyboxes.SkyboxGenerator.Generate(Skybox skybox, SkyboxGeneratorContext context, Texture skyboxTexture) in D:\a\stride-community-toolkit\stride-community-toolkit\src\Stride.CommunityToolkit.Skyboxes\SkyboxGenerator.cs:line 49
       at Stride.CommunityToolkit.Skyboxes.GameExtensions.AddSkybox(Game game, String entityName) in D:\a\stride-community-toolkit\stride-community-toolkit\src\Stride.CommunityToolkit.Skyboxes\GameExtensions.cs:line 46
       at Program.<>c__DisplayClass0_0.<<Main>$>b__0(Scene rootScene) in C:\Users\user-name\RiderProjects\Label3d\Program.cs:line 13
       at Stride.CommunityToolkit.Engine.GameExtensions.<>c__DisplayClass0_0.<<Run>g__RootScript|0>d.MoveNext() in D:\a\stride-community-toolkit\stride-community-toolkit\src\Stride.CommunityToolkit\Engine\GameExtensions.cs:line 46  
    --- End of stack trace from previous location ---
       at Stride.Core.MicroThreading.MicroThread.<>c__DisplayClass52_0.<<Start>b__0>d.MoveNext() in C:\BuildAgent\work\b5f46e3c4829a09e\sources\core\Stride.Core.MicroThreading\MicroThread.cs:line 176
    --- End of stack trace from previous location ---
       at Stride.Core.MicroThreading.Scheduler.Run() in C:\BuildAgent\work\b5f46e3c4829a09e\sources\core\Stride.Core.MicroThreading\Scheduler.cs:line 203
       at Stride.Engine.Processors.ScriptSystem.Update(GameTime gameTime) in C:\BuildAgent\work\b5f46e3c4829a09e\sources\engine\Stride.Engine\Engine\Processors\ScriptSystem.cs:line 106
       at Stride.Games.GameSystemCollection.Update(GameTime gameTime)
       at Stride.Games.GameBase.Update(GameTime gameTime)
       at Stride.Games.GameBase.InitializeBeforeRun()
       at Stride.Games.GamePlatform.OnInitCallback()
       at Stride.Games.GameWindowSDL.Run()
       at Stride.Games.GamePlatform.Run(GameContext gameContext)
       at Stride.Games.GameBase.Run(GameContext gameContext)
       at Stride.CommunityToolkit.Engine.GameExtensions.Run(Game game, GameContext context, Action`1 start, Action`2 update) in D:\a\stride-community-toolkit\stride-community-toolkit\src\Stride.CommunityToolkit\Engine\GameExtensions.cs:line 42
       at Program.<Main>$(String[] args) in C:\Users\user-name\RiderProjects\Label3d\Program.cs:line 101. 
    ```