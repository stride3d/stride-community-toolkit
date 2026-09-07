
namespace Stride.CommunityToolkit.Engine.EntityProcessors {

    /// <summary>
    /// Interface for use on components handled by ParallelProcessors.
    /// </summary>
    public interface IParallelComponent {

        /// <summary>
        /// Called once per entity processor update, for each relevant component, in arbitrary order.
        /// Will be parallelised, so beware of thread safety.
        /// </summary>
        public void UpdateInParallel();

    }

}