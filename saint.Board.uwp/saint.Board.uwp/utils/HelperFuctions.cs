using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace saint.Board.uwp.utils
{
    internal class HelperFuctions
    {
        public static Point GetWindowCenter()
        {
            // 获取当前窗口
            var currentWindow = Window.Current;

            // 获取当前窗口的大小
            double windowWidth = currentWindow.Bounds.Width;
            double windowHeight = currentWindow.Bounds.Height;

            // 计算窗口中心的坐标
            double centerX = windowWidth / 2;
            double centerY = windowHeight / 2;

            return new Point(centerX, centerY);
        }

        public static void UpdateCanvasSize(FrameworkElement root, FrameworkElement output, FrameworkElement inkCanvas)
        {
            output.Width = root.ActualWidth;
            output.Height = root.ActualHeight;
            inkCanvas.Width = root.ActualWidth;
            inkCanvas.Height = root.ActualHeight;
        }

        public static string GetCurrentTime()
        {
            DateTime currentTime = DateTime.Now;

            string formattedTime = currentTime.ToString("HH:mm:ss");

            return formattedTime;
        }
    }

}
