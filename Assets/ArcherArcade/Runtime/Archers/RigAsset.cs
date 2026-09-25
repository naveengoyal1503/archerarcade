using System.Collections.Generic;
using UnityEngine;

namespace ArcherArcade.Archers
{
    /// <summary>
    /// One character look (hero, skin or enemy) loaded from Resources/Art/Characters/&lt;look&gt;.png + .json
    /// (Tools/art): part sprites with their joint pivots, body proportions and bow string style.
    /// </summary>
    public sealed class RigAsset
    {
        public string Id;
        public string Name;
        public bool Hero;
        public float Scale = 1f;
        public bool HasCape;
        public bool Boss;
        public bool WeakSpot;
        public string BowStyle;
        public Color StringColor = Color.white;
        public bool StringGlow;
        public float UnitsPerMeter = 140f;
        public float PxPerMeter = 200f;
        public readonly RigDims Dims = new RigDims();
        public readonly Dictionary<string, Sprite> Parts = new Dictionary<string, Sprite>();
        public Color[] Swatches = new Color[0];

        public bool Crossbow => BowStyle == "crossbow";

        /// <summary>Design units → metres for this look (bigger archers have bigger units).</summary>
        public float Meters => Scale / UnitsPerMeter;

        public Sprite Part(string name) => Parts.TryGetValue(name, out Sprite s) ? s : null;
    }
}
