using System.Windows.Forms;
using GTA;

namespace QuadcopterKamikaze
{
    public class KamikazeConfig
    {
        private readonly ScriptSettings _settings;

        public Keys MenuKey { get; }
        public Keys ExplodeKey { get; }
        public int ExplodeControllerButton { get; }
        public int ImpactForceThreshold { get; }
        public int ExplosionDamageScale { get; }
        public int CutsceneDuration { get; }
        public int BombMassFactor { get; }
        public int CameraDistance { get; }

        public KamikazeConfig()
        {
            _settings = ScriptSettings.Load(@"scripts\QuadcopterKamikaze\QuadcopterKamikaze.ini");

            MenuKey = _settings.GetValue("Keys", "MenuKey", Keys.F11);
            ExplodeKey = _settings.GetValue("Keys", "ExplodeKey", Keys.J);
            ExplodeControllerButton = _settings.GetValue("Controller", "ExplodeButton", 47);

            ImpactForceThreshold = _settings.GetValue("Bomb", "ImpactForceThreshold", 10);
            ExplosionDamageScale = _settings.GetValue("Bomb", "ExplosionDamageScale", 5);
            CutsceneDuration = _settings.GetValue("Bomb", "CutsceneDuration", 5);
            BombMassFactor = _settings.GetValue("Bomb", "BombMassFactor", 8);
            CameraDistance = _settings.GetValue("Camera", "CameraDistance", 15);
        }
    }
}
