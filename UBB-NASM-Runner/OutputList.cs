using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace UBB_NASM_Runner
{
    public static class OutputList
    {
        public enum OutputTypes
        {
            Warning = 1,
            NewInstance = 2,
            AppOutput = 3,
            Error = 4
        }

        private static uint _numberOfInstances;
        private const uint MaxInstances = 5;
        private static readonly string OutPutFile = "logs.dat";
        private static readonly List<Tuple<OutputTypes, string>> OutPutList = new();
        
        static OutputList() {
            var appdata = Environment.ExpandEnvironmentVariables("%appdata%");
            var appdataNasm = Path.Combine(appdata, "NASM-RUNNER");
            if (Directory.Exists(appdata)) {
                if (!Directory.Exists(appdataNasm)) {
                    Directory.CreateDirectory(appdataNasm);
                }
                
                OutPutFile = Path.Combine(appdataNasm, OutPutFile);
            }
            else {
                OutPutFile = Path.Combine(Model.RootPath, OutPutFile); 
            }
            FillListWithLoggedContent();
        }

        private static OutputTypes GetStringToOutputTypes(string str) {
            return str switch {
                "AppOutput" => OutputTypes.AppOutput,
                "NewInstance" => OutputTypes.NewInstance,
                "Error" => OutputTypes.Error,
                "Warning" => OutputTypes.Warning,
                _ => throw new ArgumentOutOfRangeException(nameof(str), str, "nonexistent type")
            };
        }

        private static void FillListWithLoggedContent() {
            if (!File.Exists(OutPutFile)) return;
            var lines = File.ReadAllLines(OutPutFile);
            var i = 0;
            while (i < lines.Length) {
                var match = Regex.Match(lines[i],
                    $"^({OutputTypes.Error}|{OutputTypes.Warning}|" +
                    $"{OutputTypes.AppOutput}|{OutputTypes.NewInstance}) (\\d+)$");
                
                if (!match.Success) {
                    i++;
                    continue;
                }

                var type = GetStringToOutputTypes(match.Groups[1].ToString());
                var numOfLines = int.Parse(match.Groups[2].ToString()) + ++i;
                var contents = string.Empty;
                while (i < numOfLines && i < lines.Length) {
                    contents += lines[i++];
                    if (i < numOfLines) {
                        contents += View.Nl;
                    }
                }

                AddWithMaxCapacityToList(Tuple.Create(type, contents));
            }
        }
        
        private static void RemoveFirstInstanceFromLogs() {
            if (!File.Exists(OutPutFile)) return;
            var lines = File.ReadAllLines(OutPutFile);
            var i = 0;
            var newInstancesFound = 0;
            do {
                var match = Regex.Match(lines[i],
                    $"^({OutputTypes.Error}|{OutputTypes.Warning}|" +
                    $"{OutputTypes.AppOutput}|{OutputTypes.NewInstance}) (\\d+)$");
                if (!match.Success) {
                    i++;
                    continue;
                }

                var type = GetStringToOutputTypes(match.Groups[1].ToString());
                if (type.Equals(OutputTypes.NewInstance)) {
                    newInstancesFound++;
                    if (newInstancesFound > 1) {
                        break;
                    }
                }
                var numOfLines = int.Parse(match.Groups[2].ToString());
                i += numOfLines + 1;

            } while (i < lines.Length);

            File.WriteAllLines(OutPutFile, lines.Skip(i).ToArray());
        }
        
        private static void WriteToLog(Tuple<OutputTypes, string> outPut) {
            var (outputType, content) = outPut;
            var numberOfLines = content.Count(c => c.Equals('\n')) + 1;
            File.AppendAllText(OutPutFile, 
                $"{outputType} {numberOfLines}{View.Nl}{content}{View.Nl}");
        }

        private static void RemoveFirstInstanceFromList() {
            if (!OutPutList.Any()) return;
            OutPutList.RemoveAt(0);
            while (OutPutList.Any() && 
                   !OutPutList[0].Item1.Equals(OutputTypes.NewInstance)) {
                OutPutList.RemoveAt(0);
            }
            OutPutList.RemoveAt(0);
        }
        
        public static void AddWithMaxCapacityToList(Tuple<OutputTypes, string> outPut) {
            if (outPut.Item1.Equals(OutputTypes.NewInstance)) {
                if (_numberOfInstances >= MaxInstances) {
                    RemoveFirstInstanceFromList();
                    RemoveFirstInstanceFromLogs();
                }
                else {
                    _numberOfInstances++;
                }
            }

            OutPutList.Add(outPut);
            WriteToLog(outPut);
        }

        public static void PrintList() {
            foreach (var (type, content) in OutPutList) {
                switch (type) {
                    case OutputTypes.AppOutput :
                        View.PrintPlainOutputText(content);
                        break;
                    case OutputTypes.NewInstance :
                        var contentSplit = content.Split(' ');
                        var counter = uint.Parse(contentSplit[0]);
                        var title = string.Join(' ', contentSplit.Skip(1));
                        View.PrintNewInstanceDecoration(counter, title);
                        break;
                    case OutputTypes.Error :
                        View.PrintError(content);
                        break;
                    case OutputTypes.Warning :
                        // questionable
                        View.PrintWarning(content);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}