using System;
using System.Collections.Generic;
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
            Gray = ConsoleColor.Gray,
            Magenta = ConsoleColor.Magenta,
            Cyan = ConsoleColor.Cyan;

        public static readonly string Nl = Environment.NewLine;
        
        private static readonly string[] ErrorMessages = {
            $"She sells seashells by the seashore.",

            $"How much wood would a woodchuck chuck if a woodchuck could chuck wood?{View.Nl}" +
            $"He would chuck, he would, as much as he could, and chuck as much wood{View.Nl}" +
            $"As a woodchuck would if a woodchuck could chuck wood.",

            $"If you must cross a course cross cow across a crowded cow crossing,{View.Nl}" +
            $"cross the cross coarse cow across the crowded cow crossing carefully.",
            
            "Which witch switched the Swiss wristwatches?",
            
            $"To begin to toboggan first buy a toboggan, but don't buy too big a toboggan.{View.Nl}" +
            "Too big a toboggan is too big a toboggan to buy to begin to toboggan."
        };

        private static string GetRandomErrorMessage() {
            return ErrorMessages[new Random().Next(ErrorMessages.Length)];
        }

        public static void SetTitle(string title) {
            Console.Title = title;
        }

        public static void MoveCursorUp(int howManyTimes = 1) {
            if (Console.CursorTop - howManyTimes < 0) return;
            while (howManyTimes-- > 0) {
                Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);
            }
        }

        public static void ClearScreen() {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
        }

        public static void CursorVisibility(bool isVisible) {
            Console.CursorVisible = isVisible;
        }

        public static int ReadIndexFromInput() {
            var input = "";
            char ch;
            var number = 0;

            while (!(ch = Console.ReadKey(true).KeyChar).Equals(Nl[0])) {
                if (ch == '\b' && number != 0) {
                    input = input.Remove(input.Length - 1);
                    number--;
                    Console.Write("\b \b");
                }
                else if (ch != '\b' && number != 7) {
                    Console.Write(ch);
                    input += ch;
                    number++;
                }
            }

            return int.TryParse(input, out number) ? number : -1;
        }
        
        public static string ReadLabCommand(string inputText) {
            PrintInputText(inputText);
            var inputError = new InputError();
            inputError.SavePosition();
            var labCommand = Console.ReadLine();
            while (Regex.IsMatch(labCommand ?? string.Empty, @"^\s*$")) {
                inputError.PrintInputError(GetRandomErrorMessage());
                labCommand = Console.ReadLine();
                inputError.RemoveUntilSavedPosition();
            }

            return labCommand;
        }

        public static void PrintControls() {
            const string line = " - ";
            const string tab = "    ";
            string[] newInstanceString = {
                "RETURN", "compile&execute",
                "Q", "exit",
                "F", "choose file",
                "^T/T", "ACtest"
            };

            for (var i = 0; i < newInstanceString.Length; i++) {
                Console.ForegroundColor = Green;
                Console.Write(newInstanceString[i++]);
                Console.ForegroundColor = Yellow;
                Console.Write(line);
                Console.ForegroundColor = Cyan;
                Console.Write(newInstanceString[i] + tab);
            }

            Console.ResetColor();
            Console.WriteLine();
        }

        private static void RemovePreviousLine(int numberOfLines = 1) {
            if (numberOfLines < 0) {
                //throw new ArgumentOutOfRangeException($"numberOfLines must be greater than or equal to 0");
                return;
            }

            if (Console.CursorTop.Equals(0)) {
                return;
            }

            Console.SetCursorPosition(0, Console.CursorTop);
            while (numberOfLines-- > 0) {
                if (Console.CursorTop.Equals(0)) {
                    break;
                }

                // Some unexplainable f***** up s*** happens here too
                // sometimes the cursor goes new line sometimes it doesn't, even though
                // we print from the same position the same length of string
                // so I have to save cursor top because of this inconsistency
                var cursorTop = Console.CursorTop - 1;
                Console.Write(new string(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, cursorTop);
            }
        }

        public static void ScrollDown() {
            Console.Write(string.Concat(Enumerable.Repeat(Nl, Console.WindowHeight - 2)));
            Console.SetCursorPosition(0, Console.CursorTop - Console.WindowHeight + 2);
        }

        public static void PrintNewInstanceDecoration(int counter, string middleText = "", bool actest = false) {
            middleText = System.IO.Path.GetFileNameWithoutExtension(middleText);
            var counterStr = string.Empty;
            if (actest) {
                counterStr = $" [ ACTest - {middleText} ] ";
            }
            else if (!middleText.Equals(string.Empty)) {
                counterStr = $" [ {counter} ] ";
                if (counter < 2) {
                    counterStr = $" < {middleText} >{counterStr}";
                }
            }

            var decorativeStr = new char[Console.BufferWidth];
            var decorativeStrMiddle = (Console.BufferWidth - counterStr.Length) / 2;
            for (var i = 0; i < Console.BufferWidth; i++) {
                if (i != decorativeStrMiddle) {
                    decorativeStr[i] = '-';
                }
                else if (!counterStr.Equals(string.Empty)) {
                    foreach (var ch in counterStr) {
                        decorativeStr[i++] = ch;
                    }

                    i--;
                }
            }

            Console.CursorLeft = 0;
            PrintSpecialPlainText(new string(decorativeStr));
            Console.WriteLine();
        }

        public class ConsoleTextRemover
        {
            private int _cursorTop;
            private int _cursorLeft;

            public ConsoleTextRemover() {
                _cursorTop = Console.CursorTop;
                _cursorLeft = Console.CursorLeft;
            }
            
            public void SaveCursorPosition() {
                _cursorTop = Console.CursorTop;
                _cursorLeft = Console.CursorLeft;
            }

            public void RemoveUntilSavedCursorPosition() {
                var secondCursorTop = Console.CursorTop;
                if (secondCursorTop < _cursorTop) {
                    //throw new InvalidOperationException("cursor top is behind the starting point");
                    // maybe it should just return; but that might ignore potential problematic behaviour
                    return;
                }

                var numOfLines = Console.CursorTop - _cursorTop;
                Console.CursorVisible = false;
                if (numOfLines > 1) {
                    RemovalAtBottom();
                    RemovePreviousLine(numOfLines - 2);
                }
                RemovalAtTop();
                Console.CursorVisible = true;
            }

            private void RemovalAtTop() {
                Console.SetCursorPosition(_cursorLeft, _cursorTop);
                Console.Write(new string(' ', Console.BufferWidth - _cursorLeft));
                Console.SetCursorPosition(_cursorLeft, _cursorTop);
            }

            private void RemovalAtBottom() {
                var cursorLeft = Console.CursorLeft;
                var cursorTop = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', cursorLeft));
                Console.SetCursorPosition(0, cursorTop - 1);
            }

            public int GetCursorTop() {
                return _cursorTop;
            }

            public int GetCursorLeft() {
                return _cursorLeft;
            }
        }

        public class InputError
        {
            private readonly ConsoleTextRemover _consoleTextRemover;
            private int _warningCursorTop, _warningCursorLeft;
            private bool _isTextWritten;

            public InputError() {
                _consoleTextRemover = new ConsoleTextRemover();
            }

            public void SavePosition() {
                _consoleTextRemover.SaveCursorPosition();
            }

            public void PrintInputError(string text) {
                _consoleTextRemover.RemoveUntilSavedCursorPosition();
                PrintWarning($"{Nl}{Nl}\t{text}");
                _warningCursorLeft = Console.CursorLeft;
                _warningCursorTop = Console.CursorTop;
                Console.SetCursorPosition(_consoleTextRemover.GetCursorLeft(), _consoleTextRemover.GetCursorTop());
                _isTextWritten = true;
            }

            public void RemoveUntilSavedPosition() {
                if (!_isTextWritten) {
                    throw new InvalidOperationException("no text has been written yet");
                }

                Console.SetCursorPosition(_warningCursorLeft, _warningCursorTop);
                _consoleTextRemover.RemoveUntilSavedCursorPosition();
                _isTextWritten = false;
            }
        }

        private static class ConsoleColors
        {
            private static ConsoleColor _preBackground, _preForeground;
            private static bool _isSet;

            public static void SetColors(ConsoleColor foreground, ConsoleColor background = ConsoleColor.Black) {
                if (!_isSet) {
                    _preBackground = Console.BackgroundColor;
                    _preForeground = Console.ForegroundColor;
                }

                Console.BackgroundColor = background;
                Console.ForegroundColor = foreground;
                _isSet = true;
            }

            public static void ResetColors() {
                if (!_isSet) {
                    throw new InvalidOperationException("no colors were set before");
                }

                Console.BackgroundColor = _preBackground;
                Console.ForegroundColor = _preForeground;
                _isSet = false;
            }
        }

        private static void Print(
            string text,
            bool printNewLine = false,
            ConsoleColor foreground = ConsoleColor.Gray,
            ConsoleColor background = ConsoleColor.Black
        ) {
            if (printNewLine) {
                text += Nl;
            }

            Print(text, foreground, background);
        }

        private static void Print(
            string text,
            ConsoleColor foreground = ConsoleColor.Gray,
            ConsoleColor background = ConsoleColor.Black
        ) {
            var i = 0;
            var preString = "";
            while (i < text.Length && new[] {'\t', '\r', '\n', ' '}.Contains(text[i])) {
                preString += text[i++];
            }

            if (i > 0) {
                Console.Write(preString);
                text = text.Substring(i);
            }

            ConsoleColors.SetColors(foreground, background);
            Console.Write(text);
            ConsoleColors.ResetColors();
        }

        public static void PrintWarning(string text) {
            Print(text, true, Darkyellow);
        }

        public static void PrintError(Exception exception) {
            Print($"{Nl}{exception.GetType()} : {exception.Message}", true, Darkred);
        }

        public static void PrintSpecialPlainText(string text) {
            Print(text, true, Black, Gray);
        }

        public static void PrintPlainText(string text = "") {
            Print(text, true, White);
        }

        public static void PrintOrderedListItem(IEnumerable<string> listItems) {
            var i = 1;
            foreach (var item in listItems) {
                var spacing = i.ToString().Length < 3 ? "   ".Substring(0, 3 - i.ToString().Length + 1) : " ";

                ConsoleColors.SetColors(Green);
                Console.Write($" {i++}.{spacing}");
                ConsoleColors.SetColors(Magenta);
                Console.WriteLine(item);
            }

            ConsoleColors.ResetColors();
        }

        public static void PrintInputText(string text = "input") {
            Print($"{text} : ", Cyan);
        }
    }
}