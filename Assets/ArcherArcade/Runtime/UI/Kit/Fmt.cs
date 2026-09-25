using ArcherArcade.Core;
using UnityEngine;

namespace ArcherArcade.UI
{
    /// <summary>Inline rich-text helpers: coin and star amounts with their gold icons (icon font fallback).</summary>
    public static class Fmt
    {
        public const string CoinColor = "#FFB800";

        public static string Coin => "<color=" + CoinColor + ">" + Icons.MonetizationOn + "</color>";

        public static string Coins(long n) => Coin + " " + Loc.N(n);

        public static string Star => "<color=#FFB800>★</color>";

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        public static string Tint(string text, Color c) => "<color=" + Hex(c) + ">" + text + "</color>";
    }
}
