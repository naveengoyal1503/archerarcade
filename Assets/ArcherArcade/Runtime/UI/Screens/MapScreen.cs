using System.Collections.Generic;
using ArcherArcade.Core;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Theme;
using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// World map (design `map`, state 10): world tabs, a horizontally scrolling dotted path of the 20 World 1
    /// levels (nodes at x = 60 + i·86, y = 130 + sin(i·.85)·68; boss 72 dp with a crown), stars under cleared
    /// nodes, the current node pulsing, locked nodes shaking, chest markers after levels 5/10/15/20 and forest
    /// scenery along the way. Other worlds explain they arrive in a free update.
    /// </summary>
    public sealed class MapScreen : UiScreen
    {
        const float Step = 86f, Left = 60f, MidY = 130f, Amp = 68f;
        static readonly string[] WorldIcons = { Icons.Forest, Icons.Sunny, Icons.Landscape, Icons.LocalFireDepartment, Icons.Castle };
        static readonly uint[] WorldColors = { 0x12A67A, 0xF0641E, 0x1C9AD6, 0xE5484D, 0x6D4AFF };

        readonly List<RectTransform> _pulse = new List<RectTransform>();
        float _t;

        public override string Title => Loc.T("title_map");
        public override string Subtitle => Loc.F("map_stars", ServiceLocator.Profile.TotalStars, WorldOne.LevelCount * 3);

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;
            BuildTabs(root, p);

            RectTransform view = UiKit.Rect(root, "Viewport");
            UiKit.Stretch(view, 0f, 110f, 0f, 0f);
            view.gameObject.AddComponent<RectMask2D>();
            UiKit.HitArea(view);
            RectTransform content = UiKit.Rect(view, "Content");
            float width = Left + (WorldOne.LevelCount - 1) * Step + 150f;
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = new Vector2(width, 0f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.decelerationRate = 0.12f;

            BuildScenery(content);
            BuildPath(content, p);
            int current = profile.ContinueLevel;
            for (int i = 0; i < WorldOne.LevelCount; i++) BuildNode(content, i, profile, current);
            for (int c = 1; c <= Chests.Count; c++) BuildChest(content, c, profile);

            float target = Mathf.Max(0f, Left + (current - 1) * Step - 360f);
            content.anchoredPosition = new Vector2(-target, 0f);

            // Progress strip bottom-left.
            Image strip = UiKit.Card(root, "Progress", 14f);
            UiKit.At(strip.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 12f), new Vector2(190f, 34f));
            UiKit.Row(strip, 8f, TextAnchor.MiddleLeft, false, false, new RectOffset(12, 12, 0, 0));
            int cleared = profile.HighestLevelCleared;
            TextMeshProUGUI t = UiKit.Label(strip.transform, Loc.F("map_progress", cleared), FontRole.Body, 12f, p.Ink);
            UiKit.Size(t, 84f, 20f);
            ProgressBar bar = ProgressBar.Create(strip.transform, p.Track, Widgets.Green, 8f, 4f);
            UiKit.Size(bar, 70f, 8f);
            bar.SetValue(cleared / (float)WorldOne.LevelCount, false);
        }

        void BuildTabs(RectTransform root, Palette p)
        {
            RectTransform tabs = UiKit.Rect(root, "Tabs");
            UiKit.TopBand(tabs, 58f, 44f, 18f, 18f);
            UiKit.Row(tabs, 8f, TextAnchor.MiddleLeft, true, true);
            for (int w = 0; w < 5; w++)
            {
                int world = w;
                Button3D tab = Button3D.Create(tabs, "World" + (w + 1), p.Solid, Color.clear, 14f, 0f, 2f);
                if (w == 0) UiKit.Border(tab.Face.transform, UiKit.Hex(WorldColors[0]), 14f, 2.5f);
                RectTransform row = UiKit.Rect(tab.Body, "Row");
                UiKit.Stretch(row, 6f, 0f, 6f, 0f);
                UiKit.Row(row, 6f, TextAnchor.MiddleCenter);
                TextMeshProUGUI ic = UiKit.Glyph(row, WorldIcons[w], 17f, UiKit.Hex(WorldColors[w]));
                UiKit.Size(ic, 20f, 20f);
                TextMeshProUGUI name = UiKit.Label(row, Loc.T("world_" + (w + 1)), FontRole.Display, 14f, p.Ink);
                UiKit.Size(name, Mathf.Min(name.preferredWidth + 2f, 110f), 20f);
                UiKit.Fit(name);
                if (w > 0)
                {
                    TextMeshProUGUI lk = UiKit.Glyph(row, Icons.Lock, 13f, p.InkMuted);
                    UiKit.Size(lk, 14f, 14f);
                    var cg = tab.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0.85f;
                    tab.OnClick(() => Ui.ShowModal(new InfoModal(Icons.Lock, Loc.F("map_world_soon_title", Loc.T("world_" + (world + 1))),
                        Loc.F("map_world_soon_body", Loc.T("world_" + (world + 1))), Loc.T("map_back_forest"), Widgets.Green, UiKit.Hex(0x0B7555))));
                }
            }
        }

        static Vector2 NodePos(int i) => new Vector2(Left + i * Step, MidY + Mathf.Sin(i * 0.85f) * Amp);

        /// <summary>Content space is y-down from the top like the design; convert for anchored positions.</summary>
        static Vector2 Place(Vector2 designPos) => new Vector2(designPos.x, -designPos.y);

        void BuildPath(RectTransform content, Palette p)
        {
            Color dot = UiKit.Dark ? new Color(1, 1, 1, 0.35f) : new Color32(42, 35, 80, 64);
            float carry = 0f;
            for (int i = 0; i < WorldOne.LevelCount - 1; i++)
            {
                Vector2 a = NodePos(i), b = NodePos(i + 1);
                // Sample the segment as a gentle curve (the design uses a polyline; round dots every 16 dp).
                float len = Vector2.Distance(a, b);
                for (float d = carry; d < len; d += 16f)
                {
                    Vector2 q = Vector2.Lerp(a, b, d / len);
                    Image im = UiKit.Disc(content, "Dot", dot);
                    UiKit.At(im.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Place(q), new Vector2(7f, 7f));
                    carry = d + 16f - len;
                }
            }
        }

        void BuildScenery(RectTransform content)
        {
            string[] kinds = { "tree_round", "bush", "tree_pine", "flower_pink", "mushroom", "bush_berry", "tree_round", "rock", "flower_yellow" };
            for (int i = 0; i < WorldOne.LevelCount; i++)
            {
                Vector2 n = NodePos(i);
                bool above = Mathf.Sin(i * 0.85f) > 0f;
                string kind = kinds[(i * 7 + 3) % kinds.Length];
                Sprite s = ArtLibrary.Get(ArtLibrary.Scenery, kind);
                if (!s) continue;
                Image im = UiKit.Box(content, kind, new Color(1f, 1f, 1f, UiKit.Dark ? 0.55f : 0.8f), 0);
                im.sprite = s;
                im.type = Image.Type.Simple;
                im.preserveAspect = true;
                float h = kind.StartsWith("tree") ? 86f : 36f;
                float y = above ? n.y - 64f : n.y + 78f;
                UiKit.At(im.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0f), Place(new Vector2(n.x + Step * 0.5f, y)), new Vector2(h, h));
                im.transform.SetAsFirstSibling();
            }
        }

        void BuildNode(RectTransform content, int i, Profile profile, int current)
        {
            int number = i + 1;
            bool boss = number == WorldOne.LevelCount;
            float size = boss ? 72f : 54f;
            Logic.Save.LevelSave save = profile.Level(number);
            bool done = save.Cleared;
            bool unlocked = profile.IsLevelUnlocked(number);
            bool cur = unlocked && !done && number == current;
            Color face, edge;
            if (done) { face = UiKit.Hex(WorldColors[0]); edge = UiKit.Hex(0x0B7555); }
            else if (cur || unlocked) { face = UiKit.Hex(0xF0641E); edge = UiKit.Hex(0xB8460F); }
            else { face = UiKit.Hex(boss ? 0x8B84B8u : 0xA9A4C9u); edge = UiKit.Hex(0x7D78A3); }

            Vector2 pos = NodePos(i);
            RectTransform holder = UiKit.Rect(content, "Level" + number);
            UiKit.At(holder, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Place(pos), new Vector2(size, size));
            Button3D b = Button3D.Create(holder, "Node", face, edge, size * 0.5f, 5f, 3f);
            UiKit.Stretch((RectTransform)b.transform);
            b.Face.sprite = ShapeSprites.Circle;
            b.Face.type = Image.Type.Simple;
            if (b.Edge)
            {
                b.Edge.sprite = ShapeSprites.Circle;
                b.Edge.type = Image.Type.Simple;
            }
            Image ring = UiKit.Box(b.Body, "Ring", Color.white, 0);
            ring.sprite = ShapeSprites.Outline(size * 0.5f, 4f);
            ring.type = Image.Type.Sliced;
            UiKit.Stretch(ring.rectTransform);
            if (boss) b.AddIcon(Icons.Crown, 34f, Color.white);
            else if (!unlocked) b.AddIcon(Icons.Lock, 22f, Color.white);
            else b.AddLabel(number.ToString(), FontRole.Display, 20f, Color.white);
            if (cur) _pulse.Add(holder);

            string label = null;
            Color labelColor = Widgets.StarGold;
            if (done) label = Stars(save.Stars);
            else if (boss) label = "Forest Warden";
            if (label != null)
            {
                TextMeshProUGUI st = UiKit.Label(content, label, FontRole.Display, boss && !done ? 12f : 15f, labelColor, TextAlignmentOptions.Center);
                st.characterSpacing = done ? 3f : 0f;
                st.outlineWidth = 0.18f;
                st.outlineColor = new Color32(0, 0, 0, 60);
                UiKit.At((RectTransform)st.transform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), Place(pos + new Vector2(0f, size * 0.5f + 7f)), new Vector2(110f, 18f));
            }

            b.OnClick(() =>
            {
                if (!unlocked)
                {
                    Ui.Toast(Loc.F("map_locked_level", number - 1));
                    Shake(holder);
                    return;
                }
                Ui.Push(LoadoutScreen.Campaign(number));
            });
        }

        static string Stars(int n)
        {
            string s = "";
            for (int k = 0; k < 3; k++) s += k < n ? "★" : "<color=#FFFFFF80>★</color>";
            return s;
        }

        void BuildChest(RectTransform content, int chest, Profile profile)
        {
            int afterLevel = chest * 5;
            Vector2 a = NodePos(afterLevel - 1);
            Vector2 b = afterLevel < WorldOne.LevelCount ? NodePos(afterLevel) : a + new Vector2(Step, 0f);
            Vector2 pos = (a + b) * 0.5f + new Vector2(afterLevel < WorldOne.LevelCount ? 0f : 10f, Mathf.Sin(afterLevel * 0.85f) > 0 ? 58f : -58f);
            bool ready = profile.ChestAvailable(chest);
            bool opened = profile.Data.ChestsOpened.Contains(chest);
            string[] ids = { "chest_wood", "chest_silver", "chest_gold", "chest_forest" };
            RectTransform holder = UiKit.Rect(content, "Chest" + chest);
            UiKit.At(holder, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Place(pos), new Vector2(46f, 46f));
            Image im = UiKit.Box(holder, "Chest", opened ? new Color(1, 1, 1, 0.45f) : Color.white, 0);
            im.sprite = ArtLibrary.Get(ArtLibrary.Ui, ids[chest - 1] + (opened ? "_open" : ""));
            im.preserveAspect = true;
            im.raycastTarget = true;
            UiKit.Stretch(im.rectTransform);
            if (ready) _pulse.Add(holder);
            var btn = holder.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = im;
            btn.onClick.AddListener(() =>
            {
                ServiceLocator.Audio?.Play(Feel.SoundId.UiTap);
                if (ready) Ui.ShowModal(new ChestOpenModal(chest, () => Ui.Refresh()));
                else if (opened) Ui.Toast(Loc.T("map_chest_opened"));
                else Ui.Toast(Loc.F("map_chest_locked", afterLevel));
            });
        }

        static void Shake(RectTransform rt)
        {
            Vector2 home = rt.anchoredPosition;
            Tween.KillTarget(rt);
            Tween.Value(0f, 1f, 0.4f, k =>
            {
                if (rt) rt.anchoredPosition = home + new Vector2(Mathf.Sin(k * Mathf.PI * 6f) * 6f * (1f - k), 0f);
            }, EaseType.Linear, 0f, true, () => { if (rt) rt.anchoredPosition = home; }, rt);
        }

        public override void OnShow() => ServiceLocator.Audio?.PlayMusic(Feel.SoundId.MusicWorld1);

        public override void Tick(float dt)
        {
            _t += dt;
            float s = 1f + (Mathf.Sin(_t * Mathf.PI * 2f / 1.6f) * 0.5f + 0.5f) * 0.07f;
            foreach (RectTransform r in _pulse) if (r) r.localScale = new Vector3(s, s, 1f);
        }
    }
}
