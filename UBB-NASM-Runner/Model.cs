using System.IO;

namespace UBB_NASM_Runner
{
    internal static class Model
    {
        public static readonly string
            RootPath = Directory.GetCurrentDirectory(),
            BinPath = Path.Combine(RootPath, "bin"),
            CompiledPath = Path.Combine(RootPath, "compiled"),
            HushPath = Path.Combine(BinPath, "hushprojects"),
            NasmPath = Path.Combine(BinPath, "nasm.exe"),
            NLinkPath = Path.Combine(BinPath, "nlink.exe"),
            LabFilePath = Path.Combine(BinPath, "labs"),
            AcTestPath = Path.Combine(BinPath, "actest.exe");

        private static string _projectsPath = RootPath;

        public static readonly string[] ImportantFiles = {
            "actest.exe", "io.inc", "io.lib", "Kernel32.Lib",
            "ld.exe", "mio.inc", "mio.lib", "nasm.exe", "nlink.exe", "start.obj"
        };

        public static string GetProjectsPath() => _projectsPath;

        public static void SetProjectsPathAccordingToHush() {
            if (!File.Exists(HushPath)) return;
            if (File.ReadAllBytes(HushPath)[0].Equals(0b1)) {
                _projectsPath = Path.Combine(RootPath, "projects");
            }
        }
    }
}