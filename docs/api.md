# Code Only API Docs

*Work in progress..*

This library is just a preview, expect breaking changes.

| Class Name | Description
| --- | --- |
| BasicCameraController | Stride.Assets.Presentation, Assets -> Scripts -> Camera|
| CameraComponentExtensions | Implements ```ScreenPointToRay()``` | |
| GameExtensions | |
| GameProfiler | Stride.Assets.Presentation, Assets -> Scripts -> Utility |
| GraphicsCompositorBuilder | |
| PrimitiveModelType |  |
| SkyboxGenerator        | |
| SkyboxGeneratorContext |  |

## CameraComponentExtensions.cs

| Extensions | Status | Note
| --- | --- | --- |
| ```ScreenPointToRay()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| |

## GameExtensions.cs

The methods which are done can be used but most likely would need some refactoring, because they are duplicating code from the engine. More research needs to be done if this could be moved to Stride engine.

| Extensions | Status | Note
| --- | --- | --- |
| ```Run()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| |
| ```SetupBase()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| |
| ```SetupBase3DScene()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| |
| ```SetupBase2DScene()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)| |
| ```AddGraphicsCompositor()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | |
| ```AddCamera()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | |
| ```AddDirectionalLight()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | |
| ```AddSkybox()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | |
| ```AddMouseLookCamera()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | |
| ```AddGizmo()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|To see X,Y,Z arrows|
| ```AddEntityNames()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|To see entity properties in the game|
| ```AddPhysicsDebugger()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|To see colliders|

## Other Requests & Features Tracking
| Title | Status | Note
| --- | --- | --- |
| [#8 Load Assets](https://github.com/VaclavElias/stride-code-only/issues/8) |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|Import and update assets|
| [#2 Code Only + Editor](https://github.com/VaclavElias/stride-code-only/issues/2) |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|Seamless workflow for both options|



