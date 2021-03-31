using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// NOTE: nasm.exe, nlink.exe and projects created by them can't have their stdio redirected,
// at least not from .NET, and so we cannot manipulate any of those, the way they handle their io
// is a bit of a mystery to me
namespace UBB_NASM_Runner
{
    public class AppRunner
    {
        private TaskCompletionSource<bool> _eventHandled;
        private bool _terminatedWithCtrC;

        public async Task<int> StartConsoleApp(string executablePath, string arguments = "") {
            Process process;

            if (!File.Exists(executablePath)) {
                throw new ArgumentException($"{executablePath} does not exist");
            }

            Console.CancelKeyPress += HandleCtrlC;
            _eventHandled = new TaskCompletionSource<bool>();

            int exitCode;
            using (process = new Process()) {
                try {
                    process.StartInfo.FileName = executablePath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.ErrorDialog = false;
                    process.EnableRaisingEvents = true;
                    process.Exited += ProcessExited;
                    process.Start();
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    return 1;
                }

                await Task.WhenAny(_eventHandled.Task);
                exitCode = _terminatedWithCtrC
                    ? 15
                    : process.ExitCode;
            }

            _terminatedWithCtrC = false;
            return exitCode;
        }

        // For now it looks like actest.exe terminates stuck apps, or maybe the system does
        public Process GetProcessByExePath(string executablePath) {
            if (executablePath.Equals(string.Empty)) return null;

            return Process
                .GetProcessesByName(Path.GetFileNameWithoutExtension(executablePath))
                .FirstOrDefault(
                    p => p.MainModule?.FileName != null && p.MainModule != null &&
                         p.MainModule.FileName.Equals(executablePath)
                );
        }

        private void ProcessExited(object sender, EventArgs e) {
            Console.CancelKeyPress -= HandleCtrlC;
            _eventHandled.TrySetResult(true);
        }

        private void HandleCtrlC(object sender, ConsoleCancelEventArgs args) {
            _terminatedWithCtrC = true;
            Console.In.Dispose();
            View.PrintLine();
            View.PrintWarning("Process terminated with Ctr-C . . .");
            args.Cancel = true;
        }
    }
}