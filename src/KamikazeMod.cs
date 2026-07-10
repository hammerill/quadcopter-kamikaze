using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace QuadcopterKamikaze
{
    public class KamikazeMod : Script
    {
        enum State { Disarmed, Armed, Cutscene }

        private readonly ObjectPool _menuPool;
        private readonly NativeMenu _menu;
        private readonly NativeCheckboxItem _armItem;
        private readonly NativeSliderItem _impactForceItem;
        private readonly NativeSliderItem _explosionRadiusItem;
        private readonly NativeSliderItem _cutsceneDurationItem;
        private readonly NativeSliderItem _bombMassItem;
        private readonly NativeSliderItem _cameraDistanceItem;
        private readonly NativeListItem<string> _explosionTypeItem;
        private readonly NativeListItem<string> _bombModelItem;

        private readonly KamikazeConfig _config;

        private State _state = State.Disarmed;
        private Entity _drone;
        private Prop _bombProp;
        private Camera _cutsceneCam;
        private Vector3 _explosionPos;
        private DateTime _cutsceneEnd;
        private Vector3 _previousVelocity;
        private bool _prevCollisionState;

        private Keys _menuKey;
        private Keys _explodeKey;
        private int _explodeControllerButton;

        public KamikazeMod()
        {
            _config = new KamikazeConfig();

            _menuKey = _config.MenuKey;
            _explodeKey = _config.ExplodeKey;
            _explodeControllerButton = _config.ExplodeControllerButton;

            _menuPool = new ObjectPool();
            _menu = new NativeMenu("Kamikaze Drone", "~r~Bomb Settings");

            _armItem = new NativeCheckboxItem("Arm Bomb", "Attach a bomb to the drone", false);
            _armItem.CheckboxChanged += OnArmChanged;

            _impactForceItem = new NativeSliderItem("Impact Trigger Force", 20, _config.ImpactForceThreshold - 1);
            _impactForceItem.Description = $"Velocity delta to trigger detonation (current: {_config.ImpactForceThreshold} m/s)";

            _explosionRadiusItem = new NativeSliderItem("Explosion Power", 20, _config.ExplosionDamageScale - 1);
            _explosionRadiusItem.Description = $"Explosion damage scale (current: {_config.ExplosionDamageScale})";

            _cutsceneDurationItem = new NativeSliderItem("Cutscene Duration (s)", 15, _config.CutsceneDuration - 1);
            _cutsceneDurationItem.Description = $"Camera time after explosion (current: {_config.CutsceneDuration}s)";

            _bombMassItem = new NativeSliderItem("Bomb Mass Factor", 20, _config.BombMassFactor - 1);
            _bombMassItem.Description = $"Extra downward force (current: {_config.BombMassFactor})";

            _cameraDistanceItem = new NativeSliderItem("Camera Distance", 30, _config.CameraDistance - 1);
            _cameraDistanceItem.Description = $"Cutscene camera distance (current: {_config.CameraDistance}m)";

            _explosionTypeItem = new NativeListItem<string>("Explosion Type", "Visual style",
                "BombStandard", "Rocket", "Plane", "Tanker", "OrbitalCannon", "Blimp");
            _explosionTypeItem.SelectedIndex = 0;

            _bombModelItem = new NativeListItem<string>("Bomb Model", "Visual prop attached to drone",
                "prop_bomb_01", "prop_bomb_01_s", "prop_c4_final", "hei_prop_heist_thermite", "w_ex_pe");
            _bombModelItem.SelectedIndex = 0;

            _menu.Add(_armItem);
            _menu.Add(_bombModelItem);
            _menu.Add(_bombMassItem);
            _menu.Add(_impactForceItem);
            _menu.Add(_explosionTypeItem);
            _menu.Add(_explosionRadiusItem);
            _menu.Add(_cutsceneDurationItem);
            _menu.Add(_cameraDistanceItem);
            _menuPool.Add(_menu);

            _impactForceItem.ValueChanged += (s, e) =>
                _impactForceItem.Description = $"Velocity delta to trigger detonation (current: {_impactForceItem.Value + 1} m/s)";
            _explosionRadiusItem.ValueChanged += (s, e) =>
                _explosionRadiusItem.Description = $"Explosion damage scale (current: {_explosionRadiusItem.Value + 1})";
            _cutsceneDurationItem.ValueChanged += (s, e) =>
                _cutsceneDurationItem.Description = $"Camera time after explosion (current: {_cutsceneDurationItem.Value + 1}s)";
            _bombMassItem.ValueChanged += (s, e) =>
                _bombMassItem.Description = $"Extra downward force (current: {_bombMassItem.Value + 1})";
            _cameraDistanceItem.ValueChanged += (s, e) =>
                _cameraDistanceItem.Description = $"Cutscene camera distance (current: {_cameraDistanceItem.Value + 1}m)";

            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += OnAborted;
            Interval = 0;
        }

        private int ImpactForce => _impactForceItem.Value + 1;
        private int ExplosionDamage => _explosionRadiusItem.Value + 1;
        private int CutsceneSecs => _cutsceneDurationItem.Value + 1;
        private int BombMass => _bombMassItem.Value + 1;
        private int CamDist => _cameraDistanceItem.Value + 1;

        private int GetExplosionType()
        {
            switch (_explosionTypeItem.SelectedItem)
            {
                case "Rocket": return 4;
                case "Plane": return 8;
                case "Tanker": return 31;
                case "OrbitalCannon": return 59;
                case "Blimp": return 29;
                default: return 50; // BombStandard
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == _menuKey)
                _menu.Visible = !_menu.Visible;

            if (e.KeyCode == _explodeKey && _state == State.Armed)
                TriggerExplosion();
        }

        private void OnTick(object sender, EventArgs e)
        {
            _menuPool.Process();

            switch (_state)
            {
                case State.Armed:
                    TickArmed();
                    break;
                case State.Cutscene:
                    TickCutscene();
                    break;
            }
        }

        private void TickArmed()
        {
            if (_drone == null || !_drone.Exists())
            {
                _drone = FindDrone();
                if (_drone == null) return;
                AttachBomb();
            }

            if (_bombProp != null && _bombProp.Exists() && !_bombProp.IsAttached())
                AttachBomb();

            ApplyBombWeight();
            CheckImpact();
            CheckControllerExplodeButton();
        }

        private void ApplyBombWeight()
        {
            if (_drone == null || !_drone.Exists()) return;

            float force = BombMass * 3.0f;
            Function.Call(Hash.APPLY_FORCE_TO_ENTITY,
                _drone.Handle,
                0,
                0f, 0f, -force,
                0f, 0f, 0f,
                0,
                false, false, true, false, true);
        }

        private void CheckImpact()
        {
            if (_drone == null || !_drone.Exists()) return;

            Vector3 currentVelocity = _drone.Velocity;
            Vector3 delta = currentVelocity - _previousVelocity;
            float impactMagnitude = delta.Length();
            _previousVelocity = currentVelocity;

            bool collided = _drone.HasCollided;
            if (collided && !_prevCollisionState && impactMagnitude > ImpactForce)
                TriggerExplosion();

            _prevCollisionState = collided;
        }

        private void CheckControllerExplodeButton()
        {
            if (_explodeControllerButton < 0) return;
            if (Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, _explodeControllerButton))
                TriggerExplosion();
        }

        private void TriggerExplosion()
        {
            if (_state != State.Armed || _drone == null || !_drone.Exists()) return;

            _explosionPos = _drone.Position;
            _state = State.Cutscene;

            _drone.IsVisible = false;
            _drone.IsPositionFrozen = true;
            _drone.IsCollisionEnabled = false;
            _drone.Position = _explosionPos + new Vector3(0, 0, 500);

            if (_bombProp != null && _bombProp.Exists())
            {
                _bombProp.Detach();
                _bombProp.Delete();
                _bombProp = null;
            }

            Function.Call(Hash.ADD_EXPLOSION,
                _explosionPos.X, _explosionPos.Y, _explosionPos.Z,
                GetExplosionType(),
                (float)ExplosionDamage,
                true, false,
                1.0f, false);

            SetupCutsceneCamera();
            _cutsceneEnd = DateTime.UtcNow.AddSeconds(CutsceneSecs);
        }

        private void SetupCutsceneCamera()
        {
            float dist = CamDist;

            Vector3 camPos = _explosionPos + new Vector3(dist * 0.7f, dist * 0.7f, dist * 0.5f);
            _cutsceneCam = World.CreateCamera(camPos, Vector3.Zero, 50f);
            _cutsceneCam.PointAt(_explosionPos);

            Function.Call(Hash.SET_CAM_ACTIVE, _cutsceneCam.Handle, true);
            Function.Call(Hash.RENDER_SCRIPT_CAMS, true, true, 500, true, false);
        }

        private void TickCutscene()
        {
            Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
            Game.DisableAllControlsThisFrame();

            if (DateTime.UtcNow >= _cutsceneEnd)
                EndCutscene();
        }

        private void EndCutscene()
        {
            if (_cutsceneCam != null && _cutsceneCam.Exists())
            {
                Function.Call(Hash.RENDER_SCRIPT_CAMS, false, true, 500, true, false);
                _cutsceneCam.Delete();
                _cutsceneCam = null;
            }

            if (_drone != null && _drone.Exists())
            {
                Vector3 respawnPos = _explosionPos + new Vector3(0, 0, 200);
                _drone.Position = respawnPos;
                _drone.Velocity = Vector3.Zero;
                _drone.IsPositionFrozen = false;
                _drone.IsCollisionEnabled = true;
                _drone.IsVisible = true;
            }

            _previousVelocity = Vector3.Zero;

            AttachBomb();
            _state = State.Armed;
        }

        private void OnArmChanged(object sender, EventArgs e)
        {
            if (_armItem.Checked)
                ArmBomb();
            else
                DisarmBomb();
        }

        private void ArmBomb()
        {
            _drone = FindDrone();
            if (_drone == null)
            {
                GTA.UI.Notification.Show("~r~Kamikaze: ~w~No drone found! Start flying first.");
                _armItem.Checked = false;
                return;
            }

            Function.Call(Hash.SET_ENTITY_RECORDS_COLLISIONS, _drone.Handle, true);
            _previousVelocity = _drone.Velocity;
            _prevCollisionState = false;

            AttachBomb();
            _state = State.Armed;
            GTA.UI.Notification.Show("~r~Kamikaze: ~w~Bomb armed!");
        }

        private void DisarmBomb()
        {
            if (_bombProp != null && _bombProp.Exists())
            {
                _bombProp.Detach();
                _bombProp.Delete();
                _bombProp = null;
            }
            _state = State.Disarmed;
            _drone = null;
            GTA.UI.Notification.Show("~g~Kamikaze: ~w~Bomb disarmed.");
        }

        private void AttachBomb()
        {
            if (_drone == null || !_drone.Exists()) return;

            if (_bombProp != null && _bombProp.Exists())
            {
                _bombProp.Detach();
                _bombProp.Delete();
            }

            string modelName = _bombModelItem.SelectedItem;
            var model = new Model(modelName);
            if (!model.Request(1000))
            {
                GTA.UI.Notification.Show("~r~Kamikaze: ~w~Failed to load bomb model.");
                return;
            }

            _bombProp = World.CreateProp(model, _drone.Position, false, false);
            if (_bombProp == null) return;

            _bombProp.IsCollisionEnabled = false;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY,
                _bombProp.Handle, _drone.Handle, 0,
                0f, 0f, -0.15f,
                0f, 0f, 0f,
                false, false, false, false, 2, true);

            model.MarkAsNoLongerNeeded();
        }

        private Entity FindDrone()
        {
            Vector3 camPos = Function.Call<Vector3>(Hash.GET_FINAL_RENDERED_CAM_COORD);

            Prop[] nearbyProps = World.GetNearbyProps(camPos, 3f);
            if (nearbyProps.Length == 0)
                nearbyProps = World.GetNearbyProps(Game.Player.Character.Position, 15f);

            Prop closest = null;
            float closestDist = float.MaxValue;

            foreach (var prop in nearbyProps)
            {
                if (prop == null || !prop.Exists()) continue;
                if (prop == _bombProp) continue;
                if (prop.IsAttached()) continue;

                float dist = prop.Position.DistanceTo(camPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = prop;
                }
            }

            return closest;
        }

        private void OnAborted(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (_bombProp != null && _bombProp.Exists())
            {
                _bombProp.Detach();
                _bombProp.Delete();
                _bombProp = null;
            }
            if (_cutsceneCam != null && _cutsceneCam.Exists())
            {
                Function.Call(Hash.RENDER_SCRIPT_CAMS, false, false, 0, true, false);
                _cutsceneCam.Delete();
                _cutsceneCam = null;
            }
            if (_drone != null && _drone.Exists())
            {
                _drone.IsVisible = true;
                _drone.IsPositionFrozen = false;
                _drone.IsCollisionEnabled = true;
            }
            _state = State.Disarmed;
        }
    }
}
