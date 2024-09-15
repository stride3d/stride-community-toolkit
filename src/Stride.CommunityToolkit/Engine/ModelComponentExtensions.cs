using Stride.CommunityToolkit.Rendering;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="ModelComponent"/> class, enabling additional functionality
/// such as calculating model dimensions and extracting mesh data.
/// </summary>
public static class ModelComponentExtensions
{
    /// <summary>
    /// Calculates the height, width, and length of the model represented by the ModelComponent.
    /// </summary>
    /// <param name="modelComponent">The ModelComponent whose model dimensions are to be calculated.</param>
    /// <returns>A Vector3 representing the height, width, and length of the model.</returns>
    /// <remarks>
    /// The dimensions are calculated based on the model's bounding box. The returned Vector3 contains the height (Y-axis),
    /// width (X-axis), and length (Z-axis) of the bounding box.
    /// </remarks>
    public static Vector3 GetMeshHWL(this ModelComponent modelComponent)
    {
        var boundingBox = modelComponent.Model.BoundingBox;
        var height = boundingBox.Maximum.Y - boundingBox.Minimum.Y;
        var width = boundingBox.Maximum.X - boundingBox.Minimum.X;
        var length = boundingBox.Maximum.Z - boundingBox.Minimum.Z;

        return new Vector3(height, width, length);
    }

    /// <summary>
    /// Calculates the height of the model represented by the ModelComponent.
    /// </summary>
    /// <param name="modelComponent">The ModelComponent whose model height is to be calculated.</param>
    /// <returns>The height of the model along the Y-axis.</returns>
    /// <remarks>
    /// The height is calculated based on the model's bounding box, which encompasses its maximum extent in 3D space.
    /// </remarks>
    public static float GetMeshHeight(this ModelComponent modelComponent)
    {
        var boundingBox = modelComponent.Model.BoundingBox;

        var height = boundingBox.Maximum.Y - boundingBox.Minimum.Y;

        return height;
    }

