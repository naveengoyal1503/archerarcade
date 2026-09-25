using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Modes;

// Prints [{name, time, grounds:[[cx,cy,hx,hy]], walls:[...], props:[{kind, shape, motion, ...}], fighters:[...]}]
static class Program
{
    static readonly StringBuilder Sb = new StringBuilder();
    static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    static void Main(string[] args)
    {
        var scenes = new List<(string, MatchState, TimeOfDay)>();
        int[] levels = args.Length > 0 ? Array.ConvertAll(args, int.Parse) : new[] { 1, 3, 5, 7, 8, 10, 12, 13, 14, 16, 20 };
        foreach (int n in levels)
        {
            LevelDef l = WorldOne.Level(n);
            scenes.Add(("level" + n, new MatchState(LevelBuilder.Build(l, LevelBuilder.DefaultPlayer(), 1)), l.Time));
        }
        foreach (string arena in new[] { "crates", "platforms", "barrels" })
        {
            QuickDuel.Setup q = QuickDuel.Build(LevelBuilder.DefaultPlayer(), "medium", arena, 3);
            scenes.Add(("quick_" + arena, new MatchState(q.Match), TimeOfDay.Day));
        }
        var tr = new TrainingRange(LevelBuilder.DefaultPlayer(), 30, 2);
        scenes.Add(("training30", tr.Match, TimeOfDay.Day));

        Sb.Append('[');
        for (int i = 0; i < scenes.Count; i++)
        {
            if (i > 0) Sb.Append(',');
            Write(scenes[i].Item1, scenes[i].Item2, scenes[i].Item3);
        }
        Sb.Append(']');
        Console.WriteLine(Sb.ToString());
    }

    static void Box(Shape s) => Sb.Append('[').Append(F(s.A.X)).Append(',').Append(F(s.A.Y)).Append(',').Append(F(s.HalfSize.X)).Append(',').Append(F(s.HalfSize.Y)).Append(']');

    static void ShapeJson(Shape s)
    {
        Sb.Append("{\"kind\":\"").Append(s.Kind).Append("\",\"a\":[").Append(F(s.A.X)).Append(',').Append(F(s.A.Y))
          .Append("],\"b\":[").Append(F(s.B.X)).Append(',').Append(F(s.B.Y)).Append("],\"r\":").Append(F(s.Radius))
          .Append(",\"h\":[").Append(F(s.HalfSize.X)).Append(',').Append(F(s.HalfSize.Y)).Append("]}");
    }

    static void Write(string name, MatchState m, TimeOfDay time)
    {
        ArenaLayout a = m.Setup.Arena;
        Sb.Append("{\"name\":\"").Append(name).Append("\",\"time\":\"").Append(time).Append("\",\"grounds\":[");
        for (int i = 0; i < a.Grounds.Count; i++) { if (i > 0) Sb.Append(','); Box(a.Grounds[i]); }
        Sb.Append("],\"walls\":[");
        for (int i = 0; i < a.Walls.Count; i++) { if (i > 0) Sb.Append(','); Box(a.Walls[i]); }
        Sb.Append("],\"props\":[");
        for (int i = 0; i < m.PropCount; i++)
        {
            if (i > 0) Sb.Append(',');
            Prop p = m.GetProp(i);
            Sb.Append("{\"kind\":\"").Append(p.Kind).Append("\",\"alive\":").Append(m.PropPresent(i) ? "true" : "false")
              .Append(",\"durable\":").Append(p.Spec.Durable ? "true" : "false").Append(",\"tower\":").Append(p.Spec.Tower)
              .Append(",\"slot\":").Append(p.Spec.RotatingSlot).Append(",\"motion\":\"").Append(p.Spec.Motion.Kind)
              .Append("\",\"pivot\":[").Append(F(p.Spec.Motion.Pivot.X)).Append(',').Append(F(p.Spec.Motion.Pivot.Y)).Append("],\"shape\":");
            ShapeJson(m.PropRestShape(i));
            if (p.Spec.ShieldOwner >= 0) Sb.Append(",\"facing\":").Append(m.GetFighter(p.Spec.ShieldOwner).Facing);
            Sb.Append('}');
        }
        Sb.Append("],\"fighters\":[");
        for (int i = 0; i < m.FighterCount; i++)
        {
            if (i > 0) Sb.Append(',');
            Fighter f = m.GetFighter(i);
            Sb.Append("{\"id\":\"").Append(f.Def.Id).Append("\",\"side\":").Append(f.Side).Append(",\"feet\":[").Append(F(f.Feet.X)).Append(',')
              .Append(F(f.Feet.Y)).Append("],\"facing\":").Append(f.Facing).Append(",\"scale\":").Append(F(f.Def.BodyScale))
              .Append(",\"active\":").Append(m.ActiveFighter(f.Side) == i ? "true" : "false").Append('}');
        }
        Sb.Append("]}");
    }
}
