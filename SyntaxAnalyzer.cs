using System;
using System.Collections.Generic;
using System.Linq;

public class SyntaxAnalyzer
{
    private readonly Dictionary<string, string> lexemeTable = new Dictionary<string, string>
        {
            // Операторы
            {"+", "O1"}, {"-", "O2"}, {"*", "O3"}, {"/", "O4"}, {"**", "O5"},
            {"<", "O6"}, {">", "O7"}, {"==", "O8"}, {"!=", "O9"}, {">=", "O10"}, {"<=", "O11"},
            {"and", "O12"}, {"or", "O13"}, {"not", "O14"}, {"%", "O15"}, {"//", "O16"},

            // Разделители
            {" ", "R1"}, {",", "R2"}, {".", "R3"}, {"(", "R4"}, {")", "R5"}, {":", "R6"},
            {"[", "R7"}, {"]", "R8"}, {"\t", "R9"}, {"\n", "R10"}, {"DEINDENT", "R11"},

            // Ключевые слова
            {"=", "W1"}, {"if", "W2"}, {"elif", "W3"}, {"else", "W4"}, {"while", "W5"}, {"for", "W6"},
            {"None", "W7"}, {"True", "W8"}, {"False", "W9"},
            {"def", "W10"}, {"return", "W11"}, {"int", "W12"}, {"str", "W13"}, {"float", "W14"},
            {"print", "W15"}, {"input", "W16"}, {"in", "W17"},

            // Комментарии
            {"#", "C1"}
        };

    private List<string>? _inputTokens;
    private int _currentTokenIndex;

    public Tuple<string, bool> Analyze(string input)
    {
        _inputTokens = input.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        _currentTokenIndex = 0;

        try
        {
            Program();
            return new Tuple<string, bool>("Синтаксический анализ проведен успешно, ошибок нет.", true);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            return new Tuple<string, bool> ($"Синтаксическая ошибка: {ex.Message}", false);
        }
    }

    private void Program()
    {
        while (_currentTokenIndex < _inputTokens?.Count)
        {
            if (Peek() == "W10") // def
            {
                FunctionDefinition();
            }
            else
            {
                Statement();
            }
        }
    }

    private void FunctionDefinition()
    {
        Expect("W10"); // def
        Expect("I");   // Идентификатор функции
        Expect("R4");  // (

        // Параметры функции (могут отсутствовать)
        if (Peek() != "R5") // )
        {
            ParameterList();
        }

        Expect("R5");  // )
        Expect("R6");  // :
        Expect("R9"); // \t

        // Тело функции
        while (Peek() != "W11") // return или конец функции
        {
            Statement();
        }

        // Возврат из функции (может отсутствовать)
        if (Peek() == "W11")
        {
            Expect("W11"); // return
            Expression();
            Expect("R11");
        }
    }

    private void ParameterList()
    {
        Expect("I"); // Идентификатор параметра

        while (Peek() == "R2") // ,
        {
            Expect("R2"); // ,
            Expect("I");  // Следующий идентификатор параметра
        }
    }

    private void Statement()
    {
        while (Peek() == "R9") _currentTokenIndex++;
        string token = Peek();

        if (token == "W2") // if
        {
            IfStatement();
        }
        else if (token == "W5") // while
        {
            WhileStatement();
        }
        else if (token == "W6") // for
        {
            ForStatement();
        }
        else if (token == "I") // присваивание или вызов функции
        {
            // Проверяем, является ли это вызовом функции
            string nextToken = Peek(1);
            if (nextToken == "R4") // (
            {
                FunctionCall();
            }
            else
            {
                Assignment();
            }
        }
        else if (token == "W15") // print
        {
            PrintStatement();
        }
        else
        {
            throw new Exception($"Unexpected token {token} at position {_currentTokenIndex}");
        }
    }

    private void IfStatement()
    {
        Expect("W2"); // if
        Expression();
        Expect("R6"); // :
        Expect("R9");

        // Блок if
        while (!(Peek() == "W3" || Peek() == "W4" || Peek() == "W2" || Peek() == "R11"))
        {
            Statement();
        }
        Expect("R11"); // DEINDENT

        // elif
        while (Peek() == "W3") // elif
        {
            Expect("W3"); // elif
            Expression();
            Expect("R6");  // :
            Expect("R9");

            // Блок elif
            while (!(Peek() == "W3" || Peek() == "W4" || Peek() == "W2" || Peek() == "R11"))
            {
                Statement();
            }
            Expect("R11");
        }

        // else
        if (Peek() == "W4") // else
        {
            Expect("W4"); // else
            Expect("R6");  // :
            Expect("R9");

            // Блок else
            while (!(Peek() == "W3" || Peek() == "W4" || Peek() == "W2" || Peek() == "R11"))
            {
                Statement();
            }
            Expect("R11");
        }
    }

