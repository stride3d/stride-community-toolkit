using Hexa.NET.ImGui;
using Stride.Graphics;

namespace Stride.CommunityToolkit.ImGui;

/// <summary>
/// Owns the textures Dear ImGui manages through its 1.92+ texture protocol (RendererHasTextures):
/// creates, updates and destroys them as the draw data requests, and resolves texture ids at draw
/// time. Split out of <see cref="ImGuiSystem"/>, which forwards the protocol events here.
/// </summary>
/// <param name="device">The device the textures are created on.</param>
/// <param name="commandList">The command list the pixels are uploaded through.</param>
internal sealed class ImGuiTextureManager(GraphicsDevice device, CommandList commandList) : IDisposable
{
    // Managed ids carry a high bit, so they never collide with the small sequential ids of ImGuiExtension's user textures
    private const uint ManagedIdFlag = 0x80000000u;

    private readonly Dictionary<ImTextureID, Texture> _managedTextures = [];

    internal unsafe void ProcessTextureUpdates(ImDrawDataPtr drawData)
    {
        if (drawData.Handle->Textures == null) return;

        var textures = drawData.Textures;

        for (var i = 0; i < textures.Size; i++)
        {
            ImTextureDataPtr textureData = textures.Data[i];

            switch (textureData.Status)
            {
                case ImTextureStatus.WantCreate:
                    CreateManagedTexture(textureData);
                    break;
                case ImTextureStatus.WantUpdates:
                    UpdateManagedTexture(textureData);
                    break;
                case ImTextureStatus.WantDestroy:
                    DestroyManagedTexture(textureData);
                    break;
            }
        }
    }

    private void CreateManagedTexture(ImTextureDataPtr textureData)
    {
        var managedId = (ImTextureID)(nint)(ManagedIdFlag | (uint)textureData.UniqueID);

        _managedTextures[managedId] = Create(textureData);
        textureData.SetTexID(managedId);
        textureData.SetStatus(ImTextureStatus.Ok);
    }

    // A texture of the same size is updated in place; a resized one is made afresh
    private void UpdateManagedTexture(ImTextureDataPtr textureData)
    {
        var texId = textureData.GetTexID();

        if (_managedTextures.TryGetValue(texId, out var existing))
        {
            if (existing.Width != textureData.Width || existing.Height != textureData.Height)
            {
                existing.Dispose();
                _managedTextures[texId] = Create(textureData);
            }
            else
            {
                Upload(existing, textureData);
            }
        }

        textureData.SetStatus(ImTextureStatus.Ok);
    }

    private void DestroyManagedTexture(ImTextureDataPtr textureData)
    {
        if (_managedTextures.Remove(textureData.GetTexID(), out var texture)) texture.Dispose();

        textureData.SetStatus(ImTextureStatus.Ok);
    }

    private Texture Create(ImTextureDataPtr textureData)
    {
        var pixelFormat = textureData.Format == ImTextureFormat.Rgba32 ? PixelFormat.R8G8B8A8_UNorm : PixelFormat.R8_UNorm;
        var texture = Texture.New2D(device, textureData.Width, textureData.Height, pixelFormat, TextureFlags.ShaderResource);

        Upload(texture, textureData);

        return texture;
    }

    private unsafe void Upload(Texture texture, ImTextureDataPtr textureData)
        => texture.SetData(commandList, new ReadOnlySpan<byte>(textureData.Pixels, textureData.GetSizeInBytes()));

    /// <summary>Resolves a managed texture id to its texture, when the id is one of ours.</summary>
    internal bool TryGet(ImTextureID texId, out Texture texture) => _managedTextures.TryGetValue(texId, out texture!);

    /// <summary>The first managed texture, in practice the font atlas; <see langword="null"/> before the atlas exists.</summary>
    internal Texture? FirstTexture()
    {
        foreach (var texture in _managedTextures.Values) return texture;

        return null;
    }

    public void Dispose()
    {
        foreach (var texture in _managedTextures.Values) texture.Dispose();

        _managedTextures.Clear();
    }
}