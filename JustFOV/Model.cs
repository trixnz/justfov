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

        private bool _fovHackEnabled;
        private float _fovRecall;

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

        public bool FovHackEnabled
        {
            get
            {
                return _fovHackEnabled;
            }
        }

        #endregion

        ~Model()
        {
            PatchSetFov(false);
        }

        public void PatchSetFov(bool overrideEnable)
        {
            _fovHackEnabled = overrideEnable;

            Natives.WriteBytes(_handle, _setFovCall,
                overrideEnable ? new byte[] {0x90, 0x90, 0x90, 0x90, 0x90} : _originalCallBytes);
        }

        public void RecallFov()
        {
            PatchFov(_fovRecall);
        }

        private void PatchFov(float newFov)
        {
            _fovRecall = newFov;

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
        //private readonly IntPtr _cameraManagerPtr = new IntPtr(0x142F0CB58);
        //private readonly IntPtr _setFovCall = new IntPtr(0x143B3BC2D);

        //private const int CurrentCameraOffset = 0x5c0;
        //private const int CameraFlagsOffset = 0x55e;
        //private const int FovOffset1 = 0x580;
        //private const int FovOffset2 = 0x584;

        // Patch version 1.04 (03/06/2016)
        // NOTE(xforce): If I were to inject something in the process I could make this work independent of version
        // But I don't think that was the intention of this program
        // Or I write some memory search using ReadProcessMemory but that is slow
        // So on the next update use this to find the address to CameraManager easily
        // Or use a dump in IDA look for the ctor of CCameraManager
        // 48 8B 05 ? ? ? ? F3 0F 10 05 ? ? ? ? 4D 89 C1 48 8B 90 ? ? ? ? 4C 8D 41 0C 31 C0
        // This is a IDA compatible pattern

        // This is the pattern for the SetFOV Call
        // E8 ? ? ? ? 0F 28 BC 24 ? ? ? ? 0F 28 B4 24 ? ? ? ? 48 8B 9C 24 ? ? ? ? 48 81 C4 ? ? ? ?
        // I however thing it's not required to patch it as this is only called
        // when the camera manager gets created
        // Or maybe I just have the wrong call
        // But I was not able to acquire the versions used previously, so ye

        //private readonly IntPtr _cameraManagerPtr = new IntPtr(0x142EBEBD0);
        //private readonly IntPtr _setFovCall = new IntPtr(0x143ADAD71);

        //private const int CurrentCameraOffset = 0x5c0;
        //private const int CameraFlagsOffset = 0x55e;
        //private const int FovOffset1 = 0x580;
        //private const int FovOffset2 = 0x584;

        // Patch version 1.05 (29/07/2016)
        private readonly IntPtr _cameraManagerPtr = new IntPtr(0x142ED0E20);
        private readonly IntPtr _setFovCall = new IntPtr(0x143AEFF41);

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