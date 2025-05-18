using Microsoft.Win32;
using PythonToCSharp;
using System;
using System.IO;
using System.Windows;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private string proccessingFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void filePathButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Python files (*.py)|*.py";

            if (openFileDialog.ShowDialog() == true)
            {
                filePathTextBox.Text = openFileDialog.FileName;
                this.proccessingFilePath = openFileDialog.FileName;
            }
        }

        public string? fileRead(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private void runButtonClick(object sender, RoutedEventArgs e)
        {
            string outputFilePath = "tokens.out";
            string? fileContent = fileRead(this.proccessingFilePath);

            if (fileContent == null)
            {
                Console.WriteLine("Couldn't read the file for processing.");
                return;
            }

            string translatedText = String.Empty;
            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                Lexer lexer = new();
                string translated = lexer.ProcessText(fileContent);
                writer.WriteLine(translated);
                translatedText += translated;
            }

            var syntaxAnalyzer = new SyntaxAnalyzer();
            var result = syntaxAnalyzer.Analyze(translatedText);
            string resultText = result.Item1;
            bool syntaxIsCorrect = result.Item2;
            MessageBox.Show(resultText, syntaxIsCorrect ? "Успех" : "Неудача", MessageBoxButton.OK, MessageBoxImage.Information);

            if (syntaxIsCorrect)
            {
                string rpn = ReversePolishNotation.Convert(fileContent);
                using (StreamWriter writer = new StreamWriter("rpn.out"))
                {
                    writer.Write(rpn);
                    MessageBox.Show("Текст программы успешно переведён в ОПЗ.\nРезультат сохранён в файл rpn.out", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                using (StreamWriter writer = new StreamWriter("c_sharp.out"))
                {
                    writer.Write(RPNTranslator.TranslateRPN(rpn));
                    MessageBox.Show("Текст программы успешно переведён из ОПЗ в машинный код.\nРезультат сохранён в файл c_sharp.out", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}