using Stride.Core.Annotations;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Engine.EntityProcessors {

    /// <summary>
    /// An entity processor where component and data are the same, and easy
    /// parallelisation of operations is supported via the Dispatcher.
    /// </summary>
    /// <typeparam name="TComponent"></typeparam>
    public abstract class ParallelProcessor<TComponent> : EntityProcessor<TComponent> where TComponent : EntityComponent, IParallelComponent
    {

        /* Keep track of all components in a list as we need to get them by index */
        private List<TComponent> componentList = new List<TComponent>();

        public override void Update(GameTime time)
        {

            /* Allow the entity processor implementation to hook into things *before* the parallel processing... */
            BeforeUpdate();

            /* Dispatch UpdateInParallel across every ComponentData */
            Dispatcher.For(0, componentList.Count, i =>
            {
                componentList[i].UpdateInParallel();
            });

            /* ... and after */
            AfterUpdate();

        }

        /// <summary>
        /// Invoked before the component's UpdateInParallel() method. Should contain any non-thread-safe initialisation.
        /// </summary>
        abstract protected void BeforeUpdate();

        /// <summary>
        /// Invoked after the component's UpdateInParallel() method. Should contain any non-thread-safe post-processing.
        /// </summary>
        abstract protected void AfterUpdate();

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] TComponent component, [NotNull] TComponent data)
        {
            componentList.Add(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] TComponent component, [NotNull] TComponent data)
        {
            componentList.Remove(component);
        }

    }

}