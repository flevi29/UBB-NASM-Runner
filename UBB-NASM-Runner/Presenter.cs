using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UBB_NASM_Runner
{
    internal static class Presenter
    {
        public static async Task Start() {
            View.SetTitle("NASM");
            uint counter = 1;
            var filePath = string.Empty;

            CleanUpDirectory();

            var command = new ConsoleKeyInfo(
                'f', ConsoleKey.F, false, false, false);

            while (true) {
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (command.Key.Equals(ConsoleKey.F)) {
                    //Select which file to compile
                    var oldFilePath = filePath;
                    filePath = ChooseFile();

                    if (!filePath.Equals(string.Empty)) {
                        View.SetTitle(Path.GetFileNameWithoutExtension(filePath));
                        if (!oldFilePath.Equals(filePath)) {
                            counter = 1;
                        }
                    }
                }
                else if (command.Key.Equals(ConsoleKey.T)) {
                    var changeLab = (command.Modifiers & ConsoleModifiers.Control) != 0;
                    await AcTest(filePath, changeLab);
                    goto InputPart;
                }
                else if (command.Key.Equals(ConsoleKey.Q)) {
                    View.PrintLine();
                    goto EndOfWhile;
                }

                PrintDecoration(counter++, filePath);

                await CompileAndStartApp(filePath);
                InputPart:
                PrintControlsAndReadAllowedKey(filePath, out command);
            }

            EndOfWhile: ;
        }

        private static void PrintControlsAndReadAllowedKey(string filePath, out ConsoleKeyInfo command) {
            View.PrintLine();
            View.PrintControls();
            View.CursorVisibility(false);
            do {
                command = View.ReadKey();
            } while (!IsAllowedCommand(command, filePath));

            View.CursorVisibility(true);
        }

        private static void PrintDecoration() {
            View.PrintAcTestDecoration();
        }

        private static void PrintDecoration(uint counter, string filePath) {
            View.PrintNewInstanceDecoration(counter, filePath);
        }

        private static string GetFileBinPathWithObjExtension(string fPath) {
            return Path.Combine(Model.BinPath, GetFileNameWithObjExtension(fPath));
        }

        private static string GetFileNameWithObjExtension(string fPath) {
            return Path.GetFileNameWithoutExtension(fPath) + ".obj";
        }

        private static string GetFileNameWithExeExtension(string fPath) {
            return Path.GetFileNameWithoutExtension(fPath) + ".exe";
        }

        private static string GetFileNameWithAsmExtension(string fPath) {
            return Path.GetFileNameWithoutExtension(fPath) + ".asm";
        }

        private static bool FileExistsCaseSensitive(string filename) {
            var name = Path.GetDirectoryName(filename);

            return name != null
                   && Array.Exists(Directory.GetFiles(name), s => s.Equals(Path.GetFullPath(filename)));
        }

        private static string GetFileWithSpecificCaseInsensitiveExtension(string fPath, string extension) {
            var asmFPath = "";
            var asmString = extension.ToCharArray();

            for (var i = 1; i < Math.Pow(asmString.Length, 2); i++) {
                var variation = new char[asmString.Length];
                for (var j = 0; j < asmString.Length; j++) {
                    variation[j] = (i & (1 << j)) != 0 ? char.ToUpper(asmString[j]) : asmString[j];
                }

                var asmFPathTemp = Path.Combine(
                    Path.GetDirectoryName(fPath) ?? string.Empty,
                    $"{Path.GetFileNameWithoutExtension(fPath)}.{new string(variation)}"
                );
                if (!FileExistsCaseSensitive(asmFPathTemp)) continue;
                if (asmFPath.Equals("")) {
                    asmFPath = asmFPathTemp;
                }
                else {
                    throw new Exception("multiple files with same basename and varied extension case is unallowed");
                }
            }

            return asmFPath;
        }

        private static string IndexNameIfDuplicate(string filePath) {
            if (!File.Exists(filePath)) return filePath;
            var regMatch = Regex.Match(filePath, @"^(.+)([_0-9]*)(\.[\S]+)$");
            if (!int.TryParse(regMatch.Groups[2].Value, out var numberInt)) {
                numberInt = 0;
            }

            while (File.Exists(filePath)) {
                filePath = $"{regMatch.Groups[1].Value}_{++numberInt}{regMatch.Groups[3].Value}";
            }

            return filePath;
        }

        private static void CreateHush() {
            View.PrintWhiteText("Should your project files be moved to \\projects for " +
                                $"a cleaner directory?");
            View.PrintWarning("If you change your mind make sure you delete " +
                              $"\\bin\\hushprojects{View.Nl}");
            View.PrintInputText("answer(Y/n)");
            var answer = View.ReadFromInput().ToLower();
            View.PrintLine(View.Nl);
            var value = new[] {"yes", "ye", "y"}.Contains(answer)
                ? new byte[] {0b1}
                : new byte[] {0b0};
            File.WriteAllBytes(Model.HushPath, value);
        }

        private static void CleanUpDirectory() {
            try {
                var filePaths = GetAllFilePathsWithinDir(Model.RootPath);

                if (!Directory.Exists(Model.BinPath)) {
                    Directory.CreateDirectory(Model.BinPath);
                }

                if (!File.Exists(Model.HushPath)) {
                    CreateHush();
                }

                Model.SetProjectsPathAccordingToHush();

                if (!Directory.Exists(Model.GetProjectsPath())) {
                    Directory.CreateDirectory(Model.GetProjectsPath());
                }

                if (!Directory.Exists(Model.CompiledPath)) {
                    Directory.CreateDirectory(Model.CompiledPath);
                }

                string[] projectExtensions = {".asm", ".inc"};
                string[] ioMio = {"mio.dll", "io.dll"};
                foreach (var filePath in filePaths) {
                    var filePathExt = Path.GetExtension(filePath).ToLower();
                    var fPathDir = Path.GetDirectoryName(filePath);
                    if (Model.ImportantFiles.Contains(Path.GetFileName(filePath))) {
                        if (Model.BinPath.Equals(fPathDir)) continue;
                        // core files to be moved to \bin
                        var destPath = Path.Combine(Model.BinPath, Path.GetFileName(filePath));
                        if (File.Exists(destPath)) continue;
                        File.Move(filePath, destPath);
                    }
                    else if (ioMio.Contains(Path.GetFileName(filePath))) {
                        if (Model.CompiledPath.Equals(fPathDir)) continue;
                        // io.dll and mio.dll need be moved to \compiled
                        var destPath = Path.Combine(Model.CompiledPath, Path.GetFileName(filePath));
                        if (File.Exists(destPath)) continue;
                        File.Move(filePath, destPath);
                    }
                    else if (projectExtensions.Contains(filePathExt) && !Model.GetProjectsPath().Equals(fPathDir)
                                                                     && !Model.BinPath.Equals(fPathDir)) {
                        // .asm and .inc files will be placed in \projects or \
                        var destPath =
                            IndexNameIfDuplicate(Path.Combine(Model.GetProjectsPath(), Path.GetFileName(filePath)));
                        File.Move(filePath, destPath);
                    }
                }
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }
        }

        private static bool IsAllowedCommand(ConsoleKeyInfo command, string filename) {
            return filename.Equals("")
                ? command.Key.Equals(ConsoleKey.Q) || command.Key.Equals(ConsoleKey.F)
                : command.Key.Equals(ConsoleKey.Enter) || command.Key.Equals(ConsoleKey.Q)
                                                       || command.Key.Equals(ConsoleKey.F)
                                                       || command.Key.Equals(ConsoleKey.T);
        }

        private static void ChangeCurrentOrAppendLabString(string fileName, string labCommand) {
            if (!File.Exists(Model.LabFilePath)) {
                File.Create(Model.LabFilePath).Close();
            }

            var fileLines = File.ReadLines(Model.LabFilePath).ToList();
            var index = 0;
            while (index < fileLines.Count && !Regex.IsMatch(fileLines[index],
                $@"^[\S]+ {Regex.Escape(fileName)}[\s]*$")) {
                index++;
            }

            var lineToAdd = $"{labCommand} {fileName}";
            if (index.Equals(fileLines.Count)) {
                fileLines.Add(lineToAdd);
            }
            else {
                fileLines[index] = lineToAdd;
            }

            File.WriteAllLines(Model.LabFilePath, fileLines);
        }

        private static string GetRequiredLabString(string fileName) {
            if (!File.Exists(Model.LabFilePath)) return null;
            var fileLine = File
                .ReadLines(Model.LabFilePath)
                .FirstOrDefault(line =>
                    Regex.IsMatch(line, $@"^[\S]+ {Regex.Escape(fileName)}[\s]*$"));

            return fileLine?.Split(' ').First();
        }

        private static void PrintAvailableLabs() {
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = Model.AcTestPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            if (!process.WaitForExit(5000)) {
                return;
            }

            while (!process.StandardOutput.EndOfStream) {
                var line = process.StandardOutput.ReadLine();
                if (line == null || !line.ToLower().Contains("labs:")) continue;
                View.PrintWhiteText(line);
                break;
            }
        }

        private static async Task AcTest(string filePath, bool changeLab) {
            PrintDecoration();

            if (!File.Exists(Model.AcTestPath)) {
                View.PrintWarning("Tester program not found");
                return;
            }

            var fileName = Path.GetFileName(filePath);
            string labCommand = null;
            if (!changeLab) {
                labCommand = GetRequiredLabString(fileName);
            }

            if (changeLab || labCommand == null) {
                PrintAvailableLabs();
                labCommand = View.ReadLabCommand($"{View.Nl}lab");
                View.PrintLine();
                View.PrintLine();
            }

            if (!(await CompileApp(filePath)).Equals(0))
                return;

            try {
                var arguments =
                    $"{labCommand} \"{Path.Combine(Model.CompiledPath, GetFileNameWithExeExtension(filePath))}\"";
                var exitCode = await AppRunner.StartConsoleApp(Model.AcTestPath, arguments);

                if (!new[] {0, 15}.Contains(exitCode)) {
                    throw new Exception("Tester program failed");
                }

                ChangeCurrentOrAppendLabString(fileName, labCommand);
            }
            catch (Exception e) {
                View.PrintError(e);
            }
        }

        private static async Task RunApp(string filePath) {
            filePath = Path.GetFileNameWithoutExtension(filePath);

            //Run created executable
            try {
                if (!File.Exists(Path.Combine(Model.CompiledPath, "mio.dll")) ||
                    !File.Exists(Path.Combine(Model.CompiledPath, "io.dll"))) {
                    throw new Exception("mio.dll and/or io.dll missing");
                }

                if (filePath != "" &&
                    File.Exists(Path.Combine(Model.CompiledPath, GetFileNameWithExeExtension(filePath)))) {
                    await AppRunner.StartConsoleApp(
                        Path.Combine(Model.CompiledPath, GetFileNameWithExeExtension(filePath)));
                }
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }
        }

        private static string GetLibraryArguments(string filePath) {
            var included = "";
            SearchFileForIncludes(filePath, ref included);
            var mainLibTypes = "";
            var libTypes = "";

            foreach (var word in included.Split(' ')) {
                switch (word) {
                    case "io.inc":
                        mainLibTypes += "-lio ";
                        break;
                    case "mio.inc":
                        mainLibTypes += "-lmio ";
                        break;
                    default: {
                        if (word.Contains('.')) {
                            libTypes += Path.GetFileNameWithoutExtension(word) + ".obj ";
                        }

                        break;
                    }
                }
            }

            libTypes += mainLibTypes;
            return !libTypes.Equals("") ? libTypes[..^1] : libTypes;
        }

        private static List<string> GetMissingImportantFiles() {
            return Model.ImportantFiles
                .Select(file => Path.Combine(Model.BinPath, file))
                .Where(filePath => !File.Exists(filePath) && !filePath.Equals(Model.AcTestPath))
                .ToList();
        }

        private static async Task CompileAndStartApp(string filePath) {
            if (filePath.Equals(string.Empty)) {
                View.PrintWarning($"There are no .asm files in \\projects or in the root directory{View.Nl}");
                View.PrintLine();
                return;
            }

            var missingFiles = GetMissingImportantFiles();
            if (!missingFiles.Count.Equals(0)) {
                var warning = $"The following files are missing : {View.Nl}";
                warning = missingFiles.Aggregate(
                    warning, (current, missingFile) => current + $"\t{missingFile}{View.Nl}"
                );
                View.PrintWarning(warning);
                return;
            }

            if ((await CompileApp(filePath)).Equals(0)) {
                await RunApp(filePath);
            }

            View.PrintLine();
        }

        private static async Task<int> CompileApp(string filePath) {
            var filesToDelete = new List<string>();
            int exitCode;
            var argumentLibType = GetLibraryArguments(filePath);

            Directory.SetCurrentDirectory(Model.BinPath);
            if ((exitCode = await AssembleApp(filePath, filesToDelete)).Equals(0)) {
                exitCode = await LinkApp(filePath, argumentLibType);
            }

            Directory.SetCurrentDirectory(Model.RootPath);

            if (exitCode.Equals(0)) {
                string exeBinPath = Path.Combine(Model.BinPath, GetFileNameWithExeExtension(filePath)),
                    exeCompiledPath = Path.Combine(Model.CompiledPath, GetFileNameWithExeExtension(filePath));
                if (File.Exists(exeBinPath)) {
                    if (File.Exists(exeCompiledPath)) {
                        File.Delete(exeCompiledPath);
                    }

                    File.Move(exeBinPath, exeCompiledPath);
                }
            }

            // Delete all created object files
            foreach (var file in filesToDelete.Where(File.Exists)) {
                File.Delete(file);
            }

            return exitCode;
        }

        private static async Task<int> LinkApp(string filePath, string argumentLibType) {
            try {
                // Run Nlink, it doesnt escape spaces within quotation marks in arguments
                return await AppRunner.StartConsoleApp(Model.NLinkPath,
                    $"{GetFileNameWithObjExtension(filePath)} {argumentLibType} " +
                    $"-o {GetFileNameWithExeExtension(filePath)}");
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }

            return -1;
        }

        private static async Task<int> AssembleApp(string filePath, ICollection<string> compiledObjects) {
            var exitCode = -1;

            try {
                var incFiles = string.Empty;
                SearchFileForIncludes(filePath, ref incFiles);
                if (!incFiles.Equals(string.Empty)) {
                    // compiles all include files
                    foreach (var incFile in incFiles.Split(' ')) {
                        if (incFile.Equals(string.Empty) || new[] {"mio.inc", "io.inc"}.Contains(incFile)) {
                            continue;
                        }

                        var incFilePath = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, incFile);
                        if (!File.Exists(incFilePath)) {
                            throw new Exception($"{incFilePath} missing");
                        }

                        var associatedAssemblyFile =
                            GetFileWithSpecificCaseInsensitiveExtension(incFilePath, "asm");
                        if (associatedAssemblyFile.Equals(string.Empty)) {
                            throw new Exception($"{incFile} associated assembly file missing");
                        }

                        await AssembleApp(associatedAssemblyFile, compiledObjects);
                    }
                }

                // Add new object file to the list for cleanup
                var objFullPath = GetFileBinPathWithObjExtension(filePath);
                if (!compiledObjects.Contains(objFullPath)) {
                    // If build failed throw exception
                    var args = $"-i{Model.GetProjectsPath()}\\ " +
                               $"-f win32 " +
                               $"-o \"{objFullPath}\" \"{filePath}\"";
                    if ((exitCode = await AppRunner.StartConsoleApp(Model.NasmPath, args)) != 0) {
                        throw new Exception($"{Path.GetFileName(filePath)} build failed");
                    }

                    compiledObjects.Add(objFullPath);
                }
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }

            return exitCode;
        }

        private static string ChooseFile() {
            var filePath = string.Empty;

            try {
                ChooseFileStart:

                View.PrintNewInstanceDecoration();

                var assemblyFileNames = new DirectoryInfo(Model.GetProjectsPath())
                    .GetFiles()
                    .Where(path => path.Extension.ToLower().Equals(".asm") &&
                                   !IsIncludeAssemblyFile(path.FullName))
                    .OrderByDescending(path => path.LastWriteTimeUtc)
                    .Select(path => path.FullName)
                    .ToList();

                if (assemblyFileNames.Count.Equals(0)) return string.Empty;

                View.PrintWhiteText($"Type index of .asm file you want to compile and execute :{View.Nl}");
                View.PrintOrderedListItem(assemblyFileNames.Select(Path.GetFileNameWithoutExtension).ToList());

                View.PrintInputText($"{View.Nl}index");
                var index = View.ReadIndexFromInput((uint) assemblyFileNames.Count);

                filePath = assemblyFileNames[index - 1];

                View.PrintLine();

                if (!File.Exists(filePath)) {
                    View.PrintLine();
                    View.PrintError($"{filePath} has been moved/renamed");
                    goto ChooseFileStart;
                }

                View.PrintLine();
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }

            return filePath;
        }

        private static void SearchFileForIncludes(string filePath, ref string includeFiles) {
            try {
                var asmFile = File.ReadLines(filePath)
                    .Where(line => line.Contains("%include"))
                    .Select(line => new {Line = line});

                foreach (var asmLine in asmFile) {
                    foreach (var fragment in asmLine.Line.Split(' ')) {
                        if (fragment.Equals("")) continue;
                        var toInc = fragment;
                        if (Equals(fragment[0], fragment[^1]) && Equals(fragment[0], '\'')) {
                            toInc = fragment[1..^1];
                        }

                        if (Path.GetExtension(toInc).ToLower().Equals(".inc")) {
                            if (!includeFiles.Contains(toInc)) {
                                includeFiles += toInc + " ";
                            }

                            if (toInc != "mio.inc" && toInc != "io.inc") {
                                SearchFileForIncludes(
                                    Path.Combine(Model.GetProjectsPath(), GetFileNameWithAsmExtension(toInc)),
                                    ref includeFiles);
                            }
                        }
                        else if (fragment.Contains(";")) {
                            break;
                        }
                    }
                }
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }
        }

        private static bool IsIncludeAssemblyFile(string filePath) {
            try {
                return !File.ReadLines(filePath).Any(line => line.Contains("global main"));
            }
            catch (Exception e) {
                View.PrintError(e);
            }

            return false;
        }

        private static IEnumerable<string> GetAllFilePathsWithinDir(string dirPath, int depth = 3) {
            var list = new List<string>();
            if (depth < 1) return list;
            foreach (var directory in Directory.EnumerateDirectories(dirPath)) {
                list.AddRange(GetAllFilePathsWithinDir(directory, depth - 1));
            }

            list.AddRange(Directory.GetFiles(dirPath));
            return list;
        }
    }
}