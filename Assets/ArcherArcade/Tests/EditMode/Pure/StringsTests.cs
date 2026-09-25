using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Modes;
using ArcherArcade.Logic.Text;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    /// <summary>
    /// PROGRESS Phase 14: every UI string exists in English and Hinglish with the same placeholders, and every key
    /// the Runtime asks for (Loc.T / Loc.F literals, plus keys built from enums and data) is in the table.
    /// </summary>
    public class StringsTests
    {
        static readonly Regex Placeholder = new Regex(@"\{(\d+)\}");

        [Test]
        public void EveryEntryHasBothLanguagesWithTheSamePlaceholders()
        {
            foreach (KeyValuePair<string, string[]> kv in StringTable.Entries)
            {
                Assert.AreEqual(2, kv.Value.Length, kv.Key);
                Assert.IsFalse(string.IsNullOrWhiteSpace(kv.Value[0]), kv.Key + " English");
                Assert.IsFalse(string.IsNullOrWhiteSpace(kv.Value[1]), kv.Key + " Hinglish");
                CollectionAssert.AreEquivalent(Holders(kv.Value[0]), Holders(kv.Value[1]), kv.Key);
                Assert.IsFalse(kv.Value[0].Contains("️") || kv.Value[1].Contains("️"), kv.Key + " has an emoji variation selector");
            }
        }

        static List<string> Holders(string s)
        {
            var list = new List<string>();
            foreach (Match m in Placeholder.Matches(s)) if (!list.Contains(m.Value)) list.Add(m.Value);
            return list;
        }

        [Test]
        public void TheTributeIsKept()
        {
            Assert.AreEqual("In loving memory of Maa ❤", Strings.Get("tribute", Lang.English));
            Assert.AreEqual("In loving memory of Maa ❤", Strings.Get("tribute", Lang.Hinglish));
        }

        [Test]
        public void KeysBuiltFromGameDataExist()
        {
            var missing = new List<string>();
            void Need(string key)
            {
                if (!Strings.Has(key)) missing.Add(key);
            }
            foreach (IdeaCard c in Enum.GetValues(typeof(IdeaCard)))
            {
                Need("idea_" + c);
                Need("idea_" + c + "_text");
            }
            foreach (ArrowTip t in Enum.GetValues(typeof(ArrowTip))) Need("tipname_" + t);
            foreach (BoosterKind b in Enum.GetValues(typeof(BoosterKind)))
            {
                Need("booster_" + b);
                Need("booster_" + b + "_desc");
            }
            foreach (AbilityKind a in Enum.GetValues(typeof(AbilityKind)))
            {
                if (a != AbilityKind.None) Need("ability_" + a);
            }
            foreach (string hero in ArcherTable.HeroIds)
            {
                Need("passive_" + hero);
                Need("ability_" + ArcherTable.Hero(hero).Ability + "_desc");
            }
            foreach (Element e in Enum.GetValues(typeof(Element))) Need("el_" + e.ToString().ToLowerInvariant());
            foreach (DailyTemplate t in Enum.GetValues(typeof(DailyTemplate))) Need("tpl_" + t);
            foreach (DailyTwist t in Enum.GetValues(typeof(DailyTwist))) Need("twist_" + t);
            foreach (LevelDef l in WorldOne.Levels()) Need(l.LossTip);
            foreach (CosmeticDef c in CosmeticCatalog.All) Need("cos_" + c.Id);
            foreach (BadgeDef b in BadgeCatalog.All) Need("badge_" + b.Id + "_desc");
            foreach (string arena in ArenaCatalog.TwoPlayerArenas) Need("arena_" + arena);
            for (int i = 1; i <= 5; i++) Need("world_" + i);
            for (int i = 1; i <= Chests.Count; i++) Need("chest_" + i);
            CollectionAssert.IsEmpty(missing, "missing: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryKeyTheRuntimeUsesExists()
        {
            string runtime = FindRuntimeFolder();
            if (runtime == null)
            {
                Assert.Inconclusive("Runtime sources not found from " + Directory.GetCurrentDirectory());
                return;
            }
            // Whole literal keys only: Loc.T("key") / Loc.F("key", …); prefixes built at runtime ("tipname_" + tip) are
            // covered by the enum tests above. Doc comments are skipped.
            var used = new Regex("Loc\\.(?:T|F)\\(\\s*\"([A-Za-z0-9_]+)\"\\s*[,)]");
            var docLine = new Regex("^\\s*///.*$", RegexOptions.Multiline);
            var missing = new List<string>();
            int count = 0;
            foreach (string file in Directory.GetFiles(runtime, "*.cs", SearchOption.AllDirectories))
            {
                string code = docLine.Replace(File.ReadAllText(file), "");
                foreach (Match m in used.Matches(code))
                {
                    count++;
                    string key = m.Groups[1].Value + " (" + Path.GetFileName(file) + ")";
                    if (!Strings.Has(m.Groups[1].Value) && !missing.Contains(key)) missing.Add(key);
                }
            }
            Assert.Greater(count, 50, "the scan found the Runtime's Loc calls");
            CollectionAssert.IsEmpty(missing, "missing: " + string.Join(", ", missing));
        }

        static string FindRuntimeFolder()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, "Assets", "ArcherArcade", "Runtime");
                    if (Directory.Exists(candidate)) return candidate;
                    dir = dir.Parent;
                }
            }
            return null;
        }
    }
}
