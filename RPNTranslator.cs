using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace PythonToCSharp
{
    internal class RPNTranslator
    {
        private static Stack<string> stack = new Stack<string>();
        private static List<string> output = new List<string>();
        private static Dictionary<string, string> variables = new Dictionary<string, string>();
        private static int tempCounter = 0;
        private static bool inMethod = false;
        private static string currentMethod = null;

        private static bool IsNumber(string s)
        {
            return double.TryParse(s, out _);
        }

        private static bool IsIdentifier(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            if (!char.IsLetter(token[0]) && token[0] != '_') return false;

            foreach (char c in token)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    return false;
                }
            }
            return true;
        }

        public static string TranslateRPN(string opz)
        {
            stack.Clear();
            output.Clear();
            variables.Clear();
            tempCounter = 0;
            inMethod = false;
            currentMethod = null;

            // Добавляем стандартные using и объявление класса
            output.Add("using System;");
            output.Add("using System.Collections.Generic;");
            output.Add("");
            output.Add("public class Program");
            output.Add("{");
            output.Add("    public static void Main(string[] args)");
            output.Add("    {");

            string[] tokens = opz.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            while (i < tokens.Length)
            {
                string token = tokens[i];

                // Объявление функции (nФ)
                if (token.EndsWith("Ф"))
                {
                    int argCount = int.Parse(token.Substring(0, token.Length - 1)) - 1;
                    List<string> args = new List<string>();
                    for (int j = 0; j < argCount; j++)
                    {
                        args.Insert(0, stack.Pop());
                    }
                    string funcName = stack.Pop();
                    stack.Push(funcName); // возвращаем имя обратно для НП

                    // Добавляем тип возвращаемого значения (упрощенно - всегда double)
                    output.Add($"    public static double {funcName}({string.Join(", ", args.ConvertAll(a => $"double {a}"))})");
                    output.Add("    {");
                    inMethod = true;
                    currentMethod = funcName;
                }
                // Начало процедуры (НП)
                else if (token == "НП")
                {
                    if (!inMethod)
                    {
                        string funcName = stack.Pop();
                        output.Add($"    public static void {funcName}()");
                        output.Add("    {");
                        inMethod = true;
                        currentMethod = funcName;
                    }
                }
                // Конец процедуры (КП)
                else if (token == "КП")
                {
                    if (inMethod)
                    {
                        output.Add("    }");
                        inMethod = false;
                        currentMethod = null;
                    }
                }
                // Возврат из функции
                else if (token == "return")
                {
                    string value = stack.Pop();
                    output.Add($"        return {value};");
                }
                // Присваивание
                else if (token == "=")
                {
                    string value = stack.Pop();
                    string var = stack.Pop();

                    // Определяем тип переменной
                    string type = "int"; // по умолчанию
                    if (value.Contains(",") && value.Contains(".")) type = "List<double>";
                    else if (value.Contains(".")) type = "double";
                    else if (value.Contains("= [")) type = "List<int>";
                    else if (value.Contains("new ")) type = $"{value.Split(' ')[0]} {value.Split(' ')[1]}";

                    // Если переменная еще не объявлена
                    if (!variables.ContainsKey(var))
                    {
                        output.Add($"        {type} {var} = {value};");
                        variables.Add(var, type);
                    }
                    else
                    {
                        output.Add($"        {var} = {value};");
                    }
                }
                // Доступ к элементу массива (2АЭМ)
                else if (token == "2АЭМ")
                {
                    string index = stack.Pop();
                    string arr = stack.Pop();
                    stack.Push($"{arr}[{index}]");
                }
                // Создание массива/списка
                else if (token == "[")
                {
                    List<string> elements = new List<string>();
                    string type = "int";
                    i++;
                    while (i < tokens.Length && tokens[i] != "]")
                    {
                        if (tokens[i] != ",")
                        {
                            elements.Add(tokens[i]);
                            if (tokens[i].Contains("."))
                                type = "double";
                        }
                        i++;
                    }
                    stack.Push($"new List<{type}>() {{ {string.Join(", ", elements)} }}");
                }
                // Условный переход (УПЛ)
                else if (token == "УПЛ")
                {
                    string label = stack.Pop();
                    string condition = stack.Pop();
                    output.Add($"        if (!{condition})");
                    output.Add($"            goto {label};");
                }
                // Безусловный переход (БП)
                else if (token == "БП")
                {
                    string label = stack.Pop();
                    output.Add($"        goto {label};");
                }
                // Метка
                else if (token.EndsWith(":"))
                {
                    string label = token.Substring(0, token.Length - 1);
                    output.Add($"        {label}:");
                }
                // Арифметические операции
                else if (token == "+" || token == "-" || token == "*" || token == "/" || token == "%")
                {
                    string a = stack.Pop();
                    string b = stack.Pop();
                    stack.Push($"({b} {token} {a})");
                }
                // Возведение в степень
                else if (token == "**")
                {
                    string a = stack.Pop();
                    string b = stack.Pop();
                    stack.Push($"Math.Pow({b}, {a})");
                }
                // Операции сравнения
                else if (token == ">" || token == "<" || token == "==" || token == "!=" || token == ">=" || token == "<=")
                {
                    string a = stack.Pop();
                    string b = stack.Pop();
                    stack.Push($"({b} {token} {a})");
                }
                // Числа и идентификаторы
                else if (IsNumber(token) || IsIdentifier(token))
                {
                    stack.Push(token);
                }

                i++;
            }

            output.Add("    }");

            output.Add("}");

            return string.Join("\n", PostProcessing(output));
        }

        public static List<string> PostProcessing(List<string> code)
        {
            List<string> result = new List<string>();
            bool insideMain = false;
            bool functionFound = false;
            List<string> functionLines = new List<string>();
            int braceLevel = 0;
            int mainLine = 0;

            foreach (string line in code)
            {
                if (!insideMain)
                {
                    mainLine++;
                    result.Add(line);
                    if (line.Contains("public static void Main("))
                    {
                        insideMain = true;
                    }
                    continue;
                }

                // Если внутри Main
                if (line.Contains("public static") && line.Contains("(") && line.Contains(")"))
                {
                    functionFound = true;
                    functionLines.Add(line);
                    continue;
                }

                if (functionFound)
                {
                    functionLines.Add(line);

                    // Отслеживаем уровень вложенности скобок
                    braceLevel += CountChar(line, '{');
                    braceLevel -= CountChar(line, '}');

                    // Если уровень скобок вернулся к 0, функция закончилась
                    if (braceLevel == 0)
                    {
                        functionFound = false;
                        functionLines.Add(string.Empty);
                        // Добавляем функцию перед закрывающей скобкой Main
                        foreach (string funcLine in functionLines)
                        {
                            result.Insert(mainLine - 1, funcLine);
                            mainLine++;
                        }
                    }
                }
                else
                {
                    result.Add(line);
                }
            }

            return result;
        }

        private static int CountChar(string str, char ch)
        {
            int count = 0;
            foreach (char c in str)
            {
                if (c == ch) count++;
            }
            return count;
        }

    }
}
