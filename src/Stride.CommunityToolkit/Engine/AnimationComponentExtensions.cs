using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="AnimationComponent"/> to enhance animation playback functionality.
/// </summary>
/// <remarks>
/// These extensions simplify the process of controlling animations, such as checking if an animation is already playing before starting it.
/// </remarks>
public static class AnimationComponentExtensions
{
    /// <summary>
    /// Plays the specified animation on the animation component if it is not already playing.
    /// </summary>
    /// <param name="animationComponent">The <see cref="AnimationComponent"/> on which to play the animation.</param>
    /// <param name="name">The name of the animation to be played.</param>
    /// <remarks>
    /// This method first checks if the animation with the given name is already playing. If it is not, the animation will be started.
    /// This approach prevents restarting an animation that is currently running, allowing for smooth transitions between animations.
    /// </remarks>
    public static void PlayAnimation(this AnimationComponent animationComponent, string name)
    {
        if (!animationComponent.IsPlaying(name))
        {
            animationComponent.Play(name);
        }
    }
}