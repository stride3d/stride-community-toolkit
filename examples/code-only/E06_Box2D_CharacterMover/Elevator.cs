using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace E06_Box2D_CharacterMover;

/// <summary>
/// The kinematic elevator: a cosine ride set as a target transform before each fixed step, so it
/// moves by velocity rather than teleporting.
/// </summary>
public sealed class Elevator(B2BodyId body, Vector2 origin, float amplitude) : IBox2DSimulationUpdate
{
    private float _time;

    public void SimulationUpdate(Box2DSimulation simulation, float deltaTime)
    {
        _time += deltaTime;

        var y = amplitude * MathF.Cos(_time + MathUtil.Pi) + origin.Y;
        var target = new B2Transform(new B2Vec2(origin.X, y), b2Rot_identity);

        b2Body_SetTargetTransform(body, in target, deltaTime, true);
    }
}