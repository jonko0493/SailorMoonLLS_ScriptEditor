using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SailorMoonLLS_ScriptEditor
{
    public class DialogueTextBox : TextBox
    {
        public DialogueBox DialogueBox { get; set; }
        public int LineIndex { get; set; }
    }
}
