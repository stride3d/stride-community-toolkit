# Script System Extensions

Extension methods for `ScriptSystem`, in the `Stride.CommunityToolkit.Engine` namespace. They cover two jobs: awaiting time inside an async script, and scheduling work as a micro thread without writing one yourself.

## Waiting

`Delay()` and `ExecuteInTime()` each have a *warped* twin. Warped time is scaled by the game's update time factor, so a warped wait slows down when the game does; the plain version does not. Neither counts while the game is paused, and both end early when the game stops.

- [`Delay()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.Delay(Stride.Engine.Processors.ScriptSystem,System.Single)) - Waits the given number of seconds of game time, ignoring the update time factor.
- [`DelayWarped()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.DelayWarped(Stride.Engine.Processors.ScriptSystem,System.Single)) - Waits the given number of seconds in warped time, so slow motion stretches the wait.
- [`WaitFor()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.WaitFor(Stride.Engine.Processors.ScriptSystem,System.TimeSpan)) - The same as `Delay()` but takes a `TimeSpan`. Awaiting it throws `ArgumentOutOfRangeException` if the delay is zero or negative.

## Running something every frame for a while

- [`ExecuteInTime()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.ExecuteInTime(Stride.Engine.Processors.ScriptSystem,System.Single,System.Action{System.Single})) - Calls the action once per frame for the given number of seconds, passing the seconds elapsed so far.
- [`ExecuteInWarpedTime()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.ExecuteInWarpedTime(Stride.Engine.Processors.ScriptSystem,System.Single,System.Action{System.Single})) - The same, measured in warped time.

## Scheduling micro threads

The `Add*` methods return the `MicroThread` they created, so you can keep it and cancel it later, and each takes an optional `priority`. A delay, interval or duration of zero or less throws `ArgumentOutOfRangeException`.

- [`AddAction()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.AddAction(Stride.Engine.Processors.ScriptSystem,System.Action,System.TimeSpan,System.Int64)) - Runs an action once, after a delay. A second overload adds a `repeatEvery` interval and keeps running it.
- [`AddTask()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.AddTask(Stride.Engine.Processors.ScriptSystem,System.Func{System.Threading.Tasks.Task},System.TimeSpan,System.Int64)) - The `async` counterpart of `AddAction()`, taking a `Func<Task>`. Also has a repeating overload.
- [`AddOverTimeAction()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.AddOverTimeAction(Stride.Engine.Processors.ScriptSystem,System.Action{System.Single},System.TimeSpan,System.Int64)) - Runs an action every frame for a duration, passing progress from `0.0f` to `1.0f`. This is the one to reach for when tweening something.
- [`AddOnEventAction()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.AddOnEventAction``1(Stride.Engine.Processors.ScriptSystem,Stride.Engine.Events.EventKey{``0},System.Action{``0},System.Int64)) - Runs an action every time an event is published. Takes an `EventKey<T>` or an existing `EventReceiver<T>`.
- [`AddOnEventTask()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.AddOnEventTask``1(Stride.Engine.Processors.ScriptSystem,Stride.Engine.Events.EventKey{``0},System.Func{``0,System.Threading.Tasks.Task},System.Int64)) - The `async` counterpart of `AddOnEventAction()`.
- [`CancelAll()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.CancelAll(System.Collections.Generic.ICollection{Stride.Core.MicroThreading.MicroThread})) - Cancels every micro thread in a collection and clears it. An extension on the collection, not on the script system.

## Reaching the game thread

Scenes, entities and components may only be touched from the game thread. A network callback, a timer or a task that finished on the thread pool hands its work over with these; the work runs on the next update.

- [`RunOnGameThread()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.RunOnGameThread(Stride.Engine.Processors.ScriptSystem,System.Action)) - Queues an action for the game thread, from any thread. Build the entity where you are, add it to the scene inside the action.
- [`RunOnGameThreadAsync<T>()`](xref:Stride.CommunityToolkit.Engine.ScriptSystemExtensions.RunOnGameThreadAsync``1(Stride.Engine.Processors.ScriptSystem,System.Func{``0})) - The same for a function whose result you need back, as a `Task<T>` to await on the calling thread.

```csharp
// On a thread-pool thread, once a download has finished
game.Script.RunOnGameThread(() => CreateCrate(bytes).Scene = rootScene);

var count = await game.Script.RunOnGameThreadAsync(() => rootScene.Entities.Count);
```

> [!TIP]
> When the action passed to an `Add*` method is an instance method of a `ScriptComponent`, the micro
> thread stops on its own once that component or its entity is removed. Pass a lambda that closes over
> other state and you own the lifetime yourself - keep the `MicroThread` and cancel it.
> `RunOnGameThread` always wraps the action, so its action runs even if its script was removed.

## See also

- [Script Extensions](../script-extensions/index.md)
- [Components and Scripts](../components-and-scripts.md)
- [Sync Script](../code-only/examples/sync-script.md) example