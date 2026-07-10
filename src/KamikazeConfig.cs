using System.Windows.Forms;
using GTA;

namespace QuadcopterKamikaze
{
    public class KamikazeConfig
    {
        public const string IniPath = @"scripts\QuadcopterKamikaze\QuadcopterKamikaze.ini";
        private readonly ScriptSettings _settings;

        public Keys MenuKey { get; }
        public Keys ExplodeKey { get; }
        public int ExplodeControllerButton { get; }
        public int ImpactForceThreshold { get; }
        public int ExplosionDamageScale { get; }
        public int CutsceneDuration { get; }
        public int BombMassFactor { get; }
        public int CameraDistance { get; }

        public int BoomJoystickIndex { get; }
        public int BoomAxis { get; }
        public float BoomAxisThreshold { get; }

        public KamikazeConfig()
        {
            _settings = ScriptSettings.Load(IniPath);

            MenuKey = _settings.GetValue("Keys", "MenuKey", Keys.F11);
            ExplodeKey = _settings.GetValue("Keys", "ExplodeKey", Keys.J);
            ExplodeControllerButton = _settings.GetValue("Controller", "ExplodeButton", 47);

            ImpactForceThreshold = _settings.GetValue("Bomb", "ImpactForceThreshold", 10);
            ExplosionDamageScale = _settings.GetValue("Bomb", "ExplosionDamageScale", 5);
            CutsceneDuration = _settings.GetValue("Bomb", "CutsceneDuration", 2);
            BombMassFactor = _settings.GetValue("Bomb", "BombMassFactor", 8);
            CameraDistance = _settings.GetValue("Camera", "CameraDistance", 15);

            BoomJoystickIndex = _settings.GetValue("DirectInput", "JoystickIndex", 0);
            BoomAxis = _settings.GetValue("DirectInput", "BoomAxis", -1);
            BoomAxisThreshold = _settings.GetValue("DirectInput", "BoomAxisThreshold", 0.8f);
        }

        public static void SaveBoomAxis(int joystickIndex, int axis, float threshold)
        {
            var settings = ScriptSettings.Load(IniPath);
            settings.SetValue("DirectInput", "JoystickIndex", joystickIndex);
            settings.SetValue("DirectInput", "BoomAxis", axis);
            settings.SetValue("DirectInput", "BoomAxisThreshold", threshold);
            settings.Save();
        }
    }
}
