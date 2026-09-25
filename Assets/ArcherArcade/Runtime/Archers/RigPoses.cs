using UnityEngine;

namespace ArcherArcade.Archers
{
    /// <summary>
    /// The design's six poses (aa-characters.js POSES: Idle, Drawing, Full draw, Release, Hit, Victory), the gameplay
    /// aim pose (exact angle + draw) and pose blending.
    /// </summary>
    public static class RigPoses
    {
        public static RigPose Idle() => new RigPose();

        public static RigPose Drawing() => new RigPose
        {
            Expression = RigExpression.Aim, FrontArm = new Vector2(-76f, -84f), Bow = -6f, Nock = RigNockMode.Half, Torso = 1f,
            FrontLeg = new Vector3(-10f, 5f, 0f), BackLeg = new Vector3(14f, -4f, 0f)
        };

        public static RigPose FullDraw() => new RigPose
        {
            Expression = RigExpression.Aim, FrontArm = new Vector2(-90f, -90f), Bow = 0f, Nock = RigNockMode.Full, Torso = 3f,
            FrontLeg = new Vector3(-14f, 6f, 0f), BackLeg = new Vector3(16f, -4f, 0f)
        };

        public static RigPose Release() => new RigPose
        {
            Expression = RigExpression.Normal, FrontArm = new Vector2(-100f, -96f), Bow = -6f, BackArmMode = RigArmMode.ShoulderOffset,
            BackTarget = new Vector2(-58f, -2f), BackPickDown = true, Torso = -3f,
            FrontLeg = new Vector3(-14f, 6f, 0f), BackLeg = new Vector3(16f, -4f, 0f)
        };

        public static RigPose Hit() => new RigPose
        {
            Expression = RigExpression.Hurt, Torso = -14f, Head = -10f, FrontArm = new Vector2(-150f, -120f), Bow = 36f,
            BackArm = new Vector2(150f, 120f), FrontLeg = new Vector3(-44f, 56f, 0f), BackLeg = new Vector3(6f, -2f, 0f)
        };

        public static RigPose Victory() => new RigPose
        {
            Expression = RigExpression.Happy, FrontArm = new Vector2(-6f, -40f), Bow = 66f, BackArm = new Vector2(-100f, -130f),
            BackArmFront = true, FrontLeg = new Vector3(-10f, 6f, 0f), BackLeg = new Vector3(14f, -4f, 0f)
        };

        /// <summary>
        /// Gameplay aim: bow arm and bow point exactly along <paramref name="angle"/> (degrees, up positive), string
        /// pulled by <paramref name="draw"/> (0..1); the body leans into high shots.
        /// </summary>
        public static void Aim(RigPose p, float angle, float draw)
        {
            float a = Mathf.Clamp(angle, -60f, 85f);
            p.Expression = RigExpression.Aim;
            p.Torso = Mathf.Lerp(1f, 3f, draw) - a * 0.12f;
            p.Head = -Mathf.Clamp(a * 0.45f, -18f, 26f);
            p.FrontArm = new Vector2(-90f - a, -90f - a);
            p.Bow = -a;
            p.Nock = RigNockMode.Aim;
            p.AimAngle = angle;
            p.Draw = draw;
            p.ShowArrow = true;
            p.FrontLeg = new Vector3(Mathf.Lerp(-10f, -14f, draw), 6f, 0f);
            p.BackLeg = new Vector3(Mathf.Lerp(14f, 16f, draw), -4f, 0f);
            p.BackArmFront = false;
        }

        /// <summary>
        /// Blends two poses into <paramref name="into"/>. Angles blend directly; the string hand and the nock blend
        /// as points (solved from both poses) so the arm never detaches from the string.
        /// </summary>
        public static void Blend(RigDims d, bool crossbow, RigPose a, RigPose b, float t, RigFrame scratchA, RigFrame scratchB, RigPose into)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f) { into.CopyFrom(a); return; }
            if (t >= 1f) { into.CopyFrom(b); return; }
            RigSolver.Solve(d, a, crossbow, scratchA);
            RigSolver.Solve(d, b, crossbow, scratchB);
            into.Expression = t < 0.5f ? a.Expression : b.Expression;
            into.Torso = Mathf.Lerp(a.Torso, b.Torso, t);
            into.Head = Mathf.Lerp(a.Head, b.Head, t);
            into.FrontLeg = Vector3.Lerp(a.FrontLeg, b.FrontLeg, t);
            into.BackLeg = Vector3.Lerp(a.BackLeg, b.BackLeg, t);
            into.FrontArm = new Vector2(Mathf.LerpAngle(a.FrontArm.x, b.FrontArm.x, t), Mathf.LerpAngle(a.FrontArm.y, b.FrontArm.y, t));
            into.Bow = Mathf.LerpAngle(a.Bow, b.Bow, t);
            into.Bob = Mathf.Lerp(a.Bob, b.Bob, t);
            into.BackArmMode = RigArmMode.Absolute;
            into.BackTarget = Vector2.Lerp(scratchA.BackWrist, scratchB.BackWrist, t);
            into.BackPickDown = t < 0.5f ? a.BackPickDown : b.BackPickDown;
            into.BackArmFront = t < 0.5f ? scratchA.BackArmFront : scratchB.BackArmFront;
            bool nockA = a.Nock != RigNockMode.Rest, nockB = b.Nock != RigNockMode.Rest;
            if (nockA || nockB)
            {
                into.Nock = RigNockMode.Absolute;
                Vector2 na = nockA ? scratchA.ArrowNock : scratchA.StringMid;
                Vector2 nb = nockB ? scratchB.ArrowNock : scratchB.StringMid;
                into.NockPoint = Vector2.Lerp(na, nb, t);
                into.ShowArrow = (nockA && a.ShowArrow && t < 0.5f) || (nockB && b.ShowArrow && t >= 0.5f) || (nockA && nockB && a.ShowArrow && b.ShowArrow);
            }
            else
            {
                into.Nock = RigNockMode.Rest;
                into.ShowArrow = false;
            }
            into.AimAngle = Mathf.Lerp(a.AimAngle, b.AimAngle, t);
            into.Draw = Mathf.Lerp(a.Draw, b.Draw, t);
        }
    }
}
