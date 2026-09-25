using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;

namespace ArcherArcade.Archers
{
    /// <summary>
    /// Which exported look (Resources/Art/Characters) draws an archer: heroes by equipped skin, enemies by id
    /// (the second Twig Twin wears blue). Also the archer's theme colour and element icon for UI.
    /// </summary>
    public static class ArcherLooks
    {
        public static string ForHero(string archerId, string skinId)
        {
            if (string.IsNullOrEmpty(skinId) || skinId == CosmeticCatalog.DefaultSkinFor(archerId)) return archerId;
            return skinId;
        }

        public static string ForFighter(ArcherDef def, string skinId, int sameIdIndex)
        {
            switch (def.Id)
            {
                case "ranger":
                case "fire":
                case "electric":
                case "bomb":
                    return ForHero(def.Id, skinId);
                case "twig_twin":
                    return sameIdIndex % 2 == 0 ? "twig_red" : "twig_blue";
                default:
                    return def.Id;
            }
        }

        /// <summary>Card colour per hero (design ARCHERS colours).</summary>
        public static uint Color(string archerId)
        {
            switch (archerId)
            {
                case "fire": return 0xFF8A3D;
                case "electric": return 0xFFD23F;
                case "bomb": return 0xFF5A6E;
                case "ranger": return 0x2ED3A0;
                default: return 0x9D86FF;
            }
        }

        public static string ElementIcon(Element e)
        {
            switch (e)
            {
                case Element.Fire: return UI.Icons.LocalFireDepartment;
                case Element.Electric: return UI.Icons.ElectricBolt;
                case Element.Bomb: return UI.Icons.Bomb;
                case Element.Ice: return UI.Icons.AcUnit;
                case Element.Poison: return UI.Icons.Science;
                default: return UI.Icons.Target;
            }
        }
    }
}
