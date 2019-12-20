using System;
using System.Windows.Forms;

namespace WindowsFormsApp3
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        public static string global_path;
        public static string global_password;
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 1)
                global_path = args[0];
            if (args.Length == 2)
            {
                global_path = args[0];
                global_password = args[1];
            } 
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
