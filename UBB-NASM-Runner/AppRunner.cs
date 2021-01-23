using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// NOTE: nasm.exe, ld.exe, nlink.exe and projects created by them can't have their stdio redirected,
// at least not from .NET, and so we cannot manipulate any of those, the way they handle their io
// is a bit of a mystery to me
namespace UBB_NASM_Runner
{
    public static class AppRunner
    {
        private static TaskCompletionSource<bool> _eventHandled;
        private static Process _process;
        private static bool _terminatedWithCtrC = false;

        public static async Task<int> StartConsoleApp(string executablePath, string arguments = "") {
            if (!File.Exists(executablePath)) {
                throw new ArgumentException($"{executablePath} does not exist");
            }

            Console.CancelKeyPress += HandleCtrlC;
            _eventHandled = new TaskCompletionSource<bool>();

            int exitCode;
            using (_process = new Process()) {
                try {
                    _process.StartInfo.FileName = executablePath;
                    _process.StartInfo.Arguments = arguments;
                    _process.StartInfo.UseShellExecute = false;
                    _process.StartInfo.ErrorDialog = false;
                    _process.EnableRaisingEvents = true;
                    _process.Exited += ProcessExited;
                    _process.Start();
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    return 1;
                }

                await Task.WhenAny(_eventHandled.Task);
                exitCode = _terminatedWithCtrC
                    ? 15
                    : _process.ExitCode;
            }

            _terminatedWithCtrC = false;
            return exitCode;
        }

        // For now it looks like actest.exe terminates stuck apps, or maybe the system does
        public static Process GetProcessByExePath(string executablePath) {
            if (executablePath.Equals(string.Empty)) return null;

            return Process
                .GetProcessesByName(Path.GetFileNameWithoutExtension(executablePath))
                .FirstOrDefault(
                    p => p.MainModule?.FileName != null && p.MainModule != null &&
                         p.MainModule.FileName.Equals(executablePath)
                );
        }

        private static void ProcessExited(object sender, EventArgs e) {
            Console.CancelKeyPress -= HandleCtrlC;
            _eventHandled.TrySetResult(true);
        }

        private static void HandleCtrlC(object sender, ConsoleCancelEventArgs args) {
            _terminatedWithCtrC = true;
            Console.In.Dispose();
            View.PrintLine();
            View.PrintWarning("Process terminated with Ctr-C . . .");
            args.Cancel = true;
        }
    }
}