# Code Only API Docs

*Work in progress..*

This library is just a preview, expect breaking changes.

| Class Name | Description
| --- | --- |
| BasicCameraController | Stride.Assets.Presentation, Assets -> Scripts -> Camera|
| CameraComponentExtensions | Implements ```ScreenPointToRay()``` | |
| GameExtensions | See below |
| GameProfiler | Stride.Assets.Presentation, Assets -> Scripts -> Utility |
| GraphicsCompositorBuilder | |
| PrimitiveModelType |  |
| SkyboxGenerator        | |
| SkyboxGeneratorContext |  |

## CameraComponentExtensions.cs

| Extensions | Status | Note
| --- | --- | --- |
| ```ScreenPointToRay()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| Returns near and far vector based on a ray going from camera through a screen point. |

## GameExtensions.cs

The methods which are done can be used but most likely would need some refactoring, because they are duplicating code from the engine. More research needs to be done if this could be moved to Stride engine.

Some extensions return ```Entity``` so it can be further modified.

| Extensions | Status | Note
| --- | --- | --- |
| ```Run()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| Unitialising the game, use ```start``` and ```update``` params |
| ```SetupBase()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| Adds Graphics Compositor, Camera and Directional Light |
| ```SetupBase3DScene()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| Same as ```SetupBase()``` plus SkyBox, Ground, MouseLookCamera |
| ```SetupBase2DScene()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)| |
| ```AddGraphicsCompositor()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | Adds Graphic Compositor with Clean UI |
| ```AddCamera()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) ||
| ```AddDirectionalLight()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) ||
| ```AddSkybox()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | |
| ```AddMouseLookCamera()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | The camera entity can be moved using W, A, S, D, Q and E, arrow keys |
| ```NewDefaultMaterial()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | Adds basic material |
| ```CreatePrimitive()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | Simplifies primitives creation |
| ```AddProfiler()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | Attaches profile |
| ```AddGizmo()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)| Debug. To see X,Y,Z arrows|
| ```AddEntityNames()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)| Debug. To see entity properties in the game|
| ```AddPhysicsDebugger()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)| Debug. To see colliders. Tracked here [Issue #9](https://github.com/VaclavElias/stride-code-only/issues/9)|

## Other Requests & Features Tracking
| Title | Status | Note
| --- | --- | --- |
| [#8 Load Assets](https://github.com/VaclavElias/stride-code-only/issues/8) |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|Import and update assets|
| [#2 Code Only + Editor](https://github.com/VaclavElias/stride-code-only/issues/2) |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|Seamless workflow for both options|
| [#7 dotnet new template](https://github.com/VaclavElias/stride-code-only/issues/2) |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|Can we use templates?|



