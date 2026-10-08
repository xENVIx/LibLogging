using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LibLogging_10
{
    public static class FilePaths
    {

        public static String CurrentRunningDirectory
        {
            get
            {
                return AppContext.BaseDirectory;
            }
        }


        public static String AppDataApplicationPath 
        {  
            get
            {

                String retPath = "";
                if (OperatingSystem.IsWindows())
                {
                    string appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string appName = $"{Process.GetCurrentProcess().ProcessName.Replace(" ", "_")}";

                    retPath = Path.Combine(appDataLocalPath, appName);

                }
                else if (OperatingSystem.IsLinux())
                {
                    //string home = Environment.GetEnvironmentVariable("HOME")
                    //    ?? Environment.GetEnvironmentVariable("USERPROFILE") 
                    //    ?? "";

                    //String appName = $"{Process.GetCurrentProcess().ProcessName.Replace(" ", "_")}";

                    //retPath =  Path.Combine(home, appName);

                    string home =
                        Environment.GetEnvironmentVariable("HOME") ??
                        Environment.GetEnvironmentVariable("USERPROFILE") ??
                        Environment.GetEnvironmentVariable("SUDO_HOME") ??
                        (Environment.GetEnvironmentVariable("SUDO_USER") != null
                            ? $"/home/{Environment.GetEnvironmentVariable("SUDO_USER")}"
                            : "");

                    string appName = Process.GetCurrentProcess().ProcessName.Replace(" ", "_");

                    retPath = Path.Combine(home, appName);


                }

                //Console.WriteLine($"Log Path: {retPath}");
                //LOG.Verbose($"LOG Path: {retPath}");
                Console.WriteLine($"Log Path: {retPath}");

                return retPath;
            } 
        }

    }
}
