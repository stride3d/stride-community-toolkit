using Stride.Games;
using Stride.Rendering;

namespace Stride.Engine;

public static class ModelComponentExtensions
{
    /// <summary>
    /// Gets the ModelComponents BoundingBox and calculates the Y height
    /// </summary>
    /// <param name="modelComponent"></param>
    /// <returns></returns>
    public static float GetMeshHeight(this ModelComponent modelComponent)
    {
        var boundingBox = modelComponent.Model.BoundingBox;
        var height = boundingBox.Maximum.Y - boundingBox.Minimum.Y;

        return height;
    }

    /// <summary>
    /// Gets the ModelComponents BoundingBox and calculates the Height, Width and Length
    /// </summary>
    /// <param name="modelComponent"></param>
    /// <returns></returns>
    public static Vector3 GetMeshHWL(this ModelComponent modelComponent)
    {
        var boundingBox = modelComponent.Model.BoundingBox;
        var height = boundingBox.Maximum.Y - boundingBox.Minimum.Y;
        var width = boundingBox.Maximum.X - boundingBox.Minimum.X;
        var length = boundingBox.Maximum.Z - boundingBox.Minimum.Z;

        return new Vector3(height, width, length);
    }

    /// <summary>
    /// Gets the ModelComponents Mesh data as vertices and indices.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="game"></param>
    /// <returns></returns>
    public static (List<Vector3> verts, List<int> indices) GetMeshVerticesAndIndices(this ModelComponent model, IGame game)
    {
        return GetMeshData(model.Model, game.Services, game);
    }

    static unsafe (List<Vector3> verts, List<int> indices) GetMeshData(Model model, IServiceRegistry services, IGame game)
    {
        Matrix[] nodeTransforms = null;

        int totalVerts = 0, totalIndices = 0;
        foreach (var meshData in model.Meshes)
        {
            totalVerts += meshData.Draw.VertexBuffers[0].Count;
            totalIndices += meshData.Draw.IndexBuffer.Count;
        }

        var combinedVerts = new List<Vector3>(totalVerts);
        var combinedIndices = new List<int>(totalIndices);

        foreach (var meshData in model.Meshes)
        {
            var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
            var iBuffer = meshData.Draw.IndexBuffer.Buffer;
            byte[] verticesBytes = vBuffer.GetData<byte>(game.GraphicsContext.CommandList); ;
            byte[] indicesBytes = iBuffer.GetData<byte>(game.GraphicsContext.CommandList); ;

            if ((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
            {
                // returns empty lists if there is an issue
                return (combinedVerts, combinedIndices);
            }

            int vertMappingStart = combinedVerts.Count;

            fixed (byte* bytePtr = verticesBytes)
            {
                var vBindings = meshData.Draw.VertexBuffers[0];
                int count = vBindings.Count;
                int stride = vBindings.Declaration.VertexStride;
                for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                {
                    var pos = *(Vector3*)(bytePtr + vHead);
                    if (nodeTransforms != null)
                    {
                        Matrix posMatrix = Matrix.Translation(pos);
                        Matrix.Multiply(ref posMatrix, ref nodeTransforms[meshData.NodeIndex], out var finalMatrix);
                        pos = finalMatrix.TranslationVector;
                    }

                    combinedVerts.Add(pos);
                }
            }

            fixed (byte* bytePtr = indicesBytes)
            {
                if (meshData.Draw.IndexBuffer.Is32Bit)
                {
                    foreach (int i in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        combinedIndices.Add(vertMappingStart + i);
                    }
                }
                else
                {
                    foreach (ushort i in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        combinedIndices.Add(vertMappingStart + i);
                    }
                }
            }
        }
        return (combinedVerts, combinedIndices);
    }
}