namespace Stride.Engine;

public static class AnimationComponentExtensions
{
    public static void PlayAnimation(this AnimationComponent animationComponent, string name)
    {
        if (!animationComponent.IsPlaying(name))
        {
            animationComponent.Play(name);
        }
    }
}