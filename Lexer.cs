using System.ComponentModel;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

namespace PythonToCSharp
{
    // TODO: add \r to lexer as R10
    public class Lexer
    {
        public static Dictionary<string, string> lexemeTable = new Dictionary<string, string>
        {
            // Операторы
            {"+", "O1"}, {"-", "O2"}, {"*", "O3"}, {"/", "O4"}, {"**", "O5"},
            {"<", "O6"}, {">", "O7"}, {"==", "O8"}, {"!=", "O9"}, {">=", "O10"}, {"<=", "O11"},
            {"and", "O12"}, {"or", "O13"}, {"not", "O14"}, {"%", "O15" }, {"//", "O16"},

            // Разделители
            {" ", "R1"}, {",", "R2"}, {".", "R3"}, {"(", "R4"}, {")", "R5"}, {":", "R6"}, {"[", "R7"}, {"]", "R8"}, {"\t", "R9"},

            // Ключевые слова
            {"=", "W1"}, {"if", "W2"}, {"elif", "W3"}, {"else", "W4"}, {"while", "W5"}, {"for", "W6"},
            {"None", "W7"}, {"True", "W8"}, {"False", "W9"},
            {"def", "W10"}, {"return", "W11"}, {"int", "W12"}, {"str", "W13"}, {"float", "W14"},
            {"print", "W15"}, {"input", "W16"},

            // Комментарии
            {"#", "C1"}
        };

        private Dictionary<string, string> identifiers = new Dictionary<string, string>();
        private Dictionary<string, string> numbers = new Dictionary<string, string>();
        private Dictionary<string, string> strings = new Dictionary<string, string>();

        private int idCount = 0;
        private int numCount = 0;
        private int strCount = 0;

        public string ProcessText(string text)
        {
            var result = new List<string>();
            foreach (var line in text.Split('\n'))
            {
                var translatedLine = ProcessLine(line);
                result.Add(translatedLine);
            }
            return string.Join("\n", result);
        }

        private string ProcessLine(string line)
        {
            var state = "default";
            var buffer = new StringBuilder();
            var translated = new List<string>();

            for (int i = 0; i < line.Length; i++)
            {
                char currentChar = line[i];
                switch (state)
                {
                    case "default":
                        if (char.IsWhiteSpace(currentChar))
                        {
                            bool tab = true;
                            for (int j = i + 1; j <= i + 3; j++)
                            {
                                if (j >= line.Length || line[j] != ' ')
                                    tab = false;
                                break;
                            }
                            if (tab)
                            {
                                translated.Add("R9");
                                i += 3;
                            }
                            continue;
                        }
                        // Обработка комментариев Python (#)
                        else if (currentChar == '#')
                        {
                            i = line.Length;
                        }
                        else if (currentChar == '=' && i + 1 < line.Length && line[i + 1] == '=')
                        {
                            translated.Add("O8");
                            i += 1;
                        }
                        // Обработка оператора присваивания :=
                        else if (currentChar == ':' && i + 1 < line.Length && line[i + 1] == '=')
                        {
                            translated.Add("W1");
                            i++;
                        }
                        // Обработка многострочных строк (начало)
                        else if ((currentChar == '\'' && i + 2 < line.Length && line[i + 1] == '\'' && line[i + 2] == '\'') ||
                                 (currentChar == '"' && i + 2 < line.Length && line[i + 1] == '"' && line[i + 2] == '"'))
                        {
                            state = "multiline_string";
                            buffer.Append(currentChar);
                            buffer.Append(line[++i]); // Второй символ
                            buffer.Append(line[++i]); // Третий символ
                        }
                        else if (lexemeTable.ContainsKey(currentChar.ToString()))
                        {
                            translated.Add(lexemeTable[currentChar.ToString()]);
                        }
                        else if (char.IsDigit(currentChar))
                        {
                            state = "number";
                            buffer.Append(currentChar);
                        }
                        else if (char.IsLetter(currentChar) || currentChar == '_')
                        {
                            state = "identifier";
                            buffer.Append(currentChar);
                        }
                        else if (currentChar == '\'' || currentChar == '"')
                        {
                            state = "string";
                            buffer.Append(currentChar);
                        }
                        else
                        {
                            throw new Exception($"Unknown character: {currentChar}");
                        }
                        break;

                    case "number":
                        if (char.IsDigit(currentChar) || currentChar == '.')
                        {
                            buffer.Append(currentChar);
                        }
                        else
                        {
                            var numStr = buffer.ToString();
                            if (!numbers.ContainsKey(numStr))
                            {
                                numCount++;
                                numbers[numStr] = $"N{numCount}";
                            }
                            translated.Add(numbers[numStr]);
                            buffer.Clear();
                            state = "default";
                            i--; // Повторно обработаем текущий символ
                        }
                        break;

                    case "identifier":
                        if (char.IsLetterOrDigit(currentChar) || currentChar == '_')
                        {
                            buffer.Append(currentChar);
                        }
                        else
                        {
                            var identStr = buffer.ToString();
                            if (lexemeTable.ContainsKey(identStr))
                            {
                                translated.Add(lexemeTable[identStr]);
                            }
                            else
                            {
                                if (!identifiers.ContainsKey(identStr))
                                {
                                    idCount++;
                                    identifiers[identStr] = $"I{idCount}";
                                }
                                translated.Add(identifiers[identStr]);
                            }
                            buffer.Clear();
                            state = "default";
                            i--; // Повторно обработаем текущий символ
                        }
                        break;

                    case "string":
                        buffer.Append(currentChar);
                        if (currentChar == '\'' || currentChar == '"')
                        {
                            var str = buffer.ToString();
                            if (!strings.ContainsKey(str))
                            {
                                strCount++;
                                strings[str] = $"C{strCount}";
                            }
                            translated.Add(strings[str]);
                            buffer.Clear();
                            state = "default";
                        }
                        break;

                    case "multiline_string":
                        buffer.Append(currentChar);

                        // Проверяем конец многострочной строки (3 одинаковых кавычки подряд)
                        if ((currentChar == '\'' && i + 2 < line.Length && line[i + 1] == '\'' && line[i + 2] == '\'') ||
                            (currentChar == '"' && i + 2 < line.Length && line[i + 1] == '"' && line[i + 2] == '"'))
                        {
                            buffer.Append(line[++i]); // Второй символ
                            buffer.Append(line[++i]); // Третий символ

                            var str = buffer.ToString();
                            if (!strings.ContainsKey(str))
                            {
                                strCount++;
                                strings[str] = $"C{strCount}";
                            }
                            translated.Add(strings[str]);
                            buffer.Clear();
                            state = "default";
                        }
                        break;
                }
            }

            // Обработка оставшегося буфера после окончания строки
            switch (state)
            {
                case "number":
                    var numStr = buffer.ToString();
                    if (!numbers.ContainsKey(numStr))
                    {
                        numCount++;
                        numbers[numStr] = $"N{numCount}";
                    }
                    translated.Add(numbers[numStr]);
                    break;

                case "identifier":
                    var identStr = buffer.ToString();
                    if (lexemeTable.ContainsKey(identStr))
                    {
                        translated.Add(lexemeTable[identStr]);
                    }
                    else
                    {
                        if (!identifiers.ContainsKey(identStr))
                        {
                            idCount++;
                            identifiers[identStr] = $"I{idCount}";
                        }
                        translated.Add(identifiers[identStr]);
                    }
                    break;

                case "string":
                    throw new Exception("Unclosed string literal");
            }

            return string.Join(" ", translated);
        }
    }
}
