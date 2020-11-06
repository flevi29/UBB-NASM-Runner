
using System;
using System.IO;

namespace UBB_NASM_Runner
{
    internal static class NasmRunner
    {
        private static void Main() {
            // For some reason on Windows Terminal trying to reset the buffer size gives an error
            // so I'll leave this alone for now
            
            // var originalbufferheight = Console.BufferHeight;
            
            if (Environment.OSVersion.Version < new Version(6, 1)) {
                View.PrintWarning("Windows 7 SP1 or higher version required to run this program");
                Console.ReadLine();
                return;
            }
            if (!File.Exists(Model.PowerShellPath)) {
                View.PrintWarning("Powershell is required to be installed on " +
                                       "this system to run this program.");
                Console.ReadLine();
                return;
            }

            Console.SetBufferSize(Console.BufferWidth, 3000);

            Presenter.Start();
            
            // originalbufferheight = Math.Max(originalbufferheight, Console.WindowHeight);
            // Console.SetBufferSize(Console.BufferWidth, originalbufferheight);
        }
    }
}