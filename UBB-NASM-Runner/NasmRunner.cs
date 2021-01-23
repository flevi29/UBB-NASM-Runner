using System;
using System.Threading.Tasks;


namespace UBB_NASM_Runner
{
    internal static class NasmRunner
    {
        private static async Task Main() {
            if (!Environment.OSVersion.Platform.Equals(PlatformID.Win32NT)) {
                View.PrintWarning("This program can only run on Windows.");
                View.ReadKey();
            }

            if (Environment.OSVersion.Version < new Version(6, 1)) {
                View.PrintWarning("Windows 7 SP1 or higher version required to run this program");
                View.ReadKey();
                return;
            }

            await Presenter.Start();

            Environment.Exit(0);
        }
    }
}