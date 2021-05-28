using NUnit.Framework;
using SailorMoonLLS_ScriptEditor;
using System.IO;

namespace SailorMoonLLS_Tests
{
    public class NutrTests
    {
        private const string DRAMA_000 = ".\\inputs\\drama_000_TEST.NUTR.decompressed";
        private const string DRAMA_001 = ".\\inputs\\drama_001.NUTR.decompressed";
        private const string DRAMA_002 = ".\\inputs\\drama_002.NUTR.decompressed";
        private const string INTERFACE = ".\\inputs\\interface.NUTR.decompressed";
        private const string ITEM_FILE = ".\\inputs\\item.NUTR.decompressed";

        [Test]
        [TestCase(DRAMA_000, Nutr.FileTypeBPLength.DRAMA)]
        [TestCase(DRAMA_001, Nutr.FileTypeBPLength.DRAMA)]
        [TestCase(DRAMA_002, Nutr.FileTypeBPLength.DRAMA)]
        [TestCase(INTERFACE, Nutr.FileTypeBPLength.INTERFACE)]
        [TestCase(ITEM_FILE, Nutr.FileTypeBPLength.ITEM)]
        public void NutrParseWriteSame(string file, Nutr.FileTypeBPLength fileType)
        {
            byte[] dataOnDisk = File.ReadAllBytes(file);
            Nutr nutr = Nutr.ParseFromFile(file, fileType);

            byte[] dataInMemory = nutr.GetBytes();

            Assert.AreEqual(dataOnDisk, dataInMemory);
        }
    }
}