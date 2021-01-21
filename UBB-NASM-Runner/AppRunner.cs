using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThreadState = System.Diagnostics.ThreadState;

// NOTE: nasm.exe, ld.exe, nlink.exe and projects created by them can't have their stdio redirected
// and so we cannot manipulate any of those, the way they handle their io is a bit of a mystery to me
namespace UBB_NASM_Runner
{
    public static class AppRunner
    {
        private static TaskCompletionSource<bool> _eventHandled;
        private static Process _process;

        private static string GetOutputFromTranscript(string outputFile) {
            var lines = File.ReadLines(outputFile).ToList();
            for (var i = lines.Count - 1; i >= 0 && lines[i].Equals(string.Empty); i--) {
                lines.RemoveAt(i);
            }
            var wantedString = string.Empty;
            for (var i = 0; i < lines.Count; i++) {
                if (!lines[i].Contains("Transcript started")) continue;
                i++;
                while (i < lines.Count && !Regex.Match(lines[i], @"^[\*]+$").Success) {
                    wantedString += lines[i++] + View.Nl;
                }
                
                break;
            }
            return wantedString;
        }

        public static async Task<Tuple <int, string>> StartConsoleApp(
            string executablePath, string arguments = "", bool powershellLaunch = true) {
            if (!File.Exists(executablePath)) {
                throw new ArgumentException($"{executablePath} does not exist");
            }
            Console.CancelKeyPress += HandleCtrlC;
            _eventHandled = new TaskCompletionSource<bool>();

            var outputFile = string.Empty;
            if (powershellLaunch) { 
                outputFile = Path.Combine(Model.TempPath, $"{DateTime.Now.ToFileTimeUtc()}.dat");
            }

            int exitCode;

            StreamReader outputReader;

            using (_process = new Process()) {
                try {
                    if (powershellLaunch) {
                        _process.StartInfo.FileName = Model.PowerShellPath;
                        _process.StartInfo.Arguments = "-Command \"" +
                                                       $"Start-Transcript -Path '{outputFile}'; " +
                                                       $"& '{executablePath}' {arguments}; " +
                                                       "Stop-Transcript; " +
                                                       "exit $LASTEXITCODE\"";
                    }
                    else {
                        _process.StartInfo.FileName = executablePath;
                        _process.StartInfo.Arguments = arguments;
                    }
                    _process.StartInfo.UseShellExecute = false;
                    _process.StartInfo.ErrorDialog = false;
                    _process.StartInfo.RedirectStandardOutput = true;
                    _process.EnableRaisingEvents = true;
                    _process.Exited += ProcessExited;
                    _process.Start();
                    
                    outputReader = _process.StandardOutput;
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    return Tuple.Create(1, string.Empty);
                }
                
                await Task.WhenAny(_eventHandled.Task);
                exitCode = _process.ExitCode;
            }

            if (!powershellLaunch)
                return !outputReader.Equals(null)
                    ? Tuple.Create(exitCode, await outputReader.ReadToEndAsync())
                    : Tuple.Create(1, string.Empty);
            var transcriptOutput = GetOutputFromTranscript(outputFile);
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }

            return Tuple.Create(exitCode, transcriptOutput);
        }
        
        private static Process GetProcessByExePath(string executablePath) {
            if (executablePath.Equals(string.Empty)) return null;

            return Process
                .GetProcessesByName(Path.GetFileNameWithoutExtension(executablePath))
                .FirstOrDefault(
                    p => p.MainModule != null && p.MainModule.FileName.Equals(executablePath)
                    );
        }

        private static void ProcessExited(object sender, EventArgs e) {
            // Here I can do stuff after the process has exited
            Console.CancelKeyPress -= HandleCtrlC;
            _eventHandled.TrySetResult(true);
        }
        
        private static void HandleCtrlC(object sender, ConsoleCancelEventArgs args) {
            Console.In.Dispose();
            // TODO: Write proper error message via the View class
            Console.WriteLine();
            Console.WriteLine("Process terminated via Ctr-C . . .");
            args.Cancel = true;
        }
    }
}