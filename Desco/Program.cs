using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Desco
{
    static class Program
    {
        public static Cobalt.IO.Endian Endianness = Cobalt.IO.Endian.BigEndian;

        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
