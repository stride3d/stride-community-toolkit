# Contribute Examples

You can see all examples in the [examples](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only) folder.

If you would like your example to be launchable from the console application [Stride.CommunityToolkit.Examples](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit.Examples), follow these steps:
  
1. Create a project in the folder `examples/code-only/` and name it `ExampleXY_YourExampleNamespace` (replace `XY` with the next available number).
1. Add your example meta data to the `*.csproj`; these will be used in the console application menu:
    ```xml
    <ExampleTitle>Basic Example - Capsule with rigid body</ExampleTitle>
    <ExampleOrder>100</ExampleOrder>
    <ExampleEnabled>true</ExampleEnabled>
    <ExampleCategory>1 - Basic Example</ExampleCategory>
    ```
1. If you enable your example `<ExampleEnabled>true</ExampleEnabled>`, it will automatically show up in the console application menu.
1. Run `Stride.CommunityToolkit.Examples`
1. You should see your example listed in the console application menu.
   [!INCLUDE [examples-console-app](../../examples-console-app.md)]
1. Update the `Stride.CommunityToolkit.Docs/includes/manual/examples/basic-examples-outro.md` file to include the new example.
1. Update the `Stride.CommunityToolkit.Docs/includes/manual/basic-examples.md`, `advance-examples.md`, or `other-examples.md` file to include the new example.