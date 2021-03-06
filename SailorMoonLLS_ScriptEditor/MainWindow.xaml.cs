using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SailorMoonLLS_ScriptEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Nutr NutrFile { get; set; }

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
                Nutr.FileTypeBPLength fileType = Nutr.FileTypeBPLength.DRAMA;
                if (openFileDialog.FileName.Contains("interface", StringComparison.OrdinalIgnoreCase))
                {
                    fileType = Nutr.FileTypeBPLength.INTERFACE;
                }
                else if (openFileDialog.FileName.Contains("item", StringComparison.OrdinalIgnoreCase))
                {
                    fileType = Nutr.FileTypeBPLength.ITEM;
                }
                Title = $"Sailor Moon La Luna Splende Script Editor - {Path.GetFileName(openFileDialog.FileName)}";
                NutrFile = Nutr.ParseFromFile(openFileDialog.FileName, fileType);
                commandsListBox.ItemsSource = NutrFile.PostScriptCommands.Select(c => $"{c.Line(NutrFile.Script.Select(n => n.Text).ToList())} (0x{c.LineNumber:X2}):\t\t" +
                    $"0x{string.Join(" ", c.CommandBytes.Select(cb => $"{cb:X2}"))}");

                dialogueListBox.ItemsSource = NutrFile.DialogueBoxes;
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
                NutrFile.WriteToFile(saveFileDialog.FileName);
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
                var dialogueBoxes = NutrFile.DialogueBoxes.Select(n => n.DialogueLineStrings(NutrFile)).ToList();
                for (int i = 0; i < dialogueBoxes.Count; i++)
                {
                    dialogueBoxes[i].Add("");
                }
                File.WriteAllLines(saveFileDialog.FileName, dialogueBoxes.SelectMany(n => n));
            }
        }

        private void DialogueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editStackPanel.Children.Clear();

            if (e.AddedItems.Count == 0)
            {
                return;
            }

            var dialogueBox = (DialogueBox)e.AddedItems[0];

            List<DialogueTextBox> dialogueTextBoxes = new List<DialogueTextBox>();
            for (int i = 0; i < dialogueBox.DialogueLineIndices.Count; i++)
            {
                dialogueTextBoxes.Add(new DialogueTextBox
                {
                    Text = NutrFile.Script[dialogueBox.DialogueLineIndices[i]].Text,
                    DialogueBox = dialogueBox,
                    LineIndex = dialogueBox.DialogueLineIndices[i],
                    MaxLength = 20,
                });
                dialogueTextBoxes[i].TextChanged += DialogueTextBox_TextChanged;
                editStackPanel.Children.Add(dialogueTextBoxes[i]);
            }
            dialogueTextBoxes.Last().AcceptsReturn = true;
            dialogueTextBoxes.Last().Height = 100;
            dialogueTextBoxes.Last().MaxLength = 0;
            dialogueTextBoxes.Last().MaxLines = 7 - dialogueTextBoxes.Count;
        }

        private void DialogueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var dialogueTextBox = (DialogueTextBox)sender;

            if (dialogueTextBox.MaxLength == 0)
            {
                var lines = dialogueTextBox.Text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].Substring(0, Math.Min(lines[i].Length, 20));
                }
                dialogueTextBox.Text = string.Join('\n', lines);
            }

            NutrFile.Script[dialogueTextBox.LineIndex].Text = dialogueTextBox.Text.Replace("\r", "");
        }

        private void TplToRiffPaletteButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Decompressed TPL File|*.TPL.decompressed"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var tileFile = PaletteFile.ParseFromFile(openFileDialog.FileName);
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "RIFF Palette File|*.pal"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    tileFile.WriteRiffPaletteFile(saveFileDialog.FileName);
                }
            }
        }
    }
}
