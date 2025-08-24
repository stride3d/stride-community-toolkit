# Contribute Examples

All examples live in the [examples](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only) folder.

If you'd like your example to be launchable from the console application [Stride.CommunityToolkit.Examples](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit.Examples), follow these steps:
  
1. Create a project under `examples/code-only/` named `ExampleXY_YourExampleNamespace` (replace `XY` with the next available number).
2. Add example metadata to the `*.csproj` (used in the console app menu):
    ```xml
    <ExampleTitle>Basic Example - Capsule with rigid body</ExampleTitle>
    <ExampleOrder>100</ExampleOrder>
    <ExampleEnabled>true</ExampleEnabled>
    <ExampleCategory>1 - Basic Example</ExampleCategory>
    ```
3. If `<ExampleEnabled>true</ExampleEnabled>`, it will automatically appear in the console menu.
4. Run `Stride.CommunityToolkit.Examples`.
5. You should see your example listed in the console application menu.
   [!INCLUDE [examples-console-app](../../includes/manual/examples/examples-console-app.md)]
6. Update `Stride.CommunityToolkit.Docs/includes/manual/examples/basic-examples-outro.md` to include the new example.
7. Update `Stride.CommunityToolkit.Docs/includes/manual/basic-examples.md`, `advance-examples.md`, or `other-examples.md` to include the new example.