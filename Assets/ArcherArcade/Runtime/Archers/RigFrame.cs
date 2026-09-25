using UnityEngine;

namespace ArcherArcade.Archers
{
    /// <summary>
    /// A solved pose: every part's pivot position and rotation (design units, y down, degrees clockwise), the bow
    /// string, the nocked arrow and the lift that puts the feet on the ground. Reused every frame (no allocations).
    /// </summary>
    public sealed class RigFrame
    {
        public const int PartCount = 18;
        public readonly Vector2[] Position = new Vector2[PartCount];
        public readonly float[] Rotation = new float[PartCount];
        public float Lift;
        public bool BackArmFront;
        public RigExpression Expression;

        /// <summary>Bow string: top tip, middle (nock or rest point), bottom tip; <see cref="Pulled"/> when drawn.</summary>
        public Vector2 StringTop, StringMid, StringBottom;
        public bool Pulled;
        /// <summary>Nocked arrow: nock point and direction (degrees, y-down space); hidden when !ArrowVisible.</summary>
        public Vector2 ArrowNock;
        public float ArrowAngle;
        public bool ArrowVisible;

        /// <summary>Useful anchors for effects: head centre, chest, grip, back wrist.</summary>
        public Vector2 HeadCenter, Chest, Grip, BackWrist;

        public void Set(RigPart part, Vector2 position, float rotation)
        {
            Position[(int)part] = position;
            Rotation[(int)part] = rotation;
        }
    }
}
