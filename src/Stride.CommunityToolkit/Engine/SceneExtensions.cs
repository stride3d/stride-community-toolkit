using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;
public static class SceneExtensions
{

	/// <summary>
	/// Gets the first camera in the scene.
	/// </summary>
	/// <param name="scene"></param>
	/// <returns></returns>
	public static CameraComponent? GetCamera(this Scene scene)
	{
		var entities =  scene.Entities;
        CameraComponent? camera = null;
        foreach (var entity in entities)
        {
            camera = entity.GetComponentInChildren<CameraComponent>();
            if (camera != null)
            {
				break;
			}
        }
        return camera;
	}

	/// <summary>
	/// Gets the first camera in the scene with the specified <see cref="Entity"/> name.
	/// </summary>
	/// <param name="scene"></param>
	/// <param name="name"></param>
	/// <returns></returns>
	public static CameraComponent? GetCamera(this Scene scene, string name)
	{
		var entities = scene.Entities;
		CameraComponent? camera = null;
		foreach (var entity in entities)
		{
			camera = entity.GetComponentInChildren<CameraComponent>();
			if (camera != null && camera.Entity.Name == name)
			{
				break;
			}
		}
		return camera;
	}
}
