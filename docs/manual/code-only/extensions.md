# Extensions

## GameExtensions.cs

These extensions are used to simplify the game creation for the most common scenarios in **code only** projects. They are not required to run the game, but they can save you some time starting learning Stride.

The extensions are using lots of defaults to help you get started. If you want to change the defaults, you can always check how the extension is implemented and create your own version.

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