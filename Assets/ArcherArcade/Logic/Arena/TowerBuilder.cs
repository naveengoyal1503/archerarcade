using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>Adds a crate tower (a stack of crates, optionally with a TNT crate) to a prop list.</summary>
    public static class TowerBuilder
    {
        /// <summary>
        /// Stacks <paramref name="count"/> crates of size <paramref name="crateSize"/> standing on
        /// <paramref name="groundY"/> at <paramref name="centerX"/>. The crate at <paramref name="tntIndex"/>
        /// (−1 = none) is a TNT crate. Returns the index of the bottom crate.
        /// </summary>
        public static int Add(List<PropSpec> props, int towerId, double centerX, double groundY, int count,
            double crateSize = 0.9, int tntIndex = -1)
        {
            int first = props.Count;
            double half = crateSize * 0.5;
            for (int i = 0; i < count; i++)
            {
                var shape = Shape.Box(new Vec2(centerX, groundY + (i + 0.5) * crateSize), new Vec2(half, half));
                props.Add(new PropSpec
                {
                    Kind = i == tntIndex ? PropKind.TntCrate : PropKind.Crate,
                    Shape = shape,
                    Tower = towerId,
                    StackIndex = i
                });
            }
            return first;
        }
    }
}
