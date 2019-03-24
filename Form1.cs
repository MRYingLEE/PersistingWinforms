using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartData.Persistent
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        /// This button exists in DESIGN mode, and the real button is NOT in design mode.
        /// Only components in DESIGN mode can generate source code.
        /// But you may use this button as a normal button, the source code can be generated according to the FINAL settings.
        /// You may assign properties of the real component to the one in design mode and vice versa.
        /// 
        private System.Windows.Forms.Button buttonInDESIGNmode;

        private static ComponentDesigner designer;

        private void Form1_Load(object sender, EventArgs e)
        {
            designer = new ComponentDesigner(typeof(Button));

            buttonInDESIGNmode = designer.ComponentInDesign as System.Windows.Forms.Button;

            propertyGrid1.SelectedObject = buttonInDESIGNmode;
        }

        private static int times = 0;

        private void button1_Click(object sender, EventArgs e)
        {
            times++;
            buttonInDESIGNmode.Text = "Change Text! You have tried " + times.ToString() + " times.";

            button1.Text = buttonInDESIGNmode.Text;

            richTextBox1.Text = designer.OriginalCode();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            buttonInDESIGNmode.Left++;

            button1.Left = buttonInDESIGNmode.Left;

            richTextBox1.Text = designer.OriginalCode();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            richTextBox1.Text = designer.OriginalCode();
        }
    }
}
