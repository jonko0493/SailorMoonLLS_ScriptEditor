using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailorMoonLLS_ScriptEditor
{
    public class Nutr
    {
        public byte[] BoilerPlate { get; private set; }
        public List<NutrLine> Script { get; set; } = new List<NutrLine>();
        public List<byte> PostScript { get; set; } = new List<byte>();
        public List<PostScriptCommand> PostScriptCommands { get; set; } = new List<PostScriptCommand>();
        public List<byte> PostPostScript { get; set; } = new List<byte>();

        public List<DialogueBox> DialogueBoxes { get; set; } = new List<DialogueBox>();

        public static int DramaBoilerPlateLength = 0x1A3;

        public enum FileType
        {
            DRAMA,
            INTERFACE,
        }

        public static Nutr ParseFromFile(string file)
        {
            byte[] data = File.ReadAllBytes(file);
            Nutr nutr = new Nutr();
            int filePtr;

            nutr.BoilerPlate = data.Take(DramaBoilerPlateLength).ToArray();
            for (filePtr = DramaBoilerPlateLength; filePtr < data.Length;)
            {
                int length = BitConverter.ToInt32(new byte[] { data[filePtr], data[filePtr + 1], data[filePtr + 2], data[filePtr + 3] });
                filePtr += 4;

                byte[] textBytes = new byte[length];
                for (int j = 0; j < length; j++)
                {
                    textBytes[j] = data[filePtr];
                    filePtr++;
                }
                string text = Encoding.UTF8.GetString(textBytes);

                if (text == "suspend")
                {
                    nutr.Script.Add(new NutrLine { Text = text });
                    break;
                }

                byte[] lineEnd = new byte[] { data[filePtr], data[filePtr + 1], data[filePtr + 2], data[filePtr + 3] };
                if (!lineEnd.SequenceEqual(NutrLine.LineEnd))
                {
                    throw new FileFormatException($"Expected line end after string '{text}' but found sequence '0x{lineEnd[0]:X2} 0x{lineEnd[1]:X2} 0x{lineEnd[2]:X2} 0x{lineEnd[3]:X2}' instead.");
                }
                filePtr += 4;

                nutr.Script.Add(new NutrLine { Text = text });
            }

            bool encounteredTrapTrap = false;
            for (; ; filePtr++)
            {
                if (data[filePtr - 8] == 'T' && data[filePtr - 7] == 'R' && data[filePtr - 6] == 'A' && data[filePtr - 5] == 'P' &&
                    data[filePtr - 4] == 'T' && data[filePtr - 3] == 'R' && data[filePtr - 2] == 'A' && data[filePtr - 1] == 'P')
                {
                    if (encounteredTrapTrap)
                    {
                        break;
                    }
                    else
                    {
                        encounteredTrapTrap = true;
                    }
                }

                nutr.PostScript.Add(data[filePtr]);
            }

            for (; !(data[filePtr] == 'T' && data[filePtr + 1] == 'R' && data[filePtr + 2] == 'A' && data[filePtr + 3] == 'P');)
            {
                int lineNumber = BitConverter.ToInt32(new byte[] { data[filePtr], data[filePtr + 1], data[filePtr + 2], data[filePtr + 3] });
                if (lineNumber > nutr.Script.Count)
                {
                    break;
                }

                nutr.PostScriptCommands.Add(new PostScriptCommand
                {
                    LineNumber = lineNumber,
                    CommandBytes = new byte[] { data[filePtr + 4], data[filePtr + 5], data[filePtr + 6], data[filePtr + 7] },
                });
                filePtr += 8;
            }

            for (; filePtr < data.Length; filePtr++)
            {
                nutr.PostPostScript.Add(data[filePtr]);
            }

            DialogueBox currentDialogueBox = null;
            bool msg = false;
            foreach (var line in nutr.PostScriptCommands)
            {
                string text = line.Line(nutr.Script.Select(l => l.Text).ToList());

                if (currentDialogueBox == null && text == "talk_window")
                {
                    currentDialogueBox = new DialogueBox();
                }
                else if (currentDialogueBox != null && text == "msg")
                {
                    msg = true;
                }
                else if (msg)
                {
                    currentDialogueBox.DialogueLineIndices.Add(line.LineNumber);
                    msg = false;
                }
                else if (currentDialogueBox != null && text == "talk")
                {
                    nutr.DialogueBoxes.Add(currentDialogueBox);
                    currentDialogueBox = null;
                }
            }

            return nutr;
        }

        public void WriteToFile(string file)
        {
            List<byte> data = new List<byte>();
            data.AddRange(BoilerPlate);
            foreach (NutrLine line in Script)
            {
                data.AddRange(line.ToBytes());
            }
            data.AddRange(PostScript);
            foreach (PostScriptCommand command in PostScriptCommands)
            {
                data.AddRange(command.ToBytes());
            }
            data.AddRange(PostPostScript);

            File.WriteAllBytes(file, data.ToArray());
        }
    }

    public class NutrLine
    {
        public int Length => Encoding.UTF8.GetByteCount(Text);
        public string Text { get; set; }
        public static byte[] LineEnd => new byte[] { 0x10, 0x00, 0x00, 0x08 };

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Length));
            bytes.AddRange(Encoding.UTF8.GetBytes(Text));
            if (Text != "suspend")
            {
                bytes.AddRange(LineEnd);
            }
            
            return bytes.ToArray();
        }
    }

    public class PostScriptCommand
    {
        public int LineNumber { get; set; }
        public byte[] CommandBytes { get; set; }

        public string Line(List<string> lines)
        {
            return lines[LineNumber];
        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(LineNumber));
            bytes.AddRange(CommandBytes);

            return bytes.ToArray();
        }
    }

    public class DialogueBox
    {
        public List<int> DialogueLineIndices { get; set; } = new List<int>();

        public List<string> DialogueLineStrings(Nutr nutr)
        {
            return DialogueLineIndices.Select(i => nutr.Script[i].Text).ToList();
        }

        public override string ToString()
        {
            return string.Join(", ", DialogueLineIndices);
        }
    }
}
