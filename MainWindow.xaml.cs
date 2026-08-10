using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace audio_controller
{
    public partial class MainWindow : Window
    {
        private NAudio.CoreAudioApi.MMDevice? device;
        private NotifyIcon? notifyIcon;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_UP = 1;
        private const int HOTKEY_ID_DOWN = 2;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int VK_UP = 0x26;
        private const int VK_DOWN = 0x28;
        private const int WM_HOTKEY = 0x0312;

        private IntPtr hwnd;

        public MainWindow()
        {
            InitializeComponent();

            InitAudio();
            SetupNotifyIcon();
        }

        // どこを左クリックしてドラッグしてもウィンドウが動く機能
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        // 右上の「✕」ボタンを押したらタスクバー（通知領域）に隠す
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void InitAudio()
        {
            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                device = enumerator.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);

                UpdateVolumeUI();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("オーディオデバイスの取得に失敗しました: " + ex.Message);
            }
        }

        private void UpdateVolumeUI()
        {
            if (device == null) return;
            int currentVolume = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            sliderVolume.Value = currentVolume;
            if (lblVolume != null)
            {
                lblVolume.Text = $"🔊 音量: {currentVolume}%";
            }
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (device == null) return;
            float newVolume = (float)(sliderVolume.Value / 100.0);
            device.AudioEndpointVolume.MasterVolumeLevelScalar = newVolume;

            if (lblVolume != null)
            {
                lblVolume.Text = $"🔊 音量: {(int)sliderVolume.Value}%";
            }
        }

        private void ChangeVolume(int delta)
        {
            if (device == null) return;
            int currentVol = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            int newVol = Math.Max(0, Math.Min(100, currentVol + delta));

            device.AudioEndpointVolume.MasterVolumeLevelScalar = newVol / 100f;
            sliderVolume.Value = newVol;
            lblVolume.Text = $"🔊 音量: {newVol}%";
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            hwnd = helper.Handle;

            var source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(HwndHook);

            RegisterHotKey(hwnd, HOTKEY_ID_UP, MOD_CONTROL | MOD_ALT, VK_UP);
            RegisterHotKey(hwnd, HOTKEY_ID_DOWN, MOD_CONTROL | MOD_ALT, VK_DOWN);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_UP)
                {
                    ChangeVolume(5);
                    handled = true;
                }
                else if (id == HOTKEY_ID_DOWN)
                {
                    ChangeVolume(-5);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void SetupNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Visible = true;
            notifyIcon.Text = "Audio Controller";

            notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("開く", null, (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            });
            contextMenu.Items.Add("終了", null, (s, e) =>
            {
                System.Windows.Application.Current.Shutdown();
            });
            notifyIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (hwnd != IntPtr.Zero)
            {
                UnregisterHotKey(hwnd, HOTKEY_ID_UP);
                UnregisterHotKey(hwnd, HOTKEY_ID_DOWN);
            }
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
            }
            base.OnClosed(e);
        }
    }
}