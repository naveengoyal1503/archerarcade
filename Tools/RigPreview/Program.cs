using System;
using System.Globalization;
using System.Text;
using ArcherArcade.Archers;

// Prints {pose: {parts: {name: [x, y, rot]}, lift, backFront, expr, string: [...], arrow: [x, y, angle, visible]}}
// for the design's poses and a few gameplay aims, using the dims passed as arguments (from a look's JSON).
static class Program
{
    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    static void Main(string[] args)
    {
        var d = new RigDims();
        bool crossbow = args.Length > 0 && args[0] == "crossbow";
        for (int i = 1; i + 1 < args.Length; i += 2)
        {
            float v = float.Parse(args[i + 1], CultureInfo.InvariantCulture);
            typeof(RigDims).GetField(args[i]).SetValue(d, v);
        }
        var poses = new (string, RigPose)[]
        {
            ("idle", RigPoses.Idle()), ("drawing", RigPoses.Drawing()), ("full_draw", RigPoses.FullDraw()),
            ("release", RigPoses.Release()), ("hit", RigPoses.Hit()), ("victory", RigPoses.Victory()),
            ("aim_0_full", Aim(0, 1)), ("aim_35_half", Aim(35, 0.5f)), ("aim_60_full", Aim(60, 1)), ("aim_m15_full", Aim(-15, 1)),
            ("blend_idle_aim", Blend(d, crossbow, RigPoses.Idle(), Aim(30, 0.6f), 0.5f))
        };
        var sb = new StringBuilder("{");
        var f = new RigFrame();
        for (int p = 0; p < poses.Length; p++)
        {
            RigSolver.Solve(d, poses[p].Item2, crossbow, f);
            sb.Append(p > 0 ? "," : "").Append('"').Append(poses[p].Item1).Append("\":{\"parts\":{");
            for (int i = 0; i < RigFrame.PartCount; i++)
            {
                sb.Append(i > 0 ? "," : "").Append('"').Append(((RigPart)i).ToString()).Append("\":[")
                  .Append(F(f.Position[i].x)).Append(',').Append(F(f.Position[i].y)).Append(',').Append(F(f.Rotation[i])).Append(']');
            }
            sb.Append("},\"lift\":").Append(F(f.Lift)).Append(",\"backFront\":").Append(f.BackArmFront ? "true" : "false")
              .Append(",\"expr\":\"").Append(f.Expression.ToString().ToLowerInvariant()).Append('"')
              .Append(",\"string\":[").Append(F(f.StringTop.x)).Append(',').Append(F(f.StringTop.y)).Append(',')
              .Append(F(f.StringMid.x)).Append(',').Append(F(f.StringMid.y)).Append(',').Append(F(f.StringBottom.x)).Append(',').Append(F(f.StringBottom.y)).Append(']')
              .Append(",\"arrow\":[").Append(F(f.ArrowNock.x)).Append(',').Append(F(f.ArrowNock.y)).Append(',').Append(F(f.ArrowAngle)).Append(',').Append(f.ArrowVisible ? "1" : "0").Append("]}");
        }
        Console.WriteLine(sb.Append('}').ToString());
    }

    static RigPose Aim(float a, float draw)
    {
        var p = new RigPose();
        RigPoses.Aim(p, a, draw);
        return p;
    }

    static RigPose Blend(RigDims d, bool crossbow, RigPose a, RigPose b, float t)
    {
        var into = new RigPose();
        RigPoses.Blend(d, crossbow, a, b, t, new RigFrame(), new RigFrame(), into);
        return into;
    }
}
