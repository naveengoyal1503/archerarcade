using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    /// <summary>
    /// PROGRESS Phase 12: every sound id the game plays (Runtime Feel/SoundId.cs) is listed in DESIGN_TOKENS §8 and
    /// has a clip (Resources/Audio/Final or the Tools/gen_sounds.py placeholders in Audio/Generated).
    /// </summary>
    public class AudioFilesTests
    {
        static string FindGameFolder()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, "Assets", "ArcherArcade");
                    if (Directory.Exists(Path.Combine(candidate, "Runtime"))) return candidate;
                    dir = dir.Parent;
                }
            }
            return null;
        }

        static List<string> RuntimeIds(string game)
        {
            string code = File.ReadAllText(Path.Combine(game, "Runtime", "Feel", "SoundId.cs"));
            var ids = new List<string>();
            foreach (Match m in new Regex("const string \\w+ = \"([a-z0-9_]+)\"").Matches(code)) ids.Add(m.Groups[1].Value);
            return ids;
        }

        [Test]
        public void EverySoundIdHasAClip()
        {
            string game = FindGameFolder();
            if (game == null)
            {
                Assert.Inconclusive("Game folder not found from " + Directory.GetCurrentDirectory());
                return;
            }
            List<string> ids = RuntimeIds(game);
            Assert.Greater(ids.Count, 50, "found the sound ids");
            var missing = new List<string>();
            foreach (string id in ids)
            {
                bool found = false;
                foreach (string folder in new[] { "Final", "Generated" })
                {
                    string dir = Path.Combine(game, "Resources", "Audio", folder);
                    if (File.Exists(Path.Combine(dir, id + ".wav")) || File.Exists(Path.Combine(dir, id + "_v1.wav")) ||
                        File.Exists(Path.Combine(dir, id + ".ogg")) || File.Exists(Path.Combine(dir, id + "_v1.ogg")))
                        found = true;
                }
                if (!found) missing.Add(id);
            }
            CollectionAssert.IsEmpty(missing, "no clip for: " + string.Join(", ", missing));
        }

        [Test]
        public void SoundIdsMatchTheDesignTokens()
        {
            string game = FindGameFolder();
            if (game == null)
            {
                Assert.Inconclusive("Game folder not found");
                return;
            }
            string tokens = File.ReadAllText(Path.Combine(game, "..", "..", "Docs", "DESIGN_TOKENS.md"));
            string section = tokens.Split(new[] { "## 8." }, StringSplitOptions.None)[1].Split(new[] { "\n## " }, StringSplitOptions.None)[0];
            var listed = new HashSet<string>();
            foreach (Match m in new Regex("`((?:sfx|mus|sting)_[a-z0-9_]+?)(?:_(\\d)\\.\\.(\\d))?`").Matches(section))
            {
                if (m.Groups[2].Success)
                {
                    for (int k = int.Parse(m.Groups[2].Value); k <= int.Parse(m.Groups[3].Value); k++) listed.Add(m.Groups[1].Value + "_" + k);
                }
                else listed.Add(m.Groups[1].Value);
            }
            var runtime = new HashSet<string>(RuntimeIds(game));
            var notListed = new List<string>();
            foreach (string id in runtime) if (!listed.Contains(id)) notListed.Add(id);
            var notUsed = new List<string>();
            foreach (string id in listed) if (!runtime.Contains(id)) notUsed.Add(id);
            CollectionAssert.IsEmpty(notListed, "played but not in DESIGN_TOKENS §8: " + string.Join(", ", notListed));
            CollectionAssert.IsEmpty(notUsed, "in DESIGN_TOKENS §8 but no SoundId: " + string.Join(", ", notUsed));
        }
    }
}
