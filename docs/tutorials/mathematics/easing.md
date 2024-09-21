# Using Easing Functions: Animating Position and Material Color

**Easing functions** are used in animations to create smooth transitions between values over a specified time. In this tutorial, we'll explore how to use easing functions not only to move a 3D object but also to interpolate its material color. This will allow us to create animations that blend both movement and visual effects.

## What You'll Learn
- How to create a 3D primitive (cube).
- How to implement easing functions for smooth transitions in animations.
- How to animate the movement of a 3D object using easing functions.
- How to interpolate and change the material color of a 3D object dynamically.

## Code Walkthrough

[!code-csharp[Adding a Texture at Runtime](../../../examples/snippets/Easing_Example01/Program.cs)]

## Code Breakdown

- **Position Animation:** The `MathUtilEx.Interpolate` method is used with a quintic easing function to smoothly transition the sphere's position from its start to its end position.
- **Material Color Animation:** The same interpolation approach is applied to change the color of the sphere’s material. A **linear easing function** is used to gradually change the color from white to a randomly generated color.
- **Resetting the Animation:** The animation is reset each time the spacebar is pressed, allowing the movement and color transition to start over.

## Running the Code

When you run the code, you'll see a 3D sphere smoothly moving from the starting position (8 units above the ground) to the bottom (2 units above the ground) over a duration of 2 seconds. At the same time, the sphere’s material color will gradually change from white to a randomly generated color. You can press the **spacebar** to reset the animation and see the sphere rise back to the top while its color returns to white.

## Summary

In this tutorial, you learned how to animate both the position and material color of a 3D object using easing functions. This technique allows you to create smooth and visually appealing transitions, which are essential for creating polished game experiences.