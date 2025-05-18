using System;
using System.Collections.Generic;
using System.Linq;

public class ReversePolishNotation
{
    private static readonly Dictionary<string, int> priority = new Dictionary<string, int>
    {
        {"(", 0}, {"[", 0}, {"АЭМ", 0}, {"Ф", 0}, {"if", 0},
        {",", 1}, {")", 1}, {"]", 1}, {":", 1}, {"else", 1}, {"DEINDENT", 1}, { "return", 1 },
        {"=", 2},
        {"!=", 3}, {">", 3}, {"<", 3}, {"==", 3},
        {"+", 4}, {"-", 4},
        {"*", 5}, {"/", 5}, {"%", 5},
        {"**", 6}
    };

    private static bool IsIdentifier(string token)
    {
        return !priority.ContainsKey(token) && token != "def" && token != "\n";
    }

    private static List<string> Tokenize(string code)
    {
        List<string> tokens = new List<string>();
        int i = 0;
        List<int> indentStack = new List<int> { 0 };

        while (i < code.Length)
        {
            if (i == 0 || code[i - 1] == '\n')
            {
                string remainingCode = code.Substring(i);
                string trimmed = remainingCode.TrimStart();
                int currentIndent = remainingCode.Length - trimmed.Length;

                if (currentIndent > indentStack.Last())
                {
                    indentStack.Add(currentIndent);
                    i += currentIndent;
                    continue;
                }
                else
                {
                    while (currentIndent < indentStack.Last())
                    {
                        tokens.Add("DEINDENT");
                        indentStack.RemoveAt(indentStack.Count - 1);
                    }
                    if (currentIndent != indentStack.Last())
                    {
                        throw new Exception("Несогласованные отступы");
                    }
                }
            }

            char ch = code[i];

            if (ch == '\n')
            {
                tokens.Add("\n");
                i++;
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue;
            }

            if (i + 1 < code.Length)
            {
                string twoChar = code.Substring(i, 2);
                if (priority.ContainsKey(twoChar))
                {
                    tokens.Add(twoChar);
                    i += 2;
                    continue;
                }
            }

            if (priority.ContainsKey(ch.ToString()))
            {
                tokens.Add(ch.ToString());
                i++;
                continue;
            }

            string current = "";
            while (i < code.Length && !char.IsWhiteSpace(code[i]) && !priority.ContainsKey(code[i].ToString()))
            {
                if (i + 1 < code.Length && priority.ContainsKey(code.Substring(i, 2)))
                {
                    break;
                }
                current += code[i];
                i++;
            }
            if (!string.IsNullOrEmpty(current))
            {
                tokens.Add(current);
            }
        }

        while (indentStack.Count > 1)
        {
            tokens.Add("DEINDENT");
            indentStack.RemoveAt(indentStack.Count - 1);
        }

        return tokens;
    }

