using Hexa.NET.ImGui;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ImGui;
using Stride.Core;
using Stride.Engine;
using Stride.Games;
using System.Numerics;
using static Hexa.NET.ImGui.ImGui;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// One ImGui window with fixed content at a fixed place: text in several colours, a button, a
/// checkbox, a progress bar, a plot and a colour swatch, so the integration's colour path and the
/// font atlas are pinned without any live number in the frame.
/// </summary>
internal sealed class ImGuiScene : IGoldScene
{
    public void Start(Game game, Scene scene)
    {
        game.SetupBase3D();

        // Registers itself with the game on construction; the game owns it from here
        _ = new ImGuiSystem(game.Services, game.GraphicsDeviceManager);
        _ = new GoldWindow(game.Services);
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
    }

    private sealed class GoldWindow : BaseWindow
    {
        private static readonly float[] Samples = [0.1f, 0.4f, 0.35f, 0.8f, 0.55f, 0.9f, 0.3f, 0.6f, 0.2f, 0.7f];
        private bool _checked = true;

        internal GoldWindow(IServiceRegistry services) : base(services)
        {
        }

        protected override Vector2? WindowPos => new Vector2(40f, 40f);

        protected override Vector2? WindowSize => new Vector2(360f, 300f);

        protected override void OnDraw(bool collapsed)
        {
            if (collapsed) return;

            Text("Gold window");
            TextColored(new Vector4(1f, 0.55f, 0.15f, 1f), "Orange text");
            TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "Blue text");
            Separator();

            Button("A button");
            SameLine();
            Checkbox("A checkbox", ref _checked);

            ProgressBar(0.65f, new Vector2(-1f, 0f), "65%");
            PlotLines("Plot", ref Samples[0], Samples.Length, 0, string.Empty, 0f, 1f, new Vector2(280f, 80f));
            ColorButton("Swatch", new Vector4(0.9f, 0.2f, 0.3f, 1f), ImGuiColorEditFlags.None, new Vector2(60f, 24f));
        }

        protected override void OnDestroy()
        {
        }
    }
}