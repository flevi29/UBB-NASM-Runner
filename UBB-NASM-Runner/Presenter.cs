using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
                SwitchStart:
                switch (command.KeyChar) {
                        case 'f' :
                            //Select which file to compile
                            var oldFilePath = filePath;
                            filePath = ChooseFile();
                    
                            if (!filePath.Equals(string.Empty)) {
                                View.SetTitle(Path.GetFileNameWithoutExtension(filePath));
                                if (!oldFilePath.Equals(filePath)) {
                                    counter = 1;
                                }
                            }
                            break;
                        case 't' :
                            PrintDecoration();
                            var changeLab = (command.Modifiers & ConsoleModifiers.Control) != 0;
                            await AcTest(filePath, changeLab);
                            
                            goto SwitchStart;
                        case 'q' :
                            goto EndOfWhile;
                }
                
                PrintDecoration(counter++, filePath);

                await CompileAndStartApp(filePath);
                
                PrintControlsAndReadAllowedKey(filePath, out command);
            }
            EndOfWhile: ;
        }
        
        private static void PrintControlsAndReadAllowedKey(string filePath, out ConsoleKeyInfo command) {
            View.PrintLine();
            View.PrintControls();
            //View.CursorVisibility(false);
            do {
                command = View.ReadKey();
            } while (!IsAllowedCommand(command, filePath)) ;

            //View.CursorVisibility(true);
        }

        private static void PrintDecoration() {
            View.PrintAcTestDecoration();
            OutputList.AddWithMaxCapacityToList(Tuple.Create(
                OutputList.OutputTypes.NewInstance,
                "ACTest"
            ));
        }
        
        private static void PrintDecoration(uint counter, string filePath) {
            View.PrintNewInstanceDecoration(counter, filePath);
            OutputList.AddWithMaxCapacityToList(Tuple.Create(
                OutputList.OutputTypes.NewInstance,
                $"{counter} {Path.GetFileNameWithoutExtension(filePath)}"
            ));
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
                ? command.KeyChar.Equals('q') || command.KeyChar.Equals('f')
                : command.KeyChar.Equals('\r') || command.KeyChar.Equals('q')
                                                           || command.KeyChar.Equals('f')
                                                           || command.KeyChar.Equals('t');
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
            return fileLines.Count.Equals(0) 
                ? string.Empty 
                : fileLines.First().Line.Split(' ').First();
        }

        // TODO: Use AppRunner.StartConsoleApp, so we don't have duplicate code here
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
            if (!File.Exists(Model.AcTestPath)) {
                View.PrintWarning("Tester program not found");
                return;
            }
            var fileName = Path.GetFileName(filePath);
            var labCommand = GetRequiredLabString(fileName);
            var noRecordOfCurrentFile = labCommand.Equals(string.Empty);
            if (changeLab || noRecordOfCurrentFile) {
                //var clearText = new View.ConsoleTextRemover();
                //clearText.SaveCursorPosition();
                PrintAvailableLabs();
                labCommand = View.ReadLabCommand($"{View.Nl}lab");
                if (noRecordOfCurrentFile) {
                    await File.AppendAllTextAsync(Model.LabFilePath, $"{labCommand} {fileName}{View.Nl}");
                }
                else {
                    ChangeCurrentLabString(fileName, labCommand);
                }

                //clearText.RemoveUntilSavedCursorPosition();
            }

            if (!(await CompileApp(filePath)).Equals(0)) return;
            try {
                var arguments =
                    $"{labCommand} '{Path.Combine(Model.CompiledPath, GetFileNameWithExeExtension(filePath))}'";
                var (tupleExitCode, tupleString) = 
                    await AppRunner.StartConsoleApp(Model.AcTestPath, arguments, false);
                
                OutputList.AddWithMaxCapacityToList(Tuple.Create(
                    tupleExitCode.Equals(0) ? OutputList.OutputTypes.AppOutput : OutputList.OutputTypes.Error,
                    tupleString
                    ));
                
                // TODO: check if killing stuck tester kills app that was being tested too
                // if not, then check if it's alive and kill it via GetProcessByFileName()
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
                    var (_, tupleString) = await AppRunner.StartConsoleApp(
                        Path.Combine(Model.CompiledPath, GetFileNameWithExeExtension(filePath)));
                    
                    OutputList.AddWithMaxCapacityToList(Tuple.Create(
                        OutputList.OutputTypes.AppOutput,
                        tupleString
                    ));
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
            var argumentLibtype = GetLibraryArguments(filePath);

            Directory.SetCurrentDirectory(Model.BinPath);
            if ((exitCode = await AssembleApp(filePath, filesToDelete)) == 0) {
                exitCode = await LinkApp(filePath, argumentLibtype);
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

        private static async Task<int> LinkApp(string filePath, string argumentLibtype) {
            try {
                // Run nlink, Nlink is such a piece of shit it doesnt escape spaces within quotation marks in arguments
                // that way project files must be copied to its directory
                var (tupleExitCode, tupleString) = await AppRunner.StartConsoleApp(Model.NLinkPath,
                    $"{GetFileNameWithObjExtension(filePath)} {argumentLibtype} " +
                    $"-o {GetFileNameWithExeExtension(filePath)}");

                if (!tupleExitCode.Equals(0)) {
                    OutputList.AddWithMaxCapacityToList(Tuple.Create(
                        OutputList.OutputTypes.Error,
                        tupleString
                    ));
                }
                
                return tupleExitCode;
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }

            return -1;
        }

        private static async Task<int> AssembleApp(string filePath, ICollection<string> filesToDelete) {
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

                        await AssembleApp(associatedAssemblyFile, filesToDelete);
                    }
                }

                //Directory.SetCurrentDirectory(Model.BinPath);
                // Add new object file to the list for cleanup
                var objfullpath = GetFullFilePathWithObjExtension(filePath);
                if (!filesToDelete.Contains(objfullpath)) {
                    // If build failed throw exception
                    // TODO: make a string list you store this tupleString into and all the rest
                    var (tupleExitCode, tupleString) = 
                        await AppRunner.StartConsoleApp(Model.NasmPath, $"-f win32 '{fileBinPath}'");

                    if (!tupleExitCode.Equals(0)) {
                        OutputList.AddWithMaxCapacityToList(Tuple.Create(
                            OutputList.OutputTypes.Error,
                            tupleString
                        ));
                    }

                    if ((exitCode = tupleExitCode) != 0) {
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
            var errorMsg = string.Empty;

            try {
                ChooseFileStart:
                //View.ClearScreen();

                var assemblyFileNames = new DirectoryInfo(Model.ProjectsPath)
                    .GetFiles()
                    .Where(path => path.Extension.ToLower().Equals(".asm") &&
                                   !IsIncludeAssemblyFile(path.FullName))
                    .OrderByDescending(path => path.LastWriteTimeUtc)
                    .Select(path => path.FullName)
                    .ToList();

                if (assemblyFileNames.Count.Equals(0)) return string.Empty;

                View.PrintWhiteText($"Type index of .asm file you want to compile and execute :{View.Nl}");
                View.PrintOrderedListItem(assemblyFileNames.Select(Path.GetFileNameWithoutExtension).ToList());

                if (!errorMsg.Equals(string.Empty)) {
                    View.PrintLine();
                    View.PrintWarning(errorMsg);
                }
                
                View.PrintInputText($"{View.Nl}index");
                var index = View.ReadIndexFromInput();
                if (index < 1 || index > assemblyFileNames.Count) {
                    errorMsg = View.GetRandomErrorMessage();
                    goto ChooseFileStart;
                }

                filePath = assemblyFileNames[index - 1];
                
            }
            catch (Exception exception) {
                View.PrintError(exception);
            }
            
            //View.ClearScreen();

            return filePath;
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