    public static string ToRPN(string code)
    {
        List<string> tokens = Tokenize(code);
        Stack<object> stack = new Stack<object>();
        List<string> output = new List<string>();
        int i = 0;
        int labelCounter = 1;
        Stack<string> ifStack = new Stack<string>();
        Stack<string> elseLabels = new Stack<string>();
        bool inMethod = false;

        while (i < tokens.Count)
        {
            string token = tokens[i];
            Console.WriteLine(token);

            if (IsIdentifier(token))
            {
                if ((i + 1 < tokens.Count) && (tokens[i + 1] == "("))
                {
                    stack.Push(new Tuple<string, string, int>("Ф", token, 1));
                    output.Add(token);
                }
                else if ((i + 1 < tokens.Count) && (tokens[i + 1] == "["))
                {
                    stack.Push(new Tuple<string, int>("АЭМ", 2));
                    stack.Push("[");
                    output.Add(token);
                    i++;
                }
                else
                {
                    output.Add(token);
                }
            }
            else if ((token == "=") && (i + 1 < tokens.Count) && (tokens[i + 1] == "["))
            {
                i += 2;
                List<string> initValues = new List<string>();
                output.Add("[");
                while ((i < tokens.Count) && (tokens[i] != "]"))
                {
                    if (tokens[i] != ",")
                    {
                        initValues.Add(tokens[i]);
                    }
                    else
                    {
                        output.Add(ToRPN(string.Join("", initValues)));
                        output.Add(",");
                        initValues.Clear();
                    }
                    i++;
                }
                output.Add(ToRPN(string.Join("", initValues)) + " ]");
                initValues.Clear();
                output.Add("=");
            }
            else if ((token == "(") && stack.Count > 0 && stack.Peek() is Tuple<string, string, int> tuple && tuple.Item1 == "Ф")
            {
                List<string> initValues = new List<string>();
                var fTuple = (Tuple<string, string, int>)stack.Peek();
                var newFTuple = new Tuple<string, string, int>("Ф", fTuple.Item2, fTuple.Item3);

                while ((i < tokens.Count) && (tokens[i] != ")"))
                {
                    i++;
                    if (tokens[i] != ",")
                    {
                        initValues.Add(tokens[i]);
                    }
                    else
                    {
                        output.Add(ToRPN(string.Join("", initValues)));
                        var currFTuple = (Tuple<string, string, int>)stack.Pop();
                        newFTuple = new Tuple<string, string, int>("Ф", currFTuple.Item2, currFTuple.Item3 + 1);
                        stack.Push(newFTuple);
                        initValues.Clear();
                    }
                }
                output.Add(ToRPN(string.Join("", initValues)));
                output.Add($"{newFTuple.Item3 + 1}Ф");
                stack.Pop();
            }
            else if ((token == ",") && stack.Count > 0 && (stack.Peek() as string == "("))
            {
                while (stack.Count > 0 && (stack.Peek() as string != "("))
                {
                    object currToken = stack.Pop();
                    if (currToken is Tuple<string, string, int> funcToken)
                    {
                        output.Add($"{funcToken.Item2} {funcToken.Item3}Ф");
                    }
                    else if (currToken is Tuple<string, int> arrToken)
                    {
                        output.Add($"{arrToken.Item2}АЭМ");
                    }
                    else
                    {
                        output.Add(currToken.ToString());
                    }
                }

                foreach (var item in stack.ToArray())
                {
                    if (item is Tuple<string, string, int> ft && ft.Item1 == "Ф")
                    {
                        stack.Pop();
                        stack.Push(new Tuple<string, string, int>("Ф", ft.Item2, ft.Item3 + 1));
                        break;
                    }
                }
            }
            else if ((token == "]") && stack.Count > 0 && (stack.Peek() as string == "[") && (i + 1 < tokens.Count) && (tokens[i + 1] == "["))
            {
                i += 2;
                for (int j = 0; j < stack.Count; j++)
                {
                    if (stack.ElementAt(j) is Tuple<string, int> arrToken && arrToken.Item1 == "АЭМ")
                    {
                        stack.Pop();
                        stack.Push(new Tuple<string, int>("АЭМ", arrToken.Item2 + 1));
                        break;
                    }
                }
            }
            else if (token == ")")
            {
                while (stack.Count > 0 && (stack.Peek() as string != "("))
                {
                    object currToken = stack.Pop();
                    if (currToken is Tuple<string, string, int> funcToken)
                    {
                        output.Add($"{funcToken.Item2} {funcToken.Item3}Ф");
                    }
                    else if (currToken is Tuple<string, int> arrToken)
                    {
                        output.Add($"{arrToken.Item2}АЭМ");
                    }
                    else if (currToken is Tuple<string, string> ifToken && ifToken.Item1 == "if")
                    {
                        output.Add($"{ifToken.Item2} УПЛ");
                    }
                    else
                    {
                        output.Add(currToken.ToString());
                    }
                }
                if (stack.Count > 0)
                {
                    stack.Pop();
                    if (stack.Count > 0 && stack.Peek() is Tuple<string, string, int> fTuple && fTuple.Item1 == "Ф")
                    {
                        var popped = (Tuple<string, string, int>)stack.Pop();
                        output.Add($"{popped.Item2} {popped.Item3}Ф");
                    }
                }
            }
            else if (token == "]")
            {
                while (stack.Count > 0 && (stack.Peek() as string != "["))
                {
                    output.Add(stack.Pop().ToString());
                }
                if (stack.Count > 0)
                {
                    stack.Pop();
                    if (stack.Count > 0 && stack.Peek() is Tuple<string, int> arrToken && arrToken.Item1 == "АЭМ")
                    {
                        int count = ((Tuple<string, int>)stack.Pop()).Item2;
                        output.Add($"{count}АЭМ");
                    }
                }
            }
            else if (token == "if")
            {
                string currentLabel = $"М{labelCounter}";
                labelCounter++;
                ifStack.Push(currentLabel);
                stack.Push(new Tuple<string, string>("if", currentLabel));
            }
            else if (token == "else" && (i + 1 < tokens.Count) && (tokens[i + 1] == ":"))
            {
                if (ifStack.Count > 0)
                {
                    string elseLabel = $"М{labelCounter}";
                    output.Add(elseLabel);
                    labelCounter++;
                    output.Add($"БП {ifStack.Peek()}:");
                    elseLabels.Push(elseLabel);
                    i++;
                }
            }
            else if ((i + 1 < tokens.Count) && (token == ":") && (tokens[i + 1] == "\n") && (ifStack.Count == 0))
            {
                output.Add("НП");
                inMethod = true;
                i++;
            }
            else if (token == ":")
            {
                if (ifStack.Count > 0)
                {
                    while (!(stack.Peek() is Tuple<string, string> ifToken && ifToken.Item1 == "if"))
                    {
                        output.Add(stack.Pop().ToString());
                    }
                    output.Add($"{ifStack.Peek()} УПЛ");
                    stack.Pop();
                }
            }
            else if (token == "DEINDENT")
            {
                while (stack.Count > 0 && (stack.Peek() as string != ":"))
                {
                    object top = stack.Pop();
                    if (top is string)
                    {
                        output.Add(top.ToString());
                    }
                }
                if (inMethod && i + 1 >= tokens.Count)
                {
                    output.Add("КП");
                    stack.Pop();
                    inMethod = false;
                }
                if (stack.Count > 0)
                {
                    stack.Pop();
                }

                if ((i + 1 < tokens.Count) && (tokens[i + 1] != "else"))
                {
                    if (ifStack.Count > 0 && elseLabels.Count == 0)
                    {
                        output.Add($"{ifStack.Pop()}:");
                    }
                    else
                    {
                        output.Add("КП");
                        inMethod = false;
                    }
                }

                if (elseLabels.Count > 0)
                {
                    output.Add($"{elseLabels.Pop()}:");
                }
            }
            else if (token == "\n")
            {
                if ((i + 1 < tokens.Count) && (tokens[i + 1] == "DEINDENT"))
                {
                    i++;
                    continue;
                }
                while (stack.Count > 0)
                {
                    object currToken = stack.Pop();
                    if (currToken is Tuple<string, string, int> funcToken)
                    {
                        output.Add($"{funcToken.Item2} {funcToken.Item3}Ф");
                    }
                    else if (currToken is Tuple<string, int> arrToken)
                    {
                        output.Add($"{arrToken.Item2}АЭМ");
                    }
                    else
                    {
                        output.Add(currToken.ToString());
                    }
                }
                i++;
                continue;
            }
            else if (priority.ContainsKey(token))
            {
                while (stack.Count > 0 &&
                       !(stack.Peek() is Tuple<string, string, int>) &&
                       !(stack.Peek() is Tuple<string, int>) &&
                       !(stack.Peek() is Tuple<string, string>) &&
                       stack.Peek() as string != "(" &&
                       stack.Peek() as string != "[" &&
                       stack.Peek() as string != ":" &&
                       priority[stack.Peek() as string] >= priority[token])
                {
                    output.Add(stack.Pop().ToString());
                }
                stack.Push(token);
            }

            i++;
        }

        while (stack.Count > 0)
        {
            object currToken = stack.Pop();
            if (currToken is Tuple<string, string, int> funcToken)
            {
                output.Add($"{funcToken.Item2} {funcToken.Item3}Ф");
            }
            else if (currToken is Tuple<string, int> arrToken)
            {
                output.Add($"{arrToken.Item2}АЭМ");
            }
            else
            {
                output.Add(currToken.ToString());
            }
        }
        return string.Join(" ", output);
    }

    public static string Convert(string code)
    {
        string[] lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        List<string> filteredLines = new List<string>();

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith("#") && !string.IsNullOrWhiteSpace(trimmedLine))
            {
                filteredLines.Add(line);
            }
        }
        string filteredCode = string.Join("\n", filteredLines);

        string rpn = ToRPN(filteredCode);

        return rpn;
    }
}