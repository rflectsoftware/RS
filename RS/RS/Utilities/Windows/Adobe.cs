using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Utilities.Windows
{
    public static class Adobe
    {
        #region Public
        
        public static void CloseAdobe(string Filename)
        {
            string windowName = Path.GetFileName(Filename) + " - Adobe Acrobat Pro";
            IntPtr adobe = General.GetWindowHandle("AcrobatSDIWindow", windowName);

            if (adobe != IntPtr.Zero)
            {
                General.CloseWindow(adobe);
            }

            windowName = Path.GetFileName(Filename) + " - Adobe Acrobat";
            adobe = General.GetWindowHandle("AcrobatSDIWindow", windowName);
            
            if (adobe != IntPtr.Zero)
            {
                General.CloseWindow(adobe);
            }

            windowName = Path.GetFileName(Filename) + " - Adobe Reader";
            adobe = General.GetWindowHandle("AcrobatSDIWindow", windowName);

            if (adobe != IntPtr.Zero)
            {
                General.CloseWindow(adobe);
            }
        }

        #endregion
    }
}