    private void WhileStatement()
    {
        Expect("W5"); // while
        Expression();
        Expect("R6"); // :

        // Тело цикла
        while (Peek() != "W5" && Peek() != "W2" && Peek() != "W6" && Peek() != "R11")
        {
            Statement();
        }
        Expect("R11");
    }

    private void ForStatement()
    {
        Expect("W6"); // for
        Expect("I");  // Идентификатор переменной цикла
        Expect("I"); // in
        Expect("I");  // range
        Expect("R4"); // (
        Expression();
        Expect("R5"); // )
        Expect("R6"); // :
        Expect("R9"); // \t

        // Тело цикла
        while (Peek() != "W6" && Peek() != "W2" && Peek() != "W5" && Peek() != "R11")
        {
            Statement();
        }
        Expect("R11");
    }

    private void Assignment()
    {
        Expect("I");  // Идентификатор переменной
        Expect("W1"); // =
        Expression();
    }

    private void FunctionCall()
    {
        Expect("I");  // Имя функции
        Expect("R4"); // (

        // Аргументы (могут отсутствовать)
        if (Peek() != "R5") // )
        {
            ArgumentList();
        }

        Expect("R5"); // )
    }

    private void ArgumentList()
    {
        Expression();

        while (Peek() == "R2") // ,
        {
            Expect("R2"); // ,
            Expression();
        }
    }

    private void PrintStatement()
    {
        Expect("W15"); // print
        Expect("R4");  // (
        Expression();
        Expect("R5");  // )
    }

    private void Expression()
    {
        while (Peek() == "R9") _currentTokenIndex++;
        // Простое выражение (можно расширить для поддержки более сложных выражений)
        if (Peek() == "I")
        {
            if (Peek(1) == "R7") // [
            {
                Expect(Peek());
                Expect("R7"); // [
                Expression();
                Expect("R8"); // ]
            }
            else {
                Expect(Peek());

                // Проверяем, есть ли операция
                if (IsOperator(Peek()))
                {
                    Expect(Peek()); // Оператор
                    Expression();  // Правая часть выражения
                }
            }
        }
        else if (Peek() == "N")
        {
            Expect(Peek());

            // Проверяем, есть ли операция
            if (IsOperator(Peek()))
            {
                Expect(Peek()); // Оператор
                Expression();  // Правая часть выражения
            }
        }
        else if (Peek() == "R4") // (
        {
            Expect("R4"); // (
            Expression();
            Expect("R5"); // )
        }
        else if (Peek() == "R7")
        {
            ListAssignment();
        }
        else
        {
            throw new Exception($"Unexpected token {Peek()} in expression");
        }
    }

    private void ListAssignment()
    {
        Expect("R7"); // [

        if (Peek() != "R8") // ]
        {
            ArgumentList();
        }

        Expect("R8"); // ]
    }

    private bool IsOperator(string token)
    {
        return token.StartsWith("O") || token == "W1"; // W1 - это оператор присваивания
    }

    private string Peek(int offset = 0)
    {
        if (_currentTokenIndex + offset >= _inputTokens.Count)
        {
            return "";
        }

        string token = _inputTokens[_currentTokenIndex + offset];

        if (token.StartsWith("W") || token.StartsWith("O") || token.StartsWith("R") ||
            token.StartsWith("C"))
        {
            return token;
        }
        else if (token.StartsWith("N"))
        {
            return "N";
        }
        else if (token.StartsWith("I"))
        {
            return "I";
        }

        throw new Exception($"Unknown token type: {token}");
    }

    private void Expect(string expected)
    {
        string actual = Peek();

        if (actual != expected)
        {
            string expectedToken = expected != "R9" ? lexemeTable.FirstOrDefault(x => x.Value == expected).Key : "\\t";
            string actualToken = actual != "R9" ? lexemeTable.FirstOrDefault(x => x.Value == actual).Key : "\\t";
            throw new Exception($"Expected \"{expectedToken}\" but found \"{actualToken}\" at position {_currentTokenIndex}");
        }

        _currentTokenIndex++;
    }

}