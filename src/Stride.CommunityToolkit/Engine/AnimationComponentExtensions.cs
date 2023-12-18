using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

public static class AnimationComponentExtensions
{
    /// <summary>
    /// Plays the specified animation on the animation component if it is not already playing.
    /// </summary>
    /// <param name="animationComponent">The AnimationComponent on which to play the animation.</param>
    /// <param name="name">The name of the animation to be played.</param>
    /// <remarks>
    /// This method first checks if the animation with the given name is already playing. If not, it starts playing the animation.
    /// It's useful for preventing the interruption or restart of an animation that is currently in progress.
    /// </remarks>
    public static void PlayAnimation(this AnimationComponent animationComponent, string name)
    {
        if (!animationComponent.IsPlaying(name))
        {
            animationComponent.Play(name);
        }
    }
}