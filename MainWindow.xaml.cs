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

        public string fileRead(string filePath)
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
            string fileContent = fileRead(this.proccessingFilePath);

            if (fileContent == null)
            {
                Console.WriteLine("Couldn't read the file for processing.");
                return;
            }

            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                Lexer lexer = new();
                string translated = lexer.ProcessText(fileContent);
                writer.WriteLine(translated);
            }
            Console.WriteLine($"Tokens have been successfully written to the file: {outputFilePath}");
        }
    }
}