# Animation Extensions

Extension methods for `AnimationComponent`, in the `Stride.CommunityToolkit.Engine` namespace.

- [`PlayAnimation()`](xref:Stride.CommunityToolkit.Engine.AnimationComponentExtensions.PlayAnimation(Stride.Engine.AnimationComponent,System.String)) - Plays the named animation clip. Does nothing if that clip is already playing, so it is safe to call every frame from an input handler.