# Extensions

Each extension has been crafted to address common game development scenarios. They encapsulate and abstract away some of the complexities involved in setting up these scenarios, thus allowing you to focus more on the game logic and less on the setup and configuration.

To modify an extension, you can examine its code to understand how it works. Once you grasp the underlying logic, you can modify it or even create a new extension that better suits your needs. Remember, these extensions are just tools to help you get started; don't be afraid to modify them or build your own to align with your unique requirements.

In conclusion, whether you are a beginner just starting out with Stride or an experienced developer looking for a quicker way to get your game up and running, these extensions are a valuable resource. They are designed to be a starting point that can be used as is, or can be customised and built upon to create the perfect solution for your game development needs.

Remember, the key to mastering Stride, and game development in general, is practice and exploration. So, go ahead, play around with these extensions, and start creating!

## GameExtensions.cs

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