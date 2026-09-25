using UnityEngine;

namespace ArcherArcade.Archers
{
    /// <summary>
    /// Pose parameters, same meaning as the design's pose objects (aa-characters.js POSES): angles in degrees in the
    /// design's y-down space, where 0 = limb hanging down and −90 = pointing forward. Defaults = the design's idle.
    /// </summary>
    public sealed class RigPose
    {
        public RigExpression Expression = RigExpression.Normal;
        /// <summary>Torso tilt and extra head tilt.</summary>
        public float Torso, Head;
        /// <summary>Legs: hip angle, knee bend, foot angle.</summary>
        public Vector3 FrontLeg = new Vector3(-6f, 4f, 0f);
        public Vector3 BackLeg = new Vector3(10f, -4f, 0f);
        /// <summary>Bow arm: upper arm and forearm angles.</summary>
        public Vector2 FrontArm = new Vector2(-4f, -30f);
        /// <summary>Bow rotation around the grip.</summary>
        public float Bow = 60f;

        public RigArmMode BackArmMode = RigArmMode.Angles;
        public Vector2 BackArm = new Vector2(14f, 4f);
        /// <summary>ShoulderOffset / Absolute target (design units, y down).</summary>
        public Vector2 BackTarget;
        /// <summary>IK elbow choice: true = the lower elbow ("down"), false = the elbow further back.</summary>
        public bool BackPickDown;
        /// <summary>Back arm drawn in front of the torso (victory wave).</summary>
        public bool BackArmFront;

        public RigNockMode Nock = RigNockMode.Rest;
        /// <summary>Aim angle in degrees, up positive (gameplay aim).</summary>
        public float AimAngle;
        /// <summary>Draw amount 0..1 (gameplay aim): string from rest to the cheek.</summary>
        public float Draw;
        /// <summary>Absolute nock point (Absolute mode).</summary>
        public Vector2 NockPoint;
        public bool ShowArrow = true;

        /// <summary>Upper body lift in design units (breathing / bounce); legs stay planted.</summary>
        public float Bob;

        public RigPose Clone() => (RigPose)MemberwiseClone();

        public void CopyFrom(RigPose p)
        {
            Expression = p.Expression; Torso = p.Torso; Head = p.Head; FrontLeg = p.FrontLeg; BackLeg = p.BackLeg;
            FrontArm = p.FrontArm; Bow = p.Bow; BackArmMode = p.BackArmMode; BackArm = p.BackArm; BackTarget = p.BackTarget;
            BackPickDown = p.BackPickDown; BackArmFront = p.BackArmFront; Nock = p.Nock; AimAngle = p.AimAngle; Draw = p.Draw;
            NockPoint = p.NockPoint; ShowArrow = p.ShowArrow; Bob = p.Bob;
        }
    }
}
