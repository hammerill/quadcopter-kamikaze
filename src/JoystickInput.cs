using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.DirectInput;

namespace QuadcopterKamikaze
{
    public class JoystickInput : IDisposable
    {
        private DirectInput _directInput;
        private Joystick _joystick;
        private JoystickState _state;
        private int[] _prevAxes;
        private bool _acquired;

        public string DeviceName { get; private set; }
        public bool IsConnected => _acquired;
        public int AxisCount { get; private set; }

        public void Connect(int deviceIndex)
        {
            Dispose();

            _directInput = new DirectInput();
            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            if (devices.Count == 0 || deviceIndex >= devices.Count)
                return;

            var deviceInstance = devices[deviceIndex];
            DeviceName = deviceInstance.InstanceName;

            _joystick = new Joystick(_directInput, deviceInstance.InstanceGuid);
            _joystick.Properties.BufferSize = 128;
            _joystick.Acquire();
            _acquired = true;

            _state = new JoystickState();
            _joystick.GetCurrentState(ref _state);
            _prevAxes = GetAllAxes(_state);
            AxisCount = _prevAxes.Length;
        }

        public List<string> ListDevices()
        {
            using (var di = new DirectInput())
            {
                return di.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
                    .Select(d => d.InstanceName)
                    .ToList();
            }
        }

        public bool Poll()
        {
            if (!_acquired || _joystick == null) return false;

            try
            {
                _joystick.Poll();
                _joystick.GetCurrentState(ref _state);
                return true;
            }
            catch
            {
                _acquired = false;
                return false;
            }
        }

        public float GetAxisNormalized(int axisIndex)
        {
            if (!_acquired || _state == null) return 0f;

            int[] axes = GetAllAxes(_state);
            if (axisIndex < 0 || axisIndex >= axes.Length) return 0f;

            return axes[axisIndex] / 65535f;
        }

        public int DetectMovedAxis(float deadzone = 0.15f)
        {
            if (!_acquired || _state == null || _prevAxes == null) return -1;

            int[] current = GetAllAxes(_state);
            int bestAxis = -1;
            float bestDelta = 0f;

            for (int i = 0; i < Math.Min(current.Length, _prevAxes.Length); i++)
            {
                float delta = Math.Abs((current[i] - _prevAxes[i]) / 65535f);
                if (delta > deadzone && delta > bestDelta)
                {
                    bestDelta = delta;
                    bestAxis = i;
                }
            }

            return bestAxis;
        }

        public void SavePreviousAxes()
        {
            if (!_acquired || _state == null) return;
            _prevAxes = GetAllAxes(_state);
        }

        private static int[] GetAllAxes(JoystickState state)
        {
            return new[]
            {
                state.X, state.Y, state.Z,
                state.RotationX, state.RotationY, state.RotationZ,
                state.Sliders.Length > 0 ? state.Sliders[0] : 0,
                state.Sliders.Length > 1 ? state.Sliders[1] : 0
            };
        }

        public static string GetAxisName(int index)
        {
            switch (index)
            {
                case 0: return "X";
                case 1: return "Y";
                case 2: return "Z";
                case 3: return "RotX";
                case 4: return "RotY";
                case 5: return "RotZ";
                case 6: return "Slider0";
                case 7: return "Slider1";
                default: return $"Axis{index}";
            }
        }

        public void Dispose()
        {
            if (_joystick != null)
            {
                if (_acquired)
                {
                    try { _joystick.Unacquire(); } catch { }
                }
                _joystick.Dispose();
                _joystick = null;
            }
            if (_directInput != null)
            {
                _directInput.Dispose();
                _directInput = null;
            }
            _acquired = false;
        }
    }
}
