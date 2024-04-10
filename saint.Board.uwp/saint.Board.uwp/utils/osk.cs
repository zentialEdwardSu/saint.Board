using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace saint.Board.uwp.utils
{
    internal class osk
    {
        //[DllImport("user32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        //[DllImport("user32.dll", SetLastError = true)]
        //static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //    static void StartOnScreenKeyboard()
        //{
        //    try
        //    {
        //        IntPtr oskHandle = FindWindow("OSKMainClass", null);

        //        if (oskHandle == IntPtr.Zero)
        //        {
        //            Process.Start("osk.exe");
        //        }
        //        else
        //        {
        //            const uint WM_SYSCOMMAND = 0x0112;
        //            const uint SC_RESTORE = 0xF120;
        //            PostMessage(oskHandle, WM_SYSCOMMAND, (IntPtr)SC_RESTORE, IntPtr.Zero);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error: " + ex.Message);
        //    }
        //}
    }
}
