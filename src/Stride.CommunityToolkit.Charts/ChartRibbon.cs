using Stride.CommunityToolkit.Charts.Lines;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// The ribbon mesh a space curve is drawn with, rebuilt when the view changes its width by more than a
/// twentieth. The one place a series owns geometry rather than submitting strokes.
/// </summary>
internal sealed class ChartRibbon : IDisposable
{
    // Emissive intensity standing in for a glow: bloom, where the scene has it, makes the ribbon bleed
    internal const float GlowEmissive = 2.5f;

    private readonly Chart _chart;
    private readonly Entity _entity;
    private List<IReadOnlyList<Vector3>> _runs = [];
    private bool _closed;
    private float _builtWidth = -1f;
    private float _builtGlow = -1f;

    internal ChartRibbon(Chart chart, string name)
    {
        _chart = chart;
        _entity = new Entity(name);
        _entity.Transform.Position = new Vector3(0f, 0f, Chart.CurveLayer);
        chart.Root.AddChild(_entity);
    }

    /// <summary>Takes new runs; the mesh is built on the next draw, when the view is known.</summary>
    internal void Rebuild(List<IReadOnlyList<Vector3>> runs, bool closed)
    {
        _runs = runs;
        _closed = closed;
        _builtWidth = -1f;
    }

    /// <summary>Builds or keeps the mesh for this frame's view.</summary>
    internal void Draw(in ChartView view, float pixelWidth, Color color, float glow)
    {
        if (_runs.Count == 0)
        {
            Release();

            return;
        }

        var r = _chart.Options.Range;
        var centre = new Vector3((r.XMin + r.XMax) * 0.5f, (r.YMin + r.YMax) * 0.5f, (r.ZMin + r.ZMax) * 0.5f);
        var width = view.ToUnits(pixelWidth, centre);

        if (_builtWidth > 0f && MathF.Abs(width - _builtWidth) <= _builtWidth * 0.05f && glow == _builtGlow)
            return;

        Release();

        var options = new PolylineOptions
        {
            Width = width,
            Color = color,
            EmissiveIntensity = glow > 0f ? GlowEmissive : 1f,
            Closed = _closed,
        };

        var mesh = _closed && _runs.Count == 1
            ? PolylineMeshBuilder.Build(_chart.Game.GraphicsDevice, _runs[0], options)
            : PolylineMeshBuilder.BuildMany(_chart.Game.GraphicsDevice, _runs, options);

        _entity.Add(PolylineMeshBuilder.CreateModel(_chart.Game, mesh, options));
        _builtWidth = width;
        _builtGlow = glow;
    }

    /// <summary>Releases the mesh and takes the entity off the chart.</summary>
    public void Dispose()
    {
        Release();
        _entity.Transform.Parent = null;
    }

    private void Release()
    {
        if (_entity.Get<ModelComponent>() is not { } model)
            return;

        if (model.Model is { } m)
        {
            foreach (var mesh in m.Meshes)
            {
                PolylineMeshBuilder.Release(mesh);
            }
        }

        _entity.Remove(model);
        _builtWidth = -1f;
    }
}