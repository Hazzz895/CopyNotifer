using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CopyNotifer
{
    public partial class MainWindow : Window
    {
        private KeyboardHooker _hooker;
        private ClipboardHooker _clipboardHooker;
        private HwndSource hwnd;

        public MainWindow()
        {
            InitializeComponent();
            Height = SystemParameters.FullPrimaryScreenHeight;
            Width = SystemParameters.FullPrimaryScreenWidth;
        }


        public async void OnCtrlCPressed()
        {
            if (Visibility != Visibility.Visible)
            {
                Visibility = Visibility.Visible; 
            }

            var fAnimation = new System.Windows.Media.Animation.DoubleAnimation(1, TimeSpan.FromSeconds(0));
            PopupGrid.BeginAnimation(OpacityProperty, fAnimation);
            GC.SuppressFinalize(fAnimation);

            await Task.Delay(TimeSpan.FromSeconds(0.5));

            var animation = new System.Windows.Media.Animation.DoubleAnimation(0, TimeSpan.FromSeconds(0.5));

            animation.Completed += (s, e) =>
            {
                if (PopupGrid.Opacity == 0)
                {
                    Visibility = Visibility.Hidden; 
                }
                GC.SuppressFinalize(animation);
                GC.Collect();
            };

            PopupGrid.BeginAnimation(OpacityProperty, animation);
        }

        private NotifyIcon _trayIcon;

        private void InitTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.WinLogo, 
                Visible = true,
                Text = "Copy Notifer",
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Exit", null, (s,e) => Shutdown());

            _trayIcon.ContextMenuStrip = contextMenu;

            _trayIcon.DoubleClick += (s, e) => Shutdown();
        }

        private void Shutdown()
        {

            if (_hooker != null)
            {
                _hooker.KeyHookedEventHandler -= KeyHooked;
                _hooker.Dispose();
            }

            if (_clipboardHooker != null)
            {
                _clipboardHooker.ClipboardChanged -= (_, __) => OnCtrlCPressed();
                _clipboardHooker?.Dispose();
            }

            _trayIcon?.Dispose();

            System.Windows.Application.Current.Shutdown();
            GC.SuppressFinalize(this);
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Shutdown();
        }

        HookMode _hookMode = HookMode.Clipboard;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hide();
            hwnd = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);

            if (_hookMode == HookMode.Hotkey)
            {
                _hooker = new KeyboardHooker();
                _hooker.KeyHookedEventHandler += KeyHooked;
            }
            else if (_hookMode == HookMode.Clipboard)
            {
                _clipboardHooker = new ClipboardHooker(hwnd);
                _clipboardHooker.ClipboardChanged += (_, __) => OnCtrlCPressed();
            }

            InitTrayIcon();
        }


        private bool ctrlPressed = false;
        private void KeyHooked(object sender, KeyHookedEventArgs e)
        {
            if (e.State == (IntPtr)0x0100)
            {
                if (ctrlPressed && e.Key == System.Windows.Forms.Keys.C)
                {
                    OnCtrlCPressed();
                }
                else if (e.Key == System.Windows.Forms.Keys.LControlKey)
                {
                    ctrlPressed = true;
                }
            }
            else if (e.State == (IntPtr)0x0101)
            {
                if (e.Key == System.Windows.Forms.Keys.LControlKey)
                {
                    ctrlPressed = false;
                }
            }
        }
    }

    internal enum HookMode
    {
        Hotkey,
        Clipboard
    }
}
