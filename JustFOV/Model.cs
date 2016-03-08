using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using JustFOV.Annotations;
using JustFOV.Util;
using System.Windows.Input;

namespace JustFOV
{
    public class Model : INotifyPropertyChanged
    {
        private const float DegToRad = (float) (Math.PI/180.0f);
        private const float RadToDeg = (float) (180.0f/Math.PI);
        private readonly IntPtr _handle;

        private readonly byte[] _originalCallBytes;

        private ICommand _restoreDefaultFOVCommand;
        private float _defaultFOV = 34.89f;

        public Model()
        {
            var processes = Process.GetProcessesByName("JustCause3");
            if (processes.Length == 0)
            {
                MessageBox.Show("Failed to find JustCause3.exe process", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown(0);
            }

            _handle = Natives.OpenProcess(
                (uint) Natives.ProcessAccessFlags.VMOperation |
                (uint) Natives.ProcessAccessFlags.VMRead |
                (uint) Natives.ProcessAccessFlags.VMWrite, false, processes[0].Id);

            if (_handle == IntPtr.Zero)
            {
                MessageBox.Show("Failed to open a handle to JustCause3.exe process", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown(0);
            }

            _originalCallBytes = Natives.ReadBytes(_handle, _setFovCall, 5);
            PatchSetFov(true);
        }

        #region Properties

        public float Fov
        {
            get { return GetFov()*RadToDeg; }
            set
            {
                PatchFov(value*DegToRad);

                OnPropertyChanged();
            }
        }

        public ICommand RestoreDefaultFOV
        {
            get
            {
                return _restoreDefaultFOVCommand ??
                    (_restoreDefaultFOVCommand = new RelayCommand(p => Fov = _defaultFOV));
            }
        }

        #endregion

        ~Model()
        {
            PatchSetFov(false);
        }

        private void PatchSetFov(bool overrideEnable)
        {
            Natives.WriteBytes(_handle, _setFovCall,
                overrideEnable ? new byte[] {0x90, 0x90, 0x90, 0x90, 0x90} : _originalCallBytes);
        }

        private void PatchFov(float newFov)
        {
            var cameraManager = Natives.ReadIntPtr(_handle, _cameraManagerPtr);
            var currentCamera = Natives.ReadIntPtr(_handle, cameraManager + CurrentCameraOffset);

            // Update the flags to indicate an FOV change has occurred
            var flags = Natives.ReadBytes(_handle, currentCamera + CameraFlagsOffset, 1);
            flags[0] |= 0x10;
            Natives.WriteBytes(_handle, currentCamera + CameraFlagsOffset, flags);

            // Update the actual FOV values
            Natives.WriteFloat(_handle, currentCamera + FovOffset1, newFov);
            Natives.WriteFloat(_handle, currentCamera + FovOffset2, newFov);
        }

        private float GetFov()
        {
            var cameraManager = Natives.ReadIntPtr(_handle, _cameraManagerPtr);
            var currentCamera = Natives.ReadIntPtr(_handle, cameraManager + CurrentCameraOffset);

            return Natives.ReadFloat(_handle, currentCamera + FovOffset2);
        }

        #region Offsets

        // Patch 1.021
        //private readonly IntPtr _cameraManagerPtr = new IntPtr(0x142e72a58);
        //private readonly IntPtr _setFovCall = new IntPtr(0x143a546cf);

        //private const int CurrentCameraOffset = 0x5c0;
        //private const int CameraFlagsOffset = 0x55e;
        //private const int FovOffset1 = 0x580;
        //private const int FovOffset2 = 0x584;

        // Patch 20/01/2016
        //private readonly IntPtr _cameraManagerPtr = new IntPtr(0x142e72a58);
        //private readonly IntPtr _setFovCall = new IntPtr(0x143229d70);

        //private const int CurrentCameraOffset = 0x5c0;
        //private const int CameraFlagsOffset = 0x55e;
        //private const int FovOffset1 = 0x580;
        //private const int FovOffset2 = 0x584;

        // Patch 07/03/2016
        private readonly IntPtr _cameraManagerPtr = new IntPtr(0x142F0CB58);
        private readonly IntPtr _setFovCall = new IntPtr(0x143B3BC2D);

        private const int CurrentCameraOffset = 0x5c0;
        private const int CameraFlagsOffset = 0x55e;
        private const int FovOffset1 = 0x580;
        private const int FovOffset2 = 0x584;
        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}