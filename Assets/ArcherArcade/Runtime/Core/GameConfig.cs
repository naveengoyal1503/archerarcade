using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;
using UnityEngine;

namespace ArcherArcade.Core
{
    /// <summary>
    /// Every tuning number in one ScriptableObject (Resources/GameConfig.asset, made by the Build ▸ Config builder):
    /// shot physics, damage, hit zones, turn rules, props, boosters and the economy. The Logic reads these objects;
    /// the UI shows values from them. Missing asset → the Logic defaults (same numbers).
    /// </summary>
    [CreateAssetMenu(menuName = "Archer Arcade/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        public ShotConfig Shot = new ShotConfig();
        public DamageConfig Damage = new DamageConfig();
        public BodyConfig Body = new BodyConfig();
        public MatchConfig Rules = new MatchConfig();
        public PropConfig Props = new PropConfig();
        public BoosterConfig Boosters = new BoosterConfig();
        public EconomyConfig Economy = new EconomyConfig();

        static GameConfig _loaded;

        public static GameConfig Load()
        {
            if (_loaded) return _loaded;
            _loaded = Resources.Load<GameConfig>("GameConfig");
            if (!_loaded) _loaded = CreateInstance<GameConfig>();
            return _loaded;
        }

        /// <summary>
        /// Puts copies of this config into a match setup (LevelBuilder.Configure hook): copies, so a mode that tweaks
        /// its own rules (Training's no-timer) never changes the shared config.
        /// </summary>
        public void Apply(MatchSetup setup)
        {
            setup.Shot = Clone(Shot);
            setup.Damage = Clone(Damage);
            setup.Body = Clone(Body);
            setup.Rules = Clone(Rules);
            setup.PropRules = Clone(Props);
            setup.Boosters = Clone(Boosters);
        }

        static T Clone<T>(T value) where T : class => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _loaded = null;
    }
}
