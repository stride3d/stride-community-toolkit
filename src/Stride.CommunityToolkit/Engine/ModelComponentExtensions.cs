using Stride.CommunityToolkit.Rendering;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="ModelComponent"/> class, enabling additional functionality
/// such as calculating model dimensions and extracting mesh data.
/// </summary>
public static class ModelComponentExtensions
{
    /// <summary>
    /// The size of the model's bounding box: its width along X, height along Y and depth along Z.
    /// </summary>
    /// <param name="modelComponent">The component whose model is measured.</param>
    /// <returns>The extent of the bounding box on each axis, in the model's own units.</returns>
    public static Vector3 GetModelSize(this ModelComponent modelComponent)
        => modelComponent.Model.BoundingBox.Maximum - modelComponent.Model.BoundingBox.Minimum;

    /// <summary>
    /// Calculates the height of the model represented by the ModelComponent.
    /// </summary>
    /// <param name="modelComponent">The ModelComponent whose model height is to be calculated.</param>
    /// <returns>The height of the model along the Y-axis.</returns>
    /// <remarks>
    /// The height is calculated based on the model's bounding box, which encompasses its maximum extent in 3D space.
    /// </remarks>
    public static float GetMeshHeight(this ModelComponent modelComponent)
        => modelComponent.Model.BoundingBox.Maximum.Y - modelComponent.Model.BoundingBox.Minimum.Y;

    /// <summary>
    /// Retrieves the vertices and indices from the ModelComponent's mesh data.
    /// </summary>
    /// <param name="model">The ModelComponent from which to extract mesh data.</param>
    /// <param name="game">The game instance, used to access graphics context for data extraction.</param>
    /// <returns>A tuple containing two lists: the first list holds the vertices (as <see cref="Vector3"/>), and the second list holds the indices (as <see cref="int"/>).</returns>
    /// <remarks>
    /// This method extracts raw vertex and index data from the meshes in the provided ModelComponent.
    /// It's useful for operations that require direct access to mesh data, such as custom rendering, collision detection, or physics simulations.
    /// </remarks>
    public static (List<Vector3> vertices, List<int> indices) GetMeshVerticesAndIndices(this ModelComponent model, IGame game)
        => GetMeshData(model.Model, game);

