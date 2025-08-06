using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;
using System;

namespace Example18_Box2DPhysics.Helpers
{
    public static class Box2DPolygonSDFShaderKeys
    {
        public static readonly ValueParameterKey<Vector2> PolygonPoint0 = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> PolygonPoint1 = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> PolygonPoint2 = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> PolygonPoint3 = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> PolygonPoint4 = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> PolygonPoint5 = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> PolygonPoint6 = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> PolygonPoint7 = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<int> PolygonCount = ParameterKeys.NewValue<int>();
        public static readonly ValueParameterKey<float> PolygonRadius = ParameterKeys.NewValue<float>();
        public static readonly ValueParameterKey<float> PolygonThickness = ParameterKeys.NewValue<float>();
        public static readonly ValueParameterKey<Color4> PolygonColor = ParameterKeys.NewValue<Color4>();
    }

    public class PolygonSDFRenderer : IDisposable
    {
        private readonly GraphicsDevice _device;
        private readonly EffectInstance _effectInstance;
        private readonly VertexBufferBinding _vertexBuffer;
        private readonly PipelineState _pipelineState;
        private readonly VertexDeclaration _vertexLayout;

        // Vertex struct with position and texcoord
        private struct VertexPositionTexCoord
        {
            public Vector2 Position;
            public Vector2 TexCoord;
        }

        public PolygonSDFRenderer(GraphicsDevice device, EffectSystem effectSystem)
        {
            _device = device;
            // Load the custom SDF shader effect
            _effectInstance = new EffectInstance(effectSystem.LoadEffect("Box2DPolygonSDFShader").WaitForResult());
            _effectInstance.UpdateEffect(device);

            // Fullscreen quad (two triangles, CCW) with position and texcoord
            var quadVertices = new[]
            {
                new VertexPositionTexCoord { Position = new Vector2(-1, -1), TexCoord = new Vector2(0, 1) },
                new VertexPositionTexCoord { Position = new Vector2(-1,  1), TexCoord = new Vector2(0, 0) },
                new VertexPositionTexCoord { Position = new Vector2( 1, -1), TexCoord = new Vector2(1, 1) },
                new VertexPositionTexCoord { Position = new Vector2( 1,  1), TexCoord = new Vector2(1, 0) },
            };
            _vertexLayout = new VertexDeclaration(
                VertexElement.Position<Vector2>(),
                new VertexElement("TEXCOORD", 0, PixelFormat.R32G32_Float, 8)
            );
            var vertexBuffer = Buffer.Vertex.New(device, quadVertices);
            _vertexBuffer = new VertexBufferBinding(vertexBuffer, _vertexLayout, quadVertices.Length);

            // Pipeline state
            var pipelineDesc = new PipelineStateDescription
            {
                BlendState = BlendStates.AlphaBlend,
                RasterizerState = RasterizerStates.CullNone,
                DepthStencilState = DepthStencilStates.None,
                PrimitiveType = PrimitiveType.TriangleStrip,
                InputElements = _vertexLayout.CreateInputElements(),
                EffectBytecode = _effectInstance.Effect.Bytecode,
                RootSignature = _effectInstance.RootSignature,
                Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
            };
            _pipelineState = PipelineState.New(device, ref pipelineDesc);
        }

        public void DrawPolygon(CommandList commandList, GraphicsContext graphicsContext, Vector2[] points, int count, float radius, float thickness, Color4 color)
        {
            if (points.Length > 8) throw new ArgumentException("Max 8 points supported");
            // Set shader parameters using strongly-typed keys
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonCount, count);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonRadius, radius);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonThickness, thickness);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonColor, color);
            // Pad points to 8 and set individually
            var pts = new Vector2[8];
            Array.Clear(pts, 0, 8);
            Array.Copy(points, pts, count);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonPoint0, pts[0]);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonPoint1, pts[1]);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonPoint2, pts[2]);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonPoint3, pts[3]);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonPoint4, pts[4]);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonPoint5, pts[5]);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonPoint6, pts[6]);
            _effectInstance.Parameters.Set(Box2DPolygonSDFShaderKeys.PolygonPoint7, pts[7]);

            _effectInstance.Apply(graphicsContext);
            commandList.SetPipelineState(_pipelineState);
            commandList.SetVertexBuffer(0, _vertexBuffer.Buffer, 0, _vertexLayout.VertexStride);
            commandList.Draw(_vertexBuffer.Count);
        }

        public void Dispose()
        {
            _vertexBuffer.Buffer.Dispose();
            _pipelineState.Dispose();
        }
    }
}
