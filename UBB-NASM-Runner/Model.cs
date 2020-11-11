using System;
using System.IO;

namespace UBB_NASM_Runner
{
    internal static class Model
    {
        public static readonly string RootPath = Directory.GetCurrentDirectory(),
            BinPath = Path.Combine(RootPath, "bin"),
            ProjectsPath = Path.Combine(RootPath, "projects"),
            CompiledPath = Path.Combine(RootPath, "compiled"),
            NasmPath = Path.Combine(BinPath, "nasm.exe"),
            NLinkPath = Path.Combine(BinPath, "nlink.exe"),
            LabFilePath = Path.Combine(BinPath, "labs.txt"),
            AcTestPath = Path.Combine(BinPath, "actest.exe"),
            PowerShellPath = Environment.ExpandEnvironmentVariables(
                "%SystemRoot%\\system32\\WindowsPowerShell\\v1.0\\powershell.exe");
        
        public static readonly string[] ImportantFiles = {
            "actest.exe", "io.inc", "io.lib", "Kernel32.Lib",
            "ld.exe", "mio.inc", "mio.lib", "nasm.exe", "nlink.exe", "start.obj"
        };
    }
}