using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace UBB_NASM_Runner
{
    internal static class Presenter
    {
        private static readonly string[] ImportantFiles = {
            "actest.exe", "io.inc", "io.lib", "Kernel32.Lib",
            "ld.exe", "mio.inc", "mio.lib", "nasm.exe", "nlink.exe", "start.obj"
        };

        public static void Start() {
            View.SetTitle("NASM");
            var counter = 1;

            CleanUpDirectory();

            var filePath = ChooseFile();
            if (!filePath.Equals(string.Empty)) {
                View.SetTitle(Path.GetFileName(filePath));
                View.PrintNewInstanceDecoration(counter++, filePath);
                CompileAndRunApp(filePath);
            }

            var removeConsoleText = new View.RemoveConsoleText();
            while (true) {
                View.PrintPlainText();
                removeConsoleText.SaveCursorPosition();
                View.PrintControls();
                View.CursorVisibility(false);
                var command = Console.ReadKey(true);
                while (!IsAllowedCommand(command, filePath)) {
                    command = Console.ReadKey(true);
                }

                View.CursorVisibility(true);
                View.ScrollDown();

                if (command.Key.Equals(ConsoleKey.F)) {
                    //Select which file to compile
                    var oldFilePath = filePath;
                    removeConsoleText.RemoveUntilSavedCursorPosition();
                    filePath = ChooseFile();

                    View.SetTitle(Path.GetFileNameWithoutExtension(filePath));
                    View.MoveCursorUp(2);

                    if (!oldFilePath.Equals(filePath)) {
                        counter = 1;
                    }
                }
                else if (command.Key.Equals(ConsoleKey.T)) {
                    removeConsoleText.RemoveUntilSavedCursorPosition();
                    View.PrintNewInstanceDecoration(counter, filePath, filePath);
                    var changeLab = (command.Modifiers & ConsoleModifiers.Control) != 0;
                    AcTest(filePath, changeLab);

                    continue;
                }
                else if (command.Key.Equals(ConsoleKey.Q)) {
                    break;
                }

                removeConsoleText.RemoveUntilSavedCursorPosition();
                View.PrintNewInstanceDecoration(counter++, filePath);

                CompileAndRunApp(filePath);
            }
        }

        private static string GetFullFilePathWithObjExtension(string fPath) {
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

        private static void CleanUpDirectory() {
            try {
                var filePaths = GetAllFilePathsWithinDir(Model.RootPath);

                if (!Directory.Exists(Model.BinPath)) {
                    Directory.CreateDirectory(Model.BinPath);
                }

                if (!Directory.Exists(Model.ProjectsPath)) {
                    Directory.CreateDirectory(Model.ProjectsPath);
                }

                if (!Directory.Exists(Model.CompiledPath)) {
                    Directory.CreateDirectory(Model.CompiledPath);
                }

                string[] projectExtensions = {".asm", ".inc"};
                string[] ioMio = {"mio.dll", "io.dll"};
                foreach (var filePath in filePaths) {
                    var filePathExt = Path.GetExtension(filePath).ToLower();
                    var fPathDir = Path.GetDirectoryName(filePath);
                    if (ImportantFiles.Contains(Path.GetFileName(filePath))) {
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
                    else if (projectExtensions.Contains(filePathExt) && !Model.ProjectsPath.Equals(fPathDir)
                                                                     && !Model.BinPath.Equals(fPathDir)) {
                        // .asm and .inc will be placed in \projects
                        var destPath =
                            IndexNameIfDuplicate(Path.Combine(Model.ProjectsPath, Path.GetFileName(filePath)));
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

        private static void ChangeCurrentLabString(string fileName, string labCommand) {
            if (!File.Exists(Model.LabFilePath)) return;
            var fileLines = File.ReadLines(Model.LabFilePath).ToList();
            var index = 0;
            while (!Regex.IsMatch(fileLines[index], $@"^[\S]+ {Regex.Escape(fileName)}[\s]*$")) {
                index++;
            }

            fileLines[index] = $"{labCommand} {fileName}";

            File.WriteAllLines(Model.LabFilePath, fileLines);
        }

        private static string GetRequiredLabString(string fileName) {
            if (!File.Exists(Model.LabFilePath)) return string.Empty;
            var fileLines = File.ReadLines(Model.LabFilePath)
                .Where(line =>
                    Regex.IsMatch(line, $@"^[\S]+ {Regex.Escape(fileName)}[\s]*$"))
                .Select(line => new {Line = line}).ToList();
            return fileLines.First().Line.Split(' ').First();
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
                View.PrintPlainText(line);
                break;
            }
        }

        private static void AcTest(string filePath, bool changeLab) {
            var fileName = Path.GetFileName(filePath);
            var labCommand = GetRequiredLabString(fileName);
            var noRecordOfCurrentFile = labCommand.Equals(string.Empty);
            if (changeLab || noRecordOfCurrentFile) {
                var clearText = new View.RemoveConsoleText();
                clearText.SaveCursorPosition();
                PrintAvailableLabs();
                labCommand = View.ReadLabCommand($"{View.Nl}lab");
                if (noRecordOfCurrentFile) {
                    File.AppendAllText(Model.LabFilePath, $"{labCommand} {fileName}{View.Nl}");
                }
                else {
                    ChangeCurrentLabString(fileName, labCommand);
                }

                clearText.RemoveUntilSavedCursorPosition();
            }

            if (CompileApp(filePath) != 0) return;
            try {
                labCommand =
                    $"{labCommand} \\\"{Path.Combine(Model.CompiledPath, GetFileNameWithExeExtension(filePath))}\\\"";
                StartConsoleApp(Model.AcTestPath, labCommand, filePath);
            }
            catch (Exception e) {
                View.PrintError(e);
            }
        }

        private static void RunApp(string filePath) {
            filePath = Path.GetFileNameWithoutExtension(filePath);

            //Run created executable
            try {
                if (!File.Exists(Path.Combine(Model.CompiledPath, "mio.dll")) ||
                    !File.Exists(Path.Combine(Model.CompiledPath, "io.dll"))) {
                    throw new Exception("mio.dll and/or io.dll missing");
                }

                if (filePath != "" &&
                    File.Exists(Path.Combine(Model.CompiledPath, GetFileNameWithExeExtension(filePath)))) {
                    StartConsoleApp(Path.Combine(Model.CompiledPath, GetFileNameWithExeExtension(filePath)));
                }
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }
        }

        private static string GetLibraryArguments(string filePath) {
            var included = "";
            SearchFileForIncludes(filePath, ref included);
            var mainLibtypes = "";
            var libtypes = "";

            foreach (var word in included.Split(' ')) {
                if (word.Equals("io.inc")) {
                    mainLibtypes += "-lio ";
                }
                else if (word.Equals("mio.inc")) {
                    mainLibtypes += "-lmio ";
                }
                else {
                    if (word.Contains('.')) {
                        libtypes += Path.GetFileNameWithoutExtension(word) + ".obj ";
                    }
                }
            }

            libtypes += mainLibtypes;
            return !libtypes.Equals("") ? libtypes[..^1] : libtypes;
        }

        private static void CompileAndRunApp(string filePath) {
            if (CompileApp(filePath).Equals(0)) {
                RunApp(filePath);
            }
        }

        private static int CompileApp(string filePath) {
            var filesToDelete = new List<string>();
            int exitCode;
            var argumentLibtype = GetLibraryArguments(filePath);

            Directory.SetCurrentDirectory(Model.BinPath);
            if ((exitCode = AssembleApp(filePath, filesToDelete)) == 0) {
                exitCode = LinkApp(filePath, argumentLibtype);
            }

            Directory.SetCurrentDirectory(Model.RootPath);

            if (exitCode == 0) {
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

        private static int LinkApp(string filePath, string argumentLibtype) {
            try {
                // Run nlink, Nlink is such a piece of shit it doesnt escape spaces within quotation marks in arguments
                // that way project files must be copied to its directory
                return StartConsoleApp(Model.NLinkPath,
                    $"{GetFileNameWithObjExtension(filePath)} {argumentLibtype} -o {GetFileNameWithExeExtension(filePath)}");
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }

            return -1;
        }

        private static int AssembleApp(string filePath, ICollection<string> filesToDelete) {
            var exitCode = -1;

            try {
                // Copy main file into bin
                var fileBinPath = Path.Combine(Model.BinPath, Path.GetFileName(filePath));
                if (!filesToDelete.Contains(fileBinPath)) {
                    if (File.Exists(fileBinPath)) {
                        File.Delete(fileBinPath);
                    }

                    File.Copy(filePath, fileBinPath);
                    filesToDelete.Add(fileBinPath);
                }

                var incFiles = "";
                SearchFileForIncludes(filePath, ref incFiles); // includes seperated by spaces
                if (incFiles != "") {
                    // compiles all include files
                    foreach (var incFile in incFiles.Split(' ')) {
                        if (incFile.Equals(string.Empty) || new[] {"mio.inc", "io.inc"}.Contains(incFile)) {
                            continue;
                        }

                        var incFilePath = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, incFile);
                        if (!File.Exists(incFilePath)) {
                            throw new Exception($"{incFilePath} missing");
                        }

                        // Copy include file(s) into bin
                        var incFileBinPath = Path.Combine(Model.BinPath, incFile);
                        if (!filesToDelete.Contains(incFileBinPath)) {
                            if (File.Exists(incFileBinPath)) {
                                File.Delete(incFileBinPath);
                            }

                            File.Copy(incFilePath, incFileBinPath);
                            filesToDelete.Add(incFileBinPath);
                        }

                        var associatedAssemblyFile = GetFileWithSpecificCaseInsensitiveExtension(incFilePath, "asm");
                        if (associatedAssemblyFile.Equals("")) {
                            throw new Exception($"{incFile} associated assembly file missing");
                        }

                        AssembleApp(associatedAssemblyFile, filesToDelete);
                    }
                }

                //Directory.SetCurrentDirectory(Model.BinPath);
                // Add new object file to the list for cleanup
                var objfullpath = GetFullFilePathWithObjExtension(filePath);
                if (!filesToDelete.Contains(objfullpath)) {
                    // If build failed throw exception
                    if ((exitCode = StartConsoleApp(Model.NasmPath, $"-f win32 \"{fileBinPath}\"")) != 0) {
                        throw new Exception($"{Path.GetFileName(filePath)} build failed");
                    }

                    filesToDelete.Add(GetFullFilePathWithObjExtension(filePath));
                }

                //Directory.SetCurrentDirectory(Model.RootPath);
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }

            return exitCode;
        }

        // returns empty string if there are no files found that can be compiled
        private static string ChooseFile() {
            var filePath = string.Empty;

            try {
                var assemblyFileNames = new DirectoryInfo(Model.ProjectsPath)
                    .GetFiles()
                    .Where(path => path.Extension.ToLower().Equals(".asm") &&
                                   !IsIncludeAssemblyFile(path.FullName))
                    .OrderByDescending(path => path.LastWriteTimeUtc)
                    .Select(path => path.FullName)
                    .ToList();

                var clearText = new View.RemoveConsoleText();
                clearText.SaveCursorPosition();

                if (assemblyFileNames.Count.Equals(0)) {
                    View.PrintWarning(
                        $"There are no .asm files in \\projects. Nothing to compile and execute.{View.Nl}" +
                        "This program should be executed first in a directory where you " +
                        "have your projects and the bin files provided by the university, " +
                        "from where these will be moved to separate folders by the program.");
                    return string.Empty;
                }

                View.PrintPlainText($"Type index of .asm file you want to compile and execute :{View.Nl}");
                View.PrintOrderedListItem(assemblyFileNames.Select(
                    Path.GetFileNameWithoutExtension).ToList());

                View.PrintInputText($"{View.Nl}index");
                var inputError = new View.InputError();
                inputError.SavePosition();
                var index = View.ReadIndexFromInput();
                while (index < 1 || index > assemblyFileNames.Count) {
                    inputError.PrintInputError("Invalid input");
                    index = View.ReadIndexFromInput();
                    inputError.RemoveUntilSavedPosition();
                }

                filePath = assemblyFileNames[index - 1];

                clearText.RemoveUntilSavedCursorPosition();
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }

            return filePath;
        }

        private static Process GetProcessByFileName(string filename) {
            if (filename.Equals(string.Empty)) return null;

            var processes =
                Process.GetProcessesByName(Path.GetFileNameWithoutExtension(filename))
                    .OrderByDescending(p => p.StartTime).ToList();

            return !processes.Any() ? null : processes.First();
        }

        private static void PrintImportantPartOfOutput(string outputFile) {
            var lines = File.ReadLines(outputFile).ToList();
            const string str = "**********************";
            var row1 = 1;
            while (!lines[row1].Contains(str)) {
                row1++;
            }

            row1 += 2;
            var row2 = lines.Count - 2;
            while (!lines[row2].Contains(str)) {
                row2--;
            }

            var wait = 1;
            while (row1 != row2) {
                View.PrintPlainText(lines[row1++]);
                // Printing it out all fast for some reason messes with Windows Terminal, and will not display
                // most lines that it scrolled past by, slowing it down seemingly resolves this problem
                if (wait++ % (Console.WindowHeight - 2) == 0) {
                    Thread.Sleep(1);
                }
            }
        }

        private static int StartConsoleApp(string filename, string arguments = "", string testFile = "") {
            var process = new Process();
            var outputFile = Path.Combine(Model.BinPath, $"{DateTime.Now.ToFileTimeUtc()}.txt");
            while (File.Exists(outputFile)) {
                outputFile = Path.Combine(Model.BinPath, $"{DateTime.Now.ToFileTimeUtc()}.txt");
            }

            var isAcTest = filename.Equals(Model.AcTestPath);
            var printToSeparateConsole = !(isAcTest && arguments.Equals(string.Empty) ||
                                           new[] {Model.NLinkPath, Model.NasmPath}.Contains(filename));

            try {
                if (!printToSeparateConsole) {
                    process.StartInfo.FileName = filename;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                    process.WaitForExit();
                }
                else {
                    arguments = "-Command if ($?) {" +
                                $"Start-Transcript -Path \\\"{outputFile}\\\"; " +
                                "cls; " +
                                $".\\{Path.GetFileName(filename)} {arguments}; " +
                                "Stop-Transcript; }";
                    process = Process.Start(
                        new ProcessStartInfo(Model.PowerShellPath, arguments) {
                            UseShellExecute = true,
                            WorkingDirectory = isAcTest ? Model.BinPath : Model.CompiledPath
                        }
                    );

                    process?.WaitForExit();

                    if (process != null && process.ExitCode.Equals(0)) {
                        PrintImportantPartOfOutput(outputFile);
                    }
                    else {
                        View.PrintWarning("\tProgram was forcibly closed or crashed");
                    }

                    if (File.Exists(outputFile)) {
                        File.Delete(outputFile);
                    }

                    if (isAcTest) {
                        GetProcessByFileName(testFile)?.Kill();
                    }
                }
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }

            return process?.ExitCode ?? -1;
        }

        private static void SearchFileForIncludes(string filePath, ref string includeFiles) {
            try {
                var asmFile = File.ReadLines(filePath)
                    .Where(line => line.Contains("%include"))
                    .Select(line => new {Line = line});

                foreach (var asml in asmFile) {
                    foreach (var fragment in asml.Line.Split(' ')) {
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
                                    Path.Combine(Model.ProjectsPath, GetFileNameWithAsmExtension(toInc)),
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