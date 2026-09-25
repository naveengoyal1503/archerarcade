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

        /// <summary>Puts this config into a match setup (level builders start from the Logic defaults).</summary>
        public void Apply(MatchSetup setup)
        {
            setup.Shot = Shot;
            setup.Damage = Damage;
            setup.Body = Body;
            setup.Rules = Rules;
            setup.PropRules = Props;
            setup.Boosters = Boosters;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _loaded = null;
    }
}
