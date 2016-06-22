using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace JustFOV
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int minFOV = 1;
        private const int maxFOV = 90;

        private static MainWindow Instance;
        private Model model;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new Model();
            model = DataContext as Model;

            SetFov.Click += SetFov_Click;

            FOVSlider.Minimum = minFOV;
            FOVSlider.Maximum = maxFOV;

            Instance = this;
            _hookID = SetHook(_onHookEvent);
        }

        ~MainWindow()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private void SetFov_Click(object sender, RoutedEventArgs e)
        {
            float FOVValue;
            if (float.TryParse(FOVText.Text, out FOVValue))
            {
                //var model = DataContext as Model;
                if (FOVValue < minFOV || FOVValue > 90)
                {
                    MessageBox.Show("FOV should be between 1 and 90", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    if (model != null) model.Fov = FOVValue;
                }
            }
            else
            {
                MessageBox.Show("Invalid FOV supplied", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }






       


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _onHookEvent = HookCallback;

        private static IntPtr _hookID = IntPtr.Zero;


        private const int WH_KEYBOARD_LL = 13;

        enum WM
        {
            KEYDOWN = 0x0100,
            KEYUP = 0x101

        }


        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {

                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }


        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM.KEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (Instance != null)
                {
                    Key k = KeyInterop.KeyFromVirtualKey(vkCode);

                    Instance.OnKeyHookEvent(k);
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        protected void OnKeyHookEvent(Key k)
        {
            switch (k)
            {
                default:
                    return;
                case Key.Decimal:
                    if (model != null)
                    {
                        model.PatchSetFov(!model.FovHackEnabled);
                        model.RecallFov();
                    }
                    break;
                case Key.Divide:
                    if (model != null)
                    {
                        model.Fov = Math.Min(maxFOV, model.Fov + 5f);
                    }
                    break;
                case Key.Multiply:
                    if (model != null)
                    {
                        model.Fov = Math.Max(minFOV, model.Fov - 5f);
                    }
                    break;
            }
            FOVText.Text = model.Fov.ToString("0");
        }
    }
}