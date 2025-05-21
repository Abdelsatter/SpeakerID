using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Recorder
{
    public partial class Form1 : Form
    {
        Action<int> func = null;
        public Form1(Action<int> func)
        {
            this.func = func;
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Hide();
            this.func(-1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int pruning =int.Parse(Interaction.InputBox("Enter the number of frames to prune", "Pruning", "0", -1, -1));
            this.Hide();
            this.func(pruning);
        }
    }
}