    /// <summary>
    /// Sets an object of the material pass parameter. Cloning the <see cref="Material"/> if required.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
    /// <param name="parameterAccessor">The parameter to update.</param>
    /// <param name="value">The value.</param>
    /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
    /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="materialIndex"/> is less than 0 or greater than or equal to <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the number of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ObjectParameterAccessor<T> parameterAccessor, T value, int materialIndex = 0, int passIndex = 0)
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameterAccessor, value);

    /// <summary>
    /// Sets a blittable value of the material pass parameter. Cloning the <see cref="Material"/> if required.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
    /// <param name="parameter">The parameter to update.</param>
    /// <param name="value">The value.</param>
    /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
    /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="materialIndex"/> is less than 0 or greater than or equal to <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the number of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameter<T> parameter, T value, int materialIndex = 0, int passIndex = 0) where T : struct
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);

    /// <summary>
    /// Sets blittable values of the material pass parameter. Cloning the <see cref="Material"/> if required.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
    /// <param name="parameter">The parameter to update.</param>
    /// <param name="count">Number of values.</param>
    /// <param name="firstValue">The values.</param>
    /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
    /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="materialIndex"/> is less than 0 or greater than or equal to <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the number of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameter<T> parameter, int count, ref T firstValue, int materialIndex = 0, int passIndex = 0) where T : struct
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, count, ref firstValue);

    /// <inheritdoc cref="SetMaterialParameter{T}(ModelComponent, ValueParameter{T}, T, int, int)"/>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameter<T> parameter, ref T value, int materialIndex = 0, int passIndex = 0) where T : struct
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, ref value);

    /// <inheritdoc cref="SetMaterialParameter{T}(ModelComponent, ValueParameter{T}, int, ref T, int, int)"/>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, int count, ref T firstValue, int materialIndex = 0, int passIndex = 0) where T : struct
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, count, ref firstValue);

    /// <summary>
    /// Sets blittable values of the material pass parameter. Cloning the <see cref="Material"/> if required.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
    /// <param name="parameter">The parameter to update.</param>
    /// <param name="values">The values.</param>
    /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
    /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="materialIndex"/> is less than 0 or greater than or equal to <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the number of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, T[] values, int materialIndex = 0, int passIndex = 0) where T : struct
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, values);

    /// <inheritdoc cref="SetMaterialParameter{T}(ModelComponent, ValueParameter{T}, T, int, int)"/>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, ref T value, int materialIndex = 0, int passIndex = 0) where T : struct
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, ref value);

    /// <inheritdoc cref="SetMaterialParameter{T}(ModelComponent, ValueParameter{T}, T, int, int)"/>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, T value, int materialIndex = 0, int passIndex = 0) where T : struct
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);

    /// <summary>
    /// Sets a permutation of the material pass parameter. Cloning the <see cref="Material"/> if required.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
    /// <param name="parameter">The parameter to update.</param>
    /// <param name="value">The value.</param>
    /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
    /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="materialIndex"/> is less than 0 or greater than or equal to <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the number of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, PermutationParameter<T> parameter, T value, int materialIndex = 0, int passIndex = 0)
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);

    /// <inheritdoc cref="SetMaterialParameter{T}(ModelComponent, ObjectParameterAccessor{T}, T, int, int)"/>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ObjectParameterKey<T> parameter, T value, int materialIndex = 0, int passIndex = 0)
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);

    /// <inheritdoc cref="SetMaterialParameter{T}(ModelComponent, PermutationParameter{T}, T, int, int)"/>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, PermutationParameterKey<T> parameter, T value, int materialIndex = 0, int passIndex = 0)
        => modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);

    /// <summary>
    /// The parameters of one pass of one of the component's materials, on a copy of the material that belongs to this
    /// component alone, so a shared material is not changed for every model that uses it.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="materialIndex"/> or <paramref name="passIndex"/> is out of range.</exception>
    private static ParameterCollection GetMaterialPassParameters(this ModelComponent modelComponent, int materialIndex, int passIndex)
    {
        ArgumentNullException.ThrowIfNull(modelComponent);

        if (!IsValidMaterialIndex(modelComponent, materialIndex))
            throw new ArgumentOutOfRangeException(nameof(materialIndex));

        var material = modelComponent.GetMaterial(materialIndex);

        if (material is not ModelComponentMaterialCopy { ModelComponent: var owner } || owner != modelComponent)
        {
            var copy = new ModelComponentMaterialCopy { ModelComponent = modelComponent };

            MaterialExtensions.CopyProperties(material, copy);

            modelComponent.Materials[materialIndex] = material = copy;
        }

        if (passIndex < 0 || passIndex >= material.Passes.Count)
            throw new ArgumentOutOfRangeException(nameof(passIndex));

        return material.Passes[passIndex].Parameters;
    }

    /// <summary>A model's own material slots first; a component with no model can still carry materials by index.</summary>
    private static bool IsValidMaterialIndex(ModelComponent modelComponent, int materialIndex)
        => materialIndex >= 0 && modelComponent.GetMaterialCount() is var count && (count > 0 ? materialIndex < count : modelComponent.Materials.ContainsKey(materialIndex));

    private sealed class ModelComponentMaterialCopy : Material
    {
        internal required ModelComponent ModelComponent { get; init; }
    }

    private static (List<Vector3> vertices, List<int> indices) GetMeshData(Model model, IGame game)
    {
        // Counted first so both lists are allocated once at the right size; a model with many meshes
        // would otherwise regrow them repeatedly while copying.
        int totalVertices = 0, totalIndices = 0;

        foreach (var meshData in model.Meshes)
        {
            totalVertices += meshData.Draw.VertexBuffers[0].Count;
            totalIndices += meshData.Draw.IndexBuffer.Count;
        }

        var combinedVertices = new List<Vector3>(totalVertices);
        var combinedIndices = new List<int>(totalIndices);

        foreach (var meshData in model.Meshes)
        {
            var vertexBinding = meshData.Draw.VertexBuffers[0];
            var indexBinding = meshData.Draw.IndexBuffer;

            // Reads the buffers back from the GPU. Only the first vertex buffer is considered, which
            // covers the procedural and imported models the toolkit creates.
            var verticesBytes = vertexBinding.Buffer.GetData<byte>(game.GraphicsContext.CommandList);
            var indicesBytes = indexBinding.Buffer.GetData<byte>(game.GraphicsContext.CommandList);

            // Empty lists if there is an issue
            if (verticesBytes is not { Length: > 0 } || indicesBytes is not { Length: > 0 })
                return (combinedVertices, combinedIndices);

            // Meshes are concatenated into one list, so every index of this mesh shifts by the number
            // of vertices already collected.
            var vertMappingStart = combinedVertices.Count;

            // The position is read where the declaration puts it, so an imported layout that carries
            // something else first, or packs its position differently, is read right or refused.
            var stride = vertexBinding.Declaration.VertexStride;
            var positionOffset = PositionOffset(vertexBinding.Declaration);
            var vertices = verticesBytes.AsSpan();

            for (int i = 0, head = vertexBinding.Offset + positionOffset; i < vertexBinding.Count; i++, head += stride)
                combinedVertices.Add(MemoryMarshal.Read<Vector3>(vertices.Slice(head, 12)));

            // Stride narrows index buffers to 16 bits when a mesh has few enough vertices, so both
            // widths have to be handled.
            var indices = indicesBytes.AsSpan(indexBinding.Offset);

            if (indexBinding.Is32Bit)
            {
                foreach (var i in MemoryMarshal.Cast<byte, int>(indices)[..indexBinding.Count])
                    combinedIndices.Add(vertMappingStart + i);
            }
            else
            {
                foreach (var i in MemoryMarshal.Cast<byte, ushort>(indices)[..indexBinding.Count])
                    combinedIndices.Add(vertMappingStart + i);
            }
        }

        return (combinedVertices, combinedIndices);
    }

    /// <summary>The byte offset of the position inside one vertex, which must be three floats.</summary>
    /// <exception cref="InvalidOperationException">If the declaration has no position, or one that is not three floats.</exception>
    private static int PositionOffset(VertexDeclaration declaration)
    {
        foreach (var element in declaration.EnumerateWithOffsets())
        {
            if (element.VertexElement.SemanticName != VertexElementUsage.Position) continue;

            if (element.VertexElement.Format != PixelFormat.R32G32B32_Float)
                throw new InvalidOperationException($"The vertex position is {element.VertexElement.Format}, not three floats.");

            return element.Offset;
        }

        throw new InvalidOperationException("The vertex declaration has no position element.");
    }
}