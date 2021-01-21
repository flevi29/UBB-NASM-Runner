using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace UBB_NASM_Runner
{
    internal static class View
    {
        private const ConsoleColor Yellow = ConsoleColor.Yellow,
            Darkyellow = ConsoleColor.DarkYellow,
            Green = ConsoleColor.Green,
            Darkred = ConsoleColor.DarkRed,
            White = ConsoleColor.White,
            Black = ConsoleColor.Black,
            Blue = ConsoleColor.Blue,
            Gray = ConsoleColor.Gray,
            Magenta = ConsoleColor.Magenta,
            Cyan = ConsoleColor.Cyan;

        public static readonly string Nl = Environment.NewLine;
        //private static uint _lengthOfControls;

        private static readonly string[] ErrorMessages = {
            "She sells seashells by the seashore.",

            $"How much wood would a woodchuck chuck if a woodchuck could chuck wood?{Nl}" +
            $"He would chuck, he would, as much as he could, and chuck as much wood{Nl}" +
            "as a woodchuck would if a woodchuck could chuck wood.",

            $"If you must cross a course cross cow across a crowded cow crossing,{Nl}" +
            "cross the cross coarse cow across the crowded cow crossing carefully.",
            
            "Which witch switched the Swiss wristwatches?",
            
            $"To begin to toboggan first buy a toboggan, but don't buy too big a toboggan.{Nl}" +
            "Too big a toboggan is too big a toboggan to buy to begin to toboggan."
        };

        public static string GetRandomErrorMessage() {
            return ErrorMessages[new Random().Next(ErrorMessages.Length)];
        }

        public static void SetTitle(string title) {
            if (!title.Equals(string.Empty)) {
                Console.Title = title;
            }
        }

        public static ConsoleKeyInfo ReadKey() {
            return Console.ReadKey(true);
        }

        public static void ClearScreen() {
            Console.Clear();
            Console.WriteLine("\x1b[3J");
            Console.SetCursorPosition(0,0);
        }

        // public static void CursorVisibility(bool isVisible) {
        //     if (!Console.CursorVisible.Equals(isVisible)) {
        //         Console.CursorVisible = isVisible;
        //     }
        // }

        // public static void SetCursorLeftmost() {
        //     Console.SetCursorPosition(0, Console.CursorTop);
        // }
        //
        // public static void MoveCursorUp() {
        //     Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);
        // }
        //
        // public static void ScrollDown() {
        //     CursorVisibility(false);
        //     Console.Write(string.Concat(Enumerable.Repeat(Nl, Console.WindowHeight - 2)));
        //     Console.SetCursorPosition(0, Console.CursorTop - Console.WindowHeight + 2);
        //     CursorVisibility(true);
        // }

        public static int ReadIndexFromInput() {
            return int.TryParse(ReadFromInput(true), out var index) 
                ? index 
                : -1;
        }

        public static string ReadFromInput() {
            return ReadFromInput(false);
        }
        
        private static string ReadFromInput(bool isNumber) {
            ConsoleColors.SetAndPushDefaultColors();
            var input = "";
            char ch;
            var numberOfChars = 0;

            while (!(ch = Console.ReadKey(true).KeyChar).Equals(Nl[0])) {
                if (ch == '\b' && numberOfChars != 0) {
                    input = input.Remove(input.Length - 1);
                    numberOfChars--;
                    Console.Write("\b \b");
                }
                else if (numberOfChars != 10 && ch != '\b') {
                    if (isNumber && ch < '0' || ch > '9') {
                        continue;
                    }
                    Console.Write(ch);
                    input += ch;
                    numberOfChars++;
                }
            }
            
            ConsoleColors.SetPreviousAndPopColors();
            return input;
        }
        
        public static string ReadLabCommand(string inputText) {
            PrintInputText(inputText);
            var labCommand = ReadFromInput();
            return Regex.IsMatch(labCommand ?? string.Empty, @"^\s*$") ? string.Empty : labCommand;
        }

        public static void PrintControls() {
            const string line = " - ";
            const string tab = "    ";
            string[] newInstanceString = {
                "RETURN", "compile&execute",
                "Q", "exit",
                "F", "choose file",
                "Ctr-T/T", "ACtest"
            };

            //_lengthOfControls = 0;

            for (var i = 0; i < newInstanceString.Length; i++) {
                //_lengthOfControls += (uint)newInstanceString[i].Length;
                ConsoleColors.SetAndPushColors(Green);
                Console.Write(newInstanceString[i++]);
                ConsoleColors.SetPreviousAndPopColors();
                ConsoleColors.SetAndPushColors(Yellow);
                Console.Write(line);
                ConsoleColors.SetPreviousAndPopColors();
                //_lengthOfControls += (uint)newInstanceString[i].Length;
                ConsoleColors.SetAndPushColors(Cyan);
                Console.Write(newInstanceString[i]);
                if (!i.Equals(newInstanceString.Length - 1)) {
                    Console.Write(tab);
                }
                ConsoleColors.SetPreviousAndPopColors();
            }

            // _lengthOfControls += (uint) (newInstanceString.Length / 2 * line.Length +
            //                              (newInstanceString.Length / 2 - 1) * tab.Length);
        }

        public static void PrintAcTestDecoration() {
            PrintNewInstanceDecoration(0, "ACTest");
        }
        
        public static void PrintNewInstanceDecoration(uint counter, string middleText = "") {
            middleText = System.IO.Path.GetFileNameWithoutExtension(middleText);
            if (middleText.Length > 15) {
                middleText = middleText.Substring(0, 13) + "..";
            }
            string finalMidText;
            
            if (middleText.Equals("ACTest")) {
                finalMidText = $"<{middleText}>";
            }
            else if (counter != 0) {
                finalMidText = $" <{counter}> ";
                if (counter < 2 && !middleText.Equals(string.Empty)) {
                    finalMidText = $" {middleText}{finalMidText}";
                }
            }
            else {
                finalMidText = " +++ ";
            }

            //               -==: EXAMPLE <4> :==-
            finalMidText = $"-==:{finalMidText}:==-";

            var count = Console.WindowWidth / 2 - finalMidText.Length / 2;
            if (count > 0) {
                finalMidText = new string(' ', count) + finalMidText;
            }

            PrintLine(finalMidText, Blue);
            PrintLine();
        }

        private static class ConsoleColors
        {
            private static readonly ConsoleColor 
                PreBackground = Console.BackgroundColor,
                PreForeground = Console.ForegroundColor;
            private static Stack<Tuple<ConsoleColor, ConsoleColor>> _storedColors = new();

            public static void SetAndPushColors(ConsoleColor foreground) {
                SetAndPushColors(foreground, Console.BackgroundColor);
            }
            
            public static void SetAndPushColors(ConsoleColor foreground, ConsoleColor background) {
                _storedColors.Push(Tuple.Create(Console.ForegroundColor, Console.BackgroundColor));
                Console.ForegroundColor = foreground;
                Console.BackgroundColor = background;
            }

            public static void SetAndPushDefaultColors() {
                SetAndPushColors(PreForeground, PreBackground);
            }

            public static void SetPreviousAndPopColors(ushort times = 1) {
                while (times-- > 0) {
                    if (_storedColors.Count > 0) {
                        var (fore, back) = _storedColors.Pop();
                        Console.ForegroundColor = fore;
                        Console.BackgroundColor = back;
                    }
                    else {
                        Console.ForegroundColor = PreForeground;
                        Console.BackgroundColor = PreBackground;
                        break;
                    }
                }
            }
        }

        private static void Print(
            string text,
            bool printNewLine = false
        ) {
            Print(text, Console.ForegroundColor, Console.BackgroundColor, printNewLine);
        }
        
        private static void Print(
            string text,
            ConsoleColor foreground,
            bool printNewLine = false
        ) {
            Print(text, foreground, Console.BackgroundColor, printNewLine);
        }

        private static void Print(
            string text,
            ConsoleColor foreground,
            ConsoleColor background,
            bool printNewLine = false
        ) {
            if (printNewLine) {
                text += Nl;
            }
            var i = 0;
            var preString = "";
            while (i < text.Length && new[] {'\t', '\r', '\n', ' '}.Contains(text[i])) {
                preString += text[i++];
            }

            if (i > 0) {
                Console.Write(preString);
                text = text.Substring(i);
            }

            ConsoleColors.SetAndPushColors(foreground, background);
            Console.Write(text);
            ConsoleColors.SetPreviousAndPopColors();
        }

        public static void PrintWarning(string text) {
            Print($"    {text}", Darkyellow, true);
        }

        public static void PrintError(Exception exception) {
            var text = $"{Nl}{exception.GetType()} : {exception.Message}";
            Print(text, Darkred, true);
            OutputList.AddWithMaxCapacityToList(Tuple.Create(
                OutputList.OutputTypes.Error,
                text
                ));
        }
        
        public static void PrintError(string text) {
            Print(text, Darkred, true);
        }

        public static void PrintSpecialPlainText(string text) {
            Print(text, Black, Gray, true);
        }

        public static void PrintWhiteText(string text = "") {
            Print(text, White, true);
        }
        
        public static void PrintPlainOutputText(string text = "") {
            Print(text, Gray, true);
        }

        public static void PrintOrderedListItem(IEnumerable<string> listItems) {
            var i = 1;
            foreach (var item in listItems) {
                var spacing = i.ToString().Length < 3 ? "   ".Substring(0, 3 - i.ToString().Length + 1) : " ";

                ConsoleColors.SetAndPushColors(Green);
                Console.Write($" {i++}.{spacing}");
                ConsoleColors.SetPreviousAndPopColors();
                ConsoleColors.SetAndPushColors(Magenta);
                Console.WriteLine(item);
                ConsoleColors.SetPreviousAndPopColors();
            }
        }

        public static void PrintInputText(string text = "input") {
            Print($"{text} : ", Cyan);
        }

        public static void PrintLine(string text = "") {
            if (text.Equals(string.Empty)) {
                ConsoleColors.SetAndPushDefaultColors();
                Print(text, true);
                ConsoleColors.SetPreviousAndPopColors();
            }
            else {
                Print(text, Console.ForegroundColor, Console.BackgroundColor, true);
            }
        }
        
        public static void PrintLine(string text, ConsoleColor foreground) {
            Print(text, foreground, Console.BackgroundColor, true);
        }
        
        public static void PrintLine(string text, ConsoleColor foreground, ConsoleColor background) {
            Print(text, foreground, background, true);
        }
    }
}