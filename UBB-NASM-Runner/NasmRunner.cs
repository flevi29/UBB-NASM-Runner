
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms.VisualStyles;

namespace UBB_NASM_Runner
{
    internal static class NasmRunner
    {
        private static async Task Main(string[] args) {

            if (!Environment.OSVersion.Platform.Equals(PlatformID.Win32NT)) {
                View.PrintWarning("This program can only run on Windows.");
                View.ReadKey();
            }
            if (Environment.OSVersion.Version < new Version(6, 1)) {
                View.PrintWarning("Windows 7 SP1 or higher version required to run this program");
                View.ReadKey();
                return;
            }
            if (!File.Exists(Model.PowerShellPath)) {
                View.PrintWarning("Powershell is required to be installed on " +
                                       "this system to run this program.");
                View.ReadKey();
                return;
            }
            
            // Start app in either a Windows Terminal window or a CMD window
            // This way clearing the screen doesn't interfere with
            // previously typed stuff in the terminal by the user
            // IDK if this part is a good idea
            goto Start;
            // if (!(args.Length > 0 && args[0].Equals("-startincurrentwindow"))) {
            //     string terminal, arg;
            //     var fname = Path.GetDirectoryName(Environment.CommandLine) + '\\' +
            //                 Path.GetFileNameWithoutExtension(Environment.CommandLine);
            //
            //     if (File.Exists(Environment.ExpandEnvironmentVariables(
            //         "%USERPROFILE%\\AppData\\Local\\Microsoft\\WindowsApps\\wt.exe"))) {
            //         terminal = "wt";
            //         arg = $"--startingDirectory \"{Directory.GetCurrentDirectory()}\" " +
            //                   $"-p \"Windows PowerShell\" --title \"NASM\" \"{fname}\" -startincurrentwindow";
            //     }
            //     else {
            //         if (ConsoleWillBeDestroyedAtTheEnd()) {
            //             goto Start;
            //         }
            //         terminal = "cmd";
            //         arg = $"/C \"\"{fname}\" -startincurrentwindow\"";
            //     }
            //
            //     try {
            //         var p = new Process {StartInfo = {
            //             FileName = terminal, 
            //             Arguments = arg, 
            //             UseShellExecute = true
            //         }};
            //         p.Start();
            //     }
            //     catch (Exception e) {
            //         Console.WriteLine(e.Message);
            //         throw;
            //     }
            //     
            //     Environment.Exit(0);
            // }

            Start:
            await Presenter.Start();

            Environment.Exit(0);
        }
        
        // private static bool ConsoleWillBeDestroyedAtTheEnd() {
        //     return GetConsoleProcessList(new uint[1], 1).Equals(1);
        // }
        //
        // [DllImport("kernel32.dll", SetLastError = true)]
        // private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);
        
    }
}