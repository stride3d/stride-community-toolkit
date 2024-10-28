using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Reflection;

namespace Stride.CommunityToolkit.Bepu;

// ToDo Remove this class once it is implemented in Stride
[ComponentCategory("Bepu")]
public class Body2DComponent : BodyComponent
{
    Vector3 _rotationLock = new Vector3(0, 0, 0);

    [DataMemberIgnore]
    internal Vector3 RotationLock
    {
        get
        {
            return _rotationLock;
        }
        set
        {
            _rotationLock = value;
            AccessBodyReference(value);
            //if (BodyReference is { } bRef)
            //{
            //    bRef.LocalInertia.InverseInertiaTensor.XX *= value.X;
            //    bRef.LocalInertia.InverseInertiaTensor.YX *= value.X * value.Y;
            //    bRef.LocalInertia.InverseInertiaTensor.ZX *= value.Z * value.X;
            //    bRef.LocalInertia.InverseInertiaTensor.YY *= value.Y;
            //    bRef.LocalInertia.InverseInertiaTensor.ZY *= value.Z * value.Y;
            //    bRef.LocalInertia.InverseInertiaTensor.ZZ *= value.Z;
            //}
        }
    }

    protected override void AttachInner(RigidPose containerPose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        base.AttachInner(containerPose, shapeInertia, shapeIndex);
#warning what about a body that become kinematic after some time ?
        if (!Kinematic)
            RotationLock = new Vector3(0, 0, 1);
    }

    public void AccessBodyReference(Vector3 value)
    {
        // Get the type of the BodyComponent to access its members
        var bodyComponentType = typeof(BodyComponent);

        var bodyReferenceProperty = bodyComponentType.GetProperty("BodyReference", BindingFlags.NonPublic | BindingFlags.Instance);

        if (bodyReferenceProperty != null)
        {
            // Get the value of 'BodyReference' property for 'this' instance
            var bodyReferenceValue = bodyReferenceProperty.GetValue(this);

            if (bodyReferenceValue is BodyReference bRef)
            {
                bRef.LocalInertia.InverseInertiaTensor.XX *= value.X;
                bRef.LocalInertia.InverseInertiaTensor.YX *= value.X * value.Y;
                bRef.LocalInertia.InverseInertiaTensor.ZX *= value.Z * value.X;
                bRef.LocalInertia.InverseInertiaTensor.YY *= value.Y;
                bRef.LocalInertia.InverseInertiaTensor.ZY *= value.Z * value.Y;
                bRef.LocalInertia.InverseInertiaTensor.ZZ *= value.Z;
            }
        }
    }
}
