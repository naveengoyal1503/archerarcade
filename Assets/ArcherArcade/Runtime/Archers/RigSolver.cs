using UnityEngine;

namespace ArcherArcade.Archers
{
    /// <summary>
    /// Puts a character together for a pose — a straight port of the design's assembly code (aa-characters.js
    /// build(): shoulder / hip anchors, two-bone IK for the string arm, bow, string, nocked arrow, lift to the ground).
    /// Pure math on design units (y down, degrees clockwise), so it runs outside Unity too (Tools/RigPreview).
    /// </summary>
    public static class RigSolver
    {
        /// <summary>Crossbow geometry in bow space (Tools/art/characters_ext.js crossbowP).</summary>
        public static readonly Vector2 CrossbowNock = new Vector2(-46f, 0f);
        public static readonly Vector2 CrossbowStock = new Vector2(-66f, 8f);

        public static Vector2 Rot(Vector2 p, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        /// <summary>Angle that turns the "hanging down" vector (0, 1) towards v.</summary>
        public static float DirA(Vector2 v) => Mathf.Atan2(-v.x, v.y) * Mathf.Rad2Deg;

        struct Arm
        {
            public Vector2 S, E, W, C;
            public float A1, A2;
        }

        static Arm ArmAngles(Vector2 s, float a1, float a2, RigDims d)
        {
            var arm = new Arm { S = s, A1 = a1, A2 = a2 };
            arm.E = s + Rot(new Vector2(0f, d.UA), a1);
            arm.W = arm.E + Rot(new Vector2(0f, d.FA), a2);
            arm.C = arm.W + Rot(new Vector2(0f, d.HR * 0.5f), a2);
            return arm;
        }

        /// <summary>Two-bone IK (design ik()): wrist towards t, elbow chosen "down" or "back".</summary>
        static Arm ArmIk(Vector2 s, Vector2 t, RigDims d, bool pickDown)
        {
            float l1 = d.UA, l2 = d.FA;
            Vector2 v = t - s;
            float dist = v.magnitude;
            dist = Mathf.Max(Mathf.Abs(l1 - l2) + 1f, Mathf.Min(dist, l1 + l2 - 0.5f));
            float baseA = DirA(v);
            float cos = (l1 * l1 + dist * dist - l2 * l2) / (2f * l1 * dist);
            float al = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
            float c1 = baseA + al, c2 = baseA - al;
            Vector2 e1 = s + Rot(new Vector2(0f, l1), c1);
            Vector2 e2 = s + Rot(new Vector2(0f, l1), c2);
            bool first = pickDown ? e1.y >= e2.y : e1.x <= e2.x;
            float a1 = first ? c1 : c2;
            Vector2 e = first ? e1 : e2;
            return ArmAngles(s, a1, DirA(t - e), d);
        }

        public static void Solve(RigDims d, RigPose p, bool crossbow, RigFrame f)
        {
            float tr = p.Torso;
            Vector2 up = new Vector2(0f, -p.Bob);
            Vector2 neck = Rot(new Vector2(2f, -d.TH + 2f), tr) + up;
            Vector2 shF = Rot(new Vector2(d.TW * 0.14f, -d.TH + 16f), tr) + up;
            Vector2 shB = Rot(new Vector2(-d.TW * 0.08f, -d.TH + 14f), tr) + up;
            Vector2 hpF = Rot(new Vector2(d.TW * 0.16f, -8f), tr);
            Vector2 hpB = Rot(new Vector2(-d.TW * 0.14f, -8f), tr);
            float headA = tr + p.Head;

            // Legs (design leg()): hip, knee, ankle.
            Vector2 knF = hpF + Rot(new Vector2(0f, d.UL), p.FrontLeg.x);
            Vector2 anF = knF + Rot(new Vector2(0f, d.LL), p.FrontLeg.x + p.FrontLeg.y);
            Vector2 knB = hpB + Rot(new Vector2(0f, d.UL), p.BackLeg.x);
            Vector2 anB = knB + Rot(new Vector2(0f, d.LL), p.BackLeg.x + p.BackLeg.y);
            f.Lift = Mathf.Max(anF.y, anB.y) + 20f * d.K;

            Vector2 cheek = neck + Rot(new Vector2(34f, -2f), headA);

            // Bow arm and bow.
            Arm af = ArmAngles(shF, p.FrontArm.x, p.FrontArm.y, d);
            float br = p.Bow;
            Vector2 grip = af.C;
            Vector2 tipT = grip + Rot(new Vector2(-d.BD, -d.BH), br);
            Vector2 tipB = grip + Rot(new Vector2(-d.BD, d.BH), br);
            Vector2 rest = grip + Rot(new Vector2(-d.BD, 0f), br);

            bool hasNock = false;
            Vector2 nock = rest;
            switch (p.Nock)
            {
                case RigNockMode.Half:
                    nock = (rest + cheek) * 0.5f;
                    hasNock = true;
                    break;
                case RigNockMode.Full:
                    nock = cheek;
                    hasNock = true;
                    break;
                case RigNockMode.Aim:
                {
                    float a = -p.AimAngle * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    float full = Mathf.Max(d.BD, Vector2.Distance(grip, cheek) * 0.98f);
                    nock = grip - dir * Mathf.Lerp(d.BD, full, Mathf.Clamp01(p.Draw));
                    hasNock = true;
                    break;
                }
                case RigNockMode.Absolute:
                    nock = p.NockPoint;
                    hasNock = true;
                    break;
            }
            if (crossbow)
            {
                // The crossbow keeps its string latched: the arrow lies on the stock, the back hand holds the stock.
                nock = grip + Rot(CrossbowNock, br);
                hasNock = p.Nock != RigNockMode.Rest;
            }

            Arm ab;
            if (crossbow && p.Nock != RigNockMode.Rest) ab = ArmIk(shB, grip + Rot(CrossbowStock, br), d, false);
            else if (hasNock && !crossbow) ab = ArmIk(shB, nock, d, false);
            else if (p.BackArmMode == RigArmMode.ShoulderOffset) ab = ArmIk(shB, shB + p.BackTarget, d, p.BackPickDown);
            else if (p.BackArmMode == RigArmMode.Absolute) ab = ArmIk(shB, p.BackTarget, d, p.BackPickDown);
            else ab = ArmAngles(shB, p.BackArm.x, p.BackArm.y, d);

            f.BackArmFront = hasNock || p.BackArmFront;
            f.Expression = p.Expression;

            f.Set(RigPart.UpperArmBack, ab.S, ab.A1);
            f.Set(RigPart.ForearmBack, ab.E, ab.A2);
            f.Set(RigPart.HandBack, ab.W, ab.A2);
            f.Set(RigPart.UpperLegBack, hpB, p.BackLeg.x);
            f.Set(RigPart.LowerLegBack, knB, p.BackLeg.x + p.BackLeg.y);
            f.Set(RigPart.FootBack, anB, p.BackLeg.z);
            f.Set(RigPart.Cape, neck, tr);
            f.Set(RigPart.Quiver, Rot(new Vector2(-d.TW * 0.4f, -d.TH + 6f), tr) + up, tr - 14f);
            f.Set(RigPart.Torso, up, tr);
            f.Set(RigPart.UpperLegFront, hpF, p.FrontLeg.x);
            f.Set(RigPart.LowerLegFront, knF, p.FrontLeg.x + p.FrontLeg.y);
            f.Set(RigPart.FootFront, anF, p.FrontLeg.z);
            f.Set(RigPart.Head, neck, headA);
            f.Set(RigPart.Gear, neck, headA);
            f.Set(RigPart.Bow, grip, br);
            f.Set(RigPart.UpperArmFront, af.S, af.A1);
            f.Set(RigPart.ForearmFront, af.E, af.A2);
            f.Set(RigPart.HandFront, af.W, af.A2);

            f.StringTop = tipT;
            f.StringBottom = tipB;
            f.StringMid = crossbow ? rest : nock;
            f.Pulled = hasNock && !crossbow;
            f.ArrowNock = nock;
            Vector2 toGrip = grip - nock;
            f.ArrowAngle = crossbow ? br : Mathf.Atan2(toGrip.y, toGrip.x) * Mathf.Rad2Deg;
            f.ArrowVisible = hasNock && p.ShowArrow;

            f.HeadCenter = neck + Rot(new Vector2(6f, -58f), headA);
            f.Chest = Rot(new Vector2(d.TW * 0.2f, -d.TH * 0.55f), tr) + up;
            f.Grip = grip;
            f.BackWrist = ab.W;
        }
    }
}
