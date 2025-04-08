using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace PythonToCSharp
{
    internal class ReversePolishNotation
    {
        public static Dictionary<string, string> lexemeTable = new Dictionary<string, string>
        { 
            // Операторы
            {"O1", "+"}, {"O2", "-"}, {"O3", "*"}, {"O4", "/"}, {"O5", "**"},
            {"O6", "<"}, {"O7", ">"}, {"O8", "=="}, {"O9", "!="}, {"O10", ">="}, {"O11", "<="},
            {"O12", "and"}, {"O13", "or"}, {"O14", "not"},

            // Разделители
            {"R1", " "}, {"R2", ","}, {"R3", "."}, {"R4", "("}, {"R5", ")"}, {"R6", ":"},
            {"R7", "["}, {"R8", "]"}, {"R9", "\t"},

            // Ключевые слова
            {"W1", "="}, {"W2", "if"}, {"W3", "elif"}, {"W4", "else"}, {"W5", "while"}, {"W6", "for"},
            {"W7", "None"}, {"W8", "True"}, {"W9", "False"},
            {"W10", "def"}, {"W11", "return"}, {"W12", "int"}, {"W13", "str"}, {"W14", "float"},
            {"W15", "print"}, {"W16", "input"},

            // Комментарии
            {"C1", "#"}
        };


        private static Dictionary<string, int> priorityTable = new Dictionary<string, int>
        {
            { "O5", 6 }, // **
            { "O3", 5 }, // *
            { "O4", 5 }, // /
            { "O1", 4 }, // + (binar)
            { "O2", 4 }, // - (binar)
            { "O6", 3 }, // <
            { "O7", 3 }, // >
            { "O8", 3 }, // ==
            { "O9", 3 }, // !=
            { "O10", 3 }, // >=
            { "O11", 3 }, // <=
            { "O14", 2 }, // not
            { "O12", 1 }, // and
            { "O13", 0 }, // or
            { "W1", -1 } // =
        };

        public static string ConvertToRPN(string inputCode)
        {
            var tokens = Tokenize(inputCode);
            var rpnTokens = ToRPN(tokens);
            return string.Join(" ", rpnTokens);
        }

        private static List<string> Tokenize(string code)
        {
            List<string> tokens = new List<string>();
            StringBuilder currentToken = new StringBuilder();
            int i = 0;

            while (i < code.Length)
            {
                char c = code[i];

                if (char.IsWhiteSpace(c))
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                    i++;
                    continue;
                }

                if (IsSpecialCharacter(c))
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }

                    // Проверка двухсимвольных операторов
                    if (i + 1 < code.Length)
                    {
                        string potentialOperator = c.ToString() + code[i + 1];
                        if (lexemeTable.ContainsKey(potentialOperator))
                        {
                            tokens.Add(potentialOperator);
                            i += 2;
                            continue;
                        }
                    }

                    tokens.Add(c.ToString());
                    i++;
                }
                else if (char.IsLetterOrDigit(c) || c == '.')
                {
                    currentToken.Append(c);
                    i++;
                }
                else
                {
                    i++;
                }
            }

            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            return tokens;
        }

        private static bool IsSpecialCharacter(char c)
        {
            return c == '+' || c == '-' || c == '*' || c == '/' || c == '<' ||
                   c == '>' || c == '=' || c == '!' || c == '(' || c == ')' ||
                   c == ':' || c == '[' || c == ']' || c == ',' || c == '.' ||
                   c == '{' || c == '}' || c == '#';
        }

        private static bool IsOperand(string token)
        {
            // Операнд - если токена нет в таблице лексем и это не ключевое слово
            return !priorityTable.ContainsKey(token);
        }

        private static List<string> ToRPN(List<string> tokens)
        {
            Stack<string> stack = new Stack<string>();
            List<string> output = new List<string>();

            foreach (string token in tokens)
            {
                if (IsOperand(token))
                {
                    output.Add(token);
                }
                else if (token == "R4") // (
                {
                    stack.Push(token);
                }
                else if (token == "R5") // )
                {
                    // Выталкиваем все операторы до открывающей скобки
                    while (stack.Count > 0 && stack.Peek() != "R4")
                    {
                        output.Add(stack.Pop());
                    }
                    stack.Pop(); // Удаляем "(" из стека
                }
                else if (lexemeTable.ContainsKey(token))
                {
                    string lexemeCode = lexemeTable[token];

                    if (priorityTable.ContainsKey(lexemeCode))
                    {
                        // Выталкиваем операторы с более высоким или равным приоритетом
                        while (stack.Count > 0 && stack.Peek() != "R4" &&
                               lexemeTable.ContainsKey(stack.Peek()) &&
                               priorityTable.ContainsKey(lexemeTable[stack.Peek()]) &&
                               priorityTable[lexemeTable[stack.Peek()]] >= priorityTable[lexemeCode])
                        {
                            output.Add(stack.Pop());
                        }
                    }
                    stack.Push(token);
                }
            }

            // Выталкиваем оставшиеся операторы
            while (stack.Count > 0)
            {
                if (stack.Peek() != "R4")
                {
                    output.Add(stack.Pop());
                }
                else
                {
                    stack.Pop(); // Удаляем оставшиеся "("
                }
            }

            return output;
        }
    }
}
