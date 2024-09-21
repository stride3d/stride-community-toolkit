# Raycasting and Camera Focus

In this tutorial, we will learn how to use **raycasting** to detect entities in a 3D scene and adjust the camera to focus on them. This technique is useful for games or simulations where you need to interact with objects using the mouse and smoothly transition the camera's focus based on those interactions.

## What You'll Learn

- How to create 3D entities and assign materials to them.
- How to use **raycasting** to detect objects in a 3D scene.
- How to use the camera to smoothly look at the target entity.

## Code Walkthrough

[!code-csharp[Pick and Aim](../../../examples/snippets/Physics_Example001/Program.cs)]

## Code Walkthrough

- **Game Setup:** In the `Start()` method, we set up a basic 3D scene with lighting and a camera using the `SetupBase3DScene()` helper method. We then create two 3D entities, a **cube** and a **sphere**, and position them in the scene with different materials.

- **Raycasting with Mouse Input:** In the `Update()` method, we check if the left mouse button is pressed. If pressed, a ray is cast from the mouse position into the 3D world using `ScreenToWorldRaySegment()`. This ray is used to check for collisions with 3D entities in the scene via **raycasting**.

- **Camera Focus:** If the ray successfully hits an entity, that entity becomes the **target**. The camera then uses the `LookAt()` method to smoothly focus on the target entity, giving the player a clear view of the object they clicked on.

## Running the Code

When you run this code, the game will display a 3D scene with a cube and a sphere. By clicking on either object with the mouse, the camera will smoothly rotate to focus on the clicked object. The left mouse button controls the focus.

## Summary

This example demonstrates how to use raycasting to detect entities in a scene based on mouse input and how to adjust the camera to focus on those entities. This technique is useful for games that require interactive environments, object selection, or camera-based interactions.

Feel free to extend this concept by adding more entities, adjusting the camera's behavior, or experimenting with different easing functions for camera movement.