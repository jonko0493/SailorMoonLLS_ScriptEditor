using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SailorMoonLLS_ScriptEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public NutrRaw Nutr { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void FileMenuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Decompressed NUTR file|*.nutr.decompressed"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                Nutr = NutrRaw.ParseFromFile(openFileDialog.FileName);
                commandsListBox.ItemsSource = Nutr.PostScriptCommands.Select(c => $"{c.Line(Nutr.Script.Select(n => n.Text).ToList())} (0x{c.LineNumberMaybe:X2}):\t\t" +
                    $"0x{string.Join(" ", c.CommandBytes.Select(cb => $"{cb:X2}"))}");
            }
        }

        private void FileMenuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Decompressed NUTR file|*.nutr.decompressed"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                Nutr.WriteToFile(saveFileDialog.FileName);
            }
        }

        private void FileMenuExtractScript_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text File|*.txt"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                var scriptText = Nutr.PostScriptCommands.Select(c => c.Line(Nutr.Script.Select(n => n.Text).ToList()));
                List<string> extractedScript = new List<string>();

                bool message = false;
                foreach (string line in scriptText)
                {
                    if (message)
                    {
                        extractedScript.Add(line);
                        message = false;
                    }
                    else if (line == "msg")
                    {
                        message = true;
                    }
                    else if (line == "talk")
                    {
                        extractedScript.Add("\n");
                    }
                }

                File.WriteAllLines(saveFileDialog.FileName, extractedScript.ToArray());
            }
        }
    }
}
