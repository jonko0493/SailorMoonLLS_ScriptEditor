using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailorMoonLLS_ScriptEditor
{
    public class NutrRaw
    {
        public byte[] BoilerPlate { get; private set; }
        public List<NutrRawLine> Script { get; set; } = new List<NutrRawLine>();
        public List<byte> PostScript { get; set; } = new List<byte>();
        public List<PostScriptCommand> PostScriptCommands { get; set; } = new List<PostScriptCommand>();
        public List<byte> PostPostScript { get; set; } = new List<byte>();

        public static int BoilerPlateLength = 0x1A3;

        public static NutrRaw ParseFromFile(string file)
        {
            byte[] data = File.ReadAllBytes(file);
            NutrRaw nutrRaw = new NutrRaw();
            int filePtr;

            nutrRaw.BoilerPlate = data.Take(BoilerPlateLength).ToArray();
            for (filePtr = BoilerPlateLength; filePtr < data.Length;)
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
                    nutrRaw.Script.Add(new NutrRawLine { Text = text });
                    break;
                }

                byte[] lineEnd = new byte[] { data[filePtr], data[filePtr + 1], data[filePtr + 2], data[filePtr + 3] };
                if (!lineEnd.SequenceEqual(NutrRawLine.LineEnd))
                {
                    throw new FileFormatException($"Expected line end after string '{text}' but found sequence '0x{lineEnd[0]:X2} 0x{lineEnd[1]:X2} 0x{lineEnd[2]:X2} 0x{lineEnd[3]:X2}' instead.");
                }
                filePtr += 4;

                nutrRaw.Script.Add(new NutrRawLine { Text = text });
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

                nutrRaw.PostScript.Add(data[filePtr]);
            }

            for (; !(data[filePtr] == 'T' && data[filePtr + 1] == 'R' && data[filePtr + 2] == 'A' && data[filePtr + 3] == 'P');)
            {
                int lineNumber = BitConverter.ToInt32(new byte[] { data[filePtr], data[filePtr + 1], data[filePtr + 2], data[filePtr + 3] });
                if (lineNumber > nutrRaw.Script.Count)
                {
                    break;
                }

                nutrRaw.PostScriptCommands.Add(new PostScriptCommand
                {
                    LineNumberMaybe = lineNumber,
                    CommandBytes = new byte[] { data[filePtr + 4], data[filePtr + 5], data[filePtr + 6], data[filePtr + 7] },
                });
                filePtr += 8;
            }

            for (; filePtr < data.Length; filePtr++)
            {
                nutrRaw.PostPostScript.Add(data[filePtr]);
            }

            return nutrRaw;
        }

        public void WriteToFile(string file)
        {
            List<byte> data = new List<byte>();
            data.AddRange(BoilerPlate);
            foreach (NutrRawLine line in Script)
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

    public class NutrRawLine
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
        public int LineNumberMaybe { get; set; }
        public byte[] CommandBytes { get; set; }

        public string Line(List<string> lines)
        {
            return lines[LineNumberMaybe];
        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(LineNumberMaybe));
            bytes.AddRange(CommandBytes);

            return bytes.ToArray();
        }
    }
}
