using System.Collections.Generic;
using System.Globalization;
using ArcherArcade.Archers;
using ArcherArcade.Logic.Save;
using UnityEngine;

namespace ArcherArcade.Core
{
    /// <summary>
    /// Sprites from the atlases made by Tools/art (Resources/Art): character rigs, portraits, whole-body poses,
    /// and the arrows / props / scenery / backdrop / fx / ui groups. JSON gives each sprite's rect, pivot, density
    /// and 9-slice border; sprites are created on first use and cached.
    /// </summary>
    public static class ArtLibrary
    {
        public const string Arrows = "arrows", Props = "props", Scenery = "scenery", Backdrop = "backdrop", Fx = "fx", Ui = "ui";

        sealed class Atlas
        {
            public Texture2D Texture;
            public Dictionary<string, object> Sprites;
            public readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        }

        static readonly Dictionary<string, Atlas> Atlases = new Dictionary<string, Atlas>();
        static readonly Dictionary<string, RigAsset> Rigs = new Dictionary<string, RigAsset>();

        static Atlas Load(string path)
        {
            if (Atlases.TryGetValue(path, out Atlas a)) return a;
            a = new Atlas { Texture = Resources.Load<Texture2D>(path) };
            var json = Resources.Load<TextAsset>(path);
            a.Sprites = json ? Json.Read(json.text) is Dictionary<string, object> root && root.TryGetValue("sprites", out object s)
                ? s as Dictionary<string, object> : null : null;
            if (!a.Texture || a.Sprites == null) Debug.LogWarning("Missing art atlas " + path);
            Atlases[path] = a;
            return a;
        }

        static float F(Dictionary<string, object> d, string k) => d.TryGetValue(k, out object v) && v is double x ? (float)x : 0f;

        static Sprite Make(Texture2D tex, Dictionary<string, object> e, float ppm)
        {
            float x = F(e, "x"), y = F(e, "y"), w = F(e, "w"), h = F(e, "h");
            var rect = new Rect(x, tex.height - y - h, w, h);
            var pivot = new Vector2(F(e, "px") / w, 1f - F(e, "py") / h);
            Vector4 border = Vector4.zero;
            if (e.TryGetValue("border", out object b) && b is List<object> bl && bl.Count == 4)
                border = new Vector4(ToF(bl[0]), ToF(bl[1]), ToF(bl[2]), ToF(bl[3]));
            return Sprite.Create(tex, rect, pivot, ppm, 0, SpriteMeshType.FullRect, border);
        }

        static float ToF(object o) => o is double d ? (float)d : 0f;

        static Sprite From(string path, string id, float ppmOverride)
        {
            Atlas a = Load(path);
            if (a.Cache.TryGetValue(id, out Sprite s)) return s;
            s = null;
            if (a.Texture && a.Sprites != null && a.Sprites.TryGetValue(id, out object o) && o is Dictionary<string, object> e)
            {
                float ppm = ppmOverride > 0 ? ppmOverride : F(e, "ppm");
                s = Make(a.Texture, e, ppm > 0 ? ppm : 100f);
                s.name = id;
            }
            a.Cache[id] = s;
            return s;
        }

        /// <summary>A sprite from Art/Sprites/&lt;group&gt; (world size from its px-per-metre).</summary>
        public static Sprite Get(string group, string id) => From("Art/Sprites/" + group, id, 0f);

        /// <summary>Head + gear portrait (normal / hurt / happy / aim), for UI.</summary>
        public static Sprite Portrait(string look, string expression = "normal") => From("Art/Portraits", look + "_" + expression, 100f);

        /// <summary>Whole-body picture (idle / victory) for UI lists and result screens.</summary>
        public static Sprite Pose(string look, string pose = "idle") => From("Art/Poses", look + "_" + pose, 100f);

        public static RigAsset Rig(string look)
        {
            if (Rigs.TryGetValue(look, out RigAsset r)) return r;
            r = LoadRig(look) ?? (look != "bandit" ? Rig("bandit") : null);
            Rigs[look] = r;
            return r;
        }

        static RigAsset LoadRig(string look)
        {
            var tex = Resources.Load<Texture2D>("Art/Characters/" + look);
            var json = Resources.Load<TextAsset>("Art/Characters/" + look);
            if (!tex || !json || !(Json.Read(json.text) is Dictionary<string, object> d)) return null;
            var r = new RigAsset
            {
                Id = look,
                Name = d.TryGetValue("name", out object n) ? n as string : look,
                Hero = F(d, "hero") > 0.5f,
                Scale = Mathf.Max(0.1f, F(d, "scale")),
                HasCape = d.TryGetValue("hasCape", out object c) && c is bool cb && cb,
                Boss = d.TryGetValue("boss", out object bo) && bo is bool bb && bb,
                WeakSpot = d.TryGetValue("weakSpot", out object ws) && ws is bool wb && wb,
                BowStyle = d.TryGetValue("bowStyle", out object bs) ? bs as string : "long",
                StringGlow = d.TryGetValue("stringGlow", out object sg) && sg is bool sgb && sgb,
                UnitsPerMeter = F(d, "unitsPerMeter"),
                PxPerMeter = F(d, "pxPerMeter")
            };
            if (d.TryGetValue("stringColor", out object sc) && sc is string hex) r.StringColor = ParseHex(hex);
            if (d.TryGetValue("swatches", out object sw) && sw is List<object> swl)
            {
                r.Swatches = new Color[swl.Count];
                for (int i = 0; i < swl.Count; i++) r.Swatches[i] = ParseHex(swl[i] as string);
            }
            if (d.TryGetValue("dims", out object dm) && dm is Dictionary<string, object> dims)
            {
                RigDims D = r.Dims;
                D.TW = F(dims, "TW"); D.TH = F(dims, "TH"); D.UA = F(dims, "UA"); D.FA = F(dims, "FA");
                D.UL = F(dims, "UL"); D.LL = F(dims, "LL"); D.WUA = F(dims, "wUA"); D.WFA = F(dims, "wFA");
                D.WUL = F(dims, "wUL"); D.WLL = F(dims, "wLL"); D.HR = F(dims, "hr"); D.BH = F(dims, "bH");
                D.BD = F(dims, "bD"); D.K = F(dims, "k");
            }
            if (d.TryGetValue("parts", out object p) && p is Dictionary<string, object> parts)
            {
                foreach (KeyValuePair<string, object> kv in parts)
                {
                    if (!(kv.Value is Dictionary<string, object> e)) continue;
                    Sprite s = Make(tex, e, r.PxPerMeter);
                    s.name = look + "_" + kv.Key;
                    r.Parts[kv.Key] = s;
                }
            }
            return r;
        }

        public static Color ParseHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            string h = hex.TrimStart('#');
            return uint.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v)
                ? new Color(((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f)
                : Color.white;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Atlases.Clear();
            Rigs.Clear();
        }
    }
}