    /// <summary>
    /// Retrieves the vertices and indices from the ModelComponent's mesh data.
    /// </summary>
    /// <param name="model">The ModelComponent from which to extract mesh data.</param>
    /// <param name="game">The game instance, used to access graphics context for data extraction.</param>
    /// <returns>A tuple containing two lists: the first list holds the vertices (as <see cref="Vector3"/>), and the second list holds the indices (as <see cref="int"/>).</returns>
    /// <remarks>
    /// This method extracts raw vertex and index data from the meshes in the provided ModelComponent.
    /// It's useful for operations that require direct access to mesh data, such as custom rendering, collision detection, or physics simulations.
    /// Note that this method extracts combined vertex and index data from all meshes in the ModelComponent.
    /// </remarks>
    public static (List<Vector3> vertices, List<int> indices) GetMeshVerticesAndIndices(this ModelComponent model, IGame game)
    {
        return GetMeshData(model.Model, game);
    }

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
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ObjectParameterAccessor<T> parameterAccessor, T value, int materialIndex = 0, int passIndex = 0)
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameterAccessor, value);
    }

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
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameter<T> parameter, T value, int materialIndex = 0, int passIndex = 0) where T : struct
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
    }

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
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameter<T> parameter, int count, ref T firstValue, int materialIndex = 0, int passIndex = 0) where T : struct
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, count, ref firstValue);
    }

    /// <summary>
    /// Sets blittable value of the material pass parameter. Cloning the <see cref="Material"/> if required.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
    /// <param name="parameter">The parameter to update.</param>
    /// <param name="value">The value.</param>
    /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
    /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameter<T> parameter, ref T value, int materialIndex = 0, int passIndex = 0) where T : struct
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, ref value);
    }

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
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, int count, ref T firstValue, int materialIndex = 0, int passIndex = 0) where T : struct
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, count, ref firstValue);
    }

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
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, T[] values, int materialIndex = 0, int passIndex = 0) where T : struct
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, values);
    }

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
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, ref T value, int materialIndex = 0, int passIndex = 0) where T : struct
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, ref value);
    }

    /// <summary>
    /// Sets a blittable of the material pass parameter. Cloning the <see cref="Material"/> if required.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
    /// <param name="parameter">The parameter to update.</param>
    /// <param name="value">The value.</param>
    /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
    /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, T value, int materialIndex = 0, int passIndex = 0) where T : struct
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
    }

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
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, PermutationParameter<T> parameter, T value, int materialIndex = 0, int passIndex = 0)
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
    }

    /// <summary>
    /// Sets an object of the material pass parameter. Cloning the <see cref="Material"/> if required.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
    /// <param name="parameter">The parameter to update.</param>
    /// <param name="value">The value.</param>
    /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
    /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ObjectParameterKey<T> parameter, T value, int materialIndex = 0, int passIndex = 0)
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
    }

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
    /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
    /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
    /// </exception>
    public static void SetMaterialParameter<T>(this ModelComponent modelComponent, PermutationParameterKey<T> parameter, T value, int materialIndex = 0, int passIndex = 0)
    {
        modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
    }

    /// <summary>
    /// Clones a <see cref="ModelComponent"/>s <see cref="Material"/> if required;
    /// </summary>
    /// <param name="modelComponent"></param>
    /// <param name="materialIndex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.</exception>
    private static Material GetMaterialCopy(this ModelComponent modelComponent, int materialIndex)
    {
        ArgumentNullException.ThrowIfNull(modelComponent);

        if (!IsValidMaterialIndex(modelComponent, materialIndex))
        {
            throw new ArgumentOutOfRangeException(nameof(materialIndex));
        }

        var material = modelComponent.GetMaterial(materialIndex);

        if (material is ModelComponentMaterialCopy copy && copy.ModelComponent == modelComponent)
        {
            return material;
        }

        var materialCopy = new ModelComponentMaterialCopy()
        {
            ModelComponent = modelComponent,
        };

        MaterialExtensions.CopyProperties(material, materialCopy);

        modelComponent.Materials[materialIndex] = materialCopy;

        return materialCopy;
    }

    private static bool IsValidMaterialIndex(ModelComponent modelComponent, int materialIndex)
    {
        if (materialIndex < 0) return false;

        int materialCount = modelComponent.GetMaterialCount();

        if (materialCount > 0)
        {
            return materialIndex < materialCount;
        }
        else
        {
            return modelComponent.Materials.ContainsKey(materialIndex);
        }
    }

    private static ParameterCollection GetMaterialPassParameters(this ModelComponent modelComponent, int materialIndex, int passIndex)
    {
        var material = modelComponent.GetMaterialCopy(materialIndex);

        if (passIndex < 0 || passIndex >= material.Passes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(passIndex));
        }

        return material.Passes[passIndex].Parameters;
    }

    private class ModelComponentMaterialCopy : Material
    {
        public ModelComponent ModelComponent { get; set; }
    }

    private static unsafe (List<Vector3> vertices, List<int> indices) GetMeshData(Model model, IGame game)
    {

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
            var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
            var iBuffer = meshData.Draw.IndexBuffer.Buffer;
            byte[] verticesBytes = vBuffer.GetData<byte>(game.GraphicsContext.CommandList);
            byte[] indicesBytes = iBuffer.GetData<byte>(game.GraphicsContext.CommandList);

            if ((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
            {
                // returns empty lists if there is an issue
                return (combinedVertices, combinedIndices);
            }

            int vertMappingStart = combinedVertices.Count;

            fixed (byte* bytePtr = verticesBytes)
            {
                var vBindings = meshData.Draw.VertexBuffers[0];
                int count = vBindings.Count;
                int stride = vBindings.Declaration.VertexStride;
                for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                {
                    var pos = *(Vector3*)(bytePtr + vHead);

                    combinedVertices.Add(pos);
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

        return (combinedVertices, combinedIndices);
    }
}