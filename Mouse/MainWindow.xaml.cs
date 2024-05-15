using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Mouse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        int _screen1XMax = -1;
        int _screen2XMin = 0;
        int _screen2XMax = 3839;
        int _screen3XMin = 5760;

        private Portal _portal1 = new Portal(1629, 3140, 685, 2160);
        private Portal _portal2 = new Portal(475, 2160, 1629, 3100);

        MouseHooker _hooker;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _position = "init....";
            _position2 = "updating mouse moves";
            _hooker = new MouseHooker(AdjustPointer);
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _hooker.UnHook();
        }

        private (int X, int Y) Scale(int x, int y)
        {
            if (x < 0 || x > 3839)
            {
                return (Convert.ToInt32(x * 1.5), Convert.ToInt32(y * 1.5));
            }
            return (x, y);
        }

        private (int X, int Y)? AdjustPointer(int oldX, int oldY, int newX, int newY)
        {
            (int scaledOldX, int scaledOldY) = Scale(oldX, oldY);
            (int scaledNewX, int scaledNewY) = Scale(newX, newY);
            if (scaledOldX <= _screen1XMax && scaledNewX >= _screen2XMin)
            {
                return Adjust1To2(scaledOldX, scaledOldY, scaledNewX, scaledNewY);
            }
            if (scaledOldX >= _screen2XMin && scaledNewX <= _screen1XMax)
            {
                return Adjust2To1(scaledOldX, scaledOldY, scaledNewX, scaledNewY);
            }
            if (scaledOldX <= _screen2XMax && scaledNewX >= _screen3XMin)
            {
                return Adjust2To3(scaledOldX, scaledOldY, scaledNewX, scaledNewY);
            }
            if (scaledOldX >= _screen3XMin && scaledNewX <= _screen2XMax)
            {
                return Adjust3To2(scaledOldX, scaledOldY, scaledNewX, scaledNewY);
            }
            return null;
        }

        private (int, int) Adjust1To2(int oldX, int oldY, int newX, int newY)
        {
            if (_portal1.InLeftRange(oldY))
            {
                var adjustedY = _portal1.ConvertLeftToRight(oldY);
                return (newX, Convert.ToInt32(adjustedY));
            }
            return (oldX, oldY);
        }

        private (int, int) Adjust2To1(int oldX, int oldY, int newX, int newY)
        {
            if (_portal1.InRightRange(oldY))
            {
                var adjustedY = _portal1.ConvertRightToLeft(oldY);
                return (newX, Convert.ToInt32(adjustedY));
            }
            return (oldX, oldY);
        }

        private (int, int) Adjust2To3(int oldX, int oldY, int newX, int newY)
        {
            if (_portal2.InLeftRange(oldY))
            {
                var adjustedY = _portal2.ConvertLeftToRight(oldY);
                return (newX, Convert.ToInt32(adjustedY));
            }
            return (oldX, oldY);
        }

        private (int, int) Adjust3To2(int oldX, int oldY, int newX, int newY)
        {
            if (_portal2.InRightRange(oldY))
            {
                var adjustedY = _portal2.ConvertRightToLeft(oldY);
                return (newX, Convert.ToInt32(adjustedY));
            }
            return (oldX, oldY);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _position;

        public string Position
        {
            get { return _position; }
            set
            {
                if (string.Equals(value, _position))
                    return;
                _position = value;
                OnPropertyChanged(nameof(Position));
            }
        }

        private string _position2;

        public string Position2
        {
            get { return _position2; }
            set
            {
                if (string.Equals(value, _position2))
                    return;
                _position2 = value;
                OnPropertyChanged(nameof(Position2));
            }
        }

        public class Portal
        {
            public Portal(double leftMinY, double leftMaxY, double rightMinY, double rightMaxY)
            {
                LeftMinY = leftMinY;
                LeftMaxY = leftMaxY;
                RightMinY = rightMinY;
                RightMaxY = rightMaxY;
            }

            public double LeftMinY { get ; set; }
            public double LeftMaxY { get; set; }
            public double RightMinY { get; set; }
            public double RightMaxY { get; set; }

            public bool InLeftRange(double y)
            {
                return LeftMinY <= y && LeftMaxY >= y;
            }

            public bool InRightRange(double y)
            {
                return RightMinY <= y && RightMaxY >= y;
            }

            public double ConvertLeftToRight(double leftY)
            {
                var percentage = (leftY - LeftMinY) / (LeftMaxY - LeftMinY);
                return percentage * (RightMaxY - RightMinY) + RightMinY;
            }

            public double ConvertRightToLeft(double rightY)
            {
                var percentage = (rightY - RightMinY) / (RightMaxY - RightMinY);
                return percentage * (LeftMaxY - LeftMinY) + LeftMinY;
            }
        }

        public class Win32
        {
            [DllImport("User32.Dll")]
            public static extern long GetCursorPos(out POINT lpPoint);

            [DllImport("User32.Dll")]
            public static extern long SetCursorPos(int x, int y);

            [DllImport("User32.Dll")]
            public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int x;
                public int y;

                public POINT(int X, int Y)
                {
                    x = X;
                    y = Y;
                }
            }
        }
    }
}
