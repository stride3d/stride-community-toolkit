# ScriptSystemExtensions.cs

![Done](https://img.shields.io/badge/status-done-green)

- [`DelayWarped()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.DelayWarped(Stride.Engine.Processors.ScriptSystem,System.Single)) - Waits for a specified amount of time while taking into account the Update Time factor
- [`Delay()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.Delay(Stride.Engine.Processors.ScriptSystem,System.Single)) - Waits for a specified amount of time without considering the Update Time factor
- [`ExecuteInWarpedTime()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.ExecuteInWarpedTime(Stride.Engine.Processors.ScriptSystem,System.Single,System.Action{System.Single})) - Continuously executes an action every frame during a specified amount of time while taking into account the Update Time factor
- [`ExecuteInTime()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.ExecuteInTime(Stride.Engine.Processors.ScriptSystem,System.Single,System.Action{System.Single})) - Continuously executes an action every frame during a specified amount of time without considering the Update Time factor