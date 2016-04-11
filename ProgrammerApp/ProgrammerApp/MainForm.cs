using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Forms;

namespace ProgrammerApp
{
    public partial class MainForm : Form
    {
        public string path_to_hex = "";
        public static int num_of_progs = 2;
        public static USBTinyISP[] progs = new USBTinyISP[num_of_progs];
        public static Label[] prog_labels = new Label[num_of_progs];
        public static RichTextBox[] prog_textBoxes = new RichTextBox[num_of_progs];
        public static System.Windows.Forms.Timer progressTimer = new System.Windows.Forms.Timer();

        public MainForm()
        {
            InitializeComponent();

            //DriveInfo[] allDrives = DriveInfo.GetDrives();
        }

        public static void connectProgrammers(int num_of_programmers)
        {
 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            String path = System.IO.Directory.GetCurrentDirectory() + "\\batches\\hex_path.txt";
            if (!File.Exists(path))
            {
                File.WriteAllText(path, String.Empty);
            }
            String path_to_hex = File.ReadAllText(path);
            //Console.WriteLine("Path length " + path_to_hex.Length);
            if (path_to_hex.Length == 0)
            {
                hexToolStripMenuItem_Click(this, null);
            }

            //Console.WriteLine("Hex path is " + path_to_hex);

            connectProgs();
            
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (num_of_progs == 0)
            {
                connectProgs();
            }
            else {
                path_to_hex = File.ReadAllText(System.IO.Directory.GetCurrentDirectory() + "\\batches\\hex_path.txt");

                if (path_to_hex.Length == 0)
                {
                    //Console.WriteLine(path_to_hex);
                    MessageBox.Show("No HEX File Selected", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    hexToolStripMenuItem_Click(this, null);
                    return;
                }
                String root = System.IO.Directory.GetCurrentDirectory();
                for (int i = 0; i < num_of_progs; i++)
                {
                    if (progs[i].hasBoard)
                    {
                        if (!progs[i].program(path_to_hex))
                        {
                            prog_textBoxes[i].ForeColor = Color.Red;
                        }
                        else
                        {
                            prog_textBoxes[i].ForeColor = Color.ForestGreen;
                        }
                        prog_textBoxes[i].Text = progs[i].message;
                    }
                }
            }
        }

        public void connectProgs()
        {
            USBTinyISP tempProg = new USBTinyISP();
            String root = System.IO.Directory.GetCurrentDirectory();
            String target = root + "\\batches\\check.bat";
            String args = String.Format("c");

            tempProg.performBat(target, args);

            string path = root + String.Format("\\batches\\check_results\\check_results_pc.txt");

            if (File.Exists(path))
            {
                while (!tempProg.IsFileReady(path)) ;
                string text = System.IO.File.ReadAllText(path);
                num_of_progs = Regex.Matches(text, "Found USBtinyISP").Count;
            }
            else
            {
                num_of_progs = 0;
            }
            
            if(num_of_progs == 0)
            {
                button1.Text = "Refresh";
            }
            else
            {
                button1.Text = "Upload";
            }
            richTextBox1.ForeColor = SystemColors.ControlText;
            richTextBox1.Text = String.Format("{0} programmers connected.", num_of_progs);

            for (int i = 0; i < num_of_progs; i++)
            {
                progs[i] = new USBTinyISP(i + 1);

                prog_labels[i] = new Label();
                Point loc = new Point(5, this.Height - 35);
                prog_labels[i].Location = loc;
                prog_labels[i].AutoSize = true;
                prog_labels[i].ForeColor = SystemColors.ControlText;
                prog_labels[i].Text = String.Format("P{0}:", i + 1);
                prog_labels[i].Parent = this;

                prog_textBoxes[i] = new RichTextBox();
                loc = new Point(prog_labels[i].Width + 5, this.Height - 55);
                prog_textBoxes[i].Location = loc;
                prog_textBoxes[i].Width = 195;
                prog_textBoxes[i].Height = 20;
                prog_textBoxes[i].ForeColor = SystemColors.ControlText;
                prog_textBoxes[i].Parent = this;


                if (!progs[i].connected())
                {
                    prog_textBoxes[i].ForeColor = Color.Red;
                }
                else
                {
                    prog_textBoxes[i].ForeColor = Color.DarkCyan;
                }
                prog_textBoxes[i].Text = progs[i].message;
            }
            //Console.WriteLine("{0} progs found", num_of_progs);
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void hexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Displays an OpenFileDialog so the user can select a Cursor.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Compiled Hex|*.hex";
            openFileDialog1.Title = "Select a Core Hex File";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                path_to_hex = openFileDialog1.FileName;
                String path = System.IO.Directory.GetCurrentDirectory() + "\\batches\\hex_path.txt";
                File.WriteAllText(path, path_to_hex);
                //Console.WriteLine("New path to hex is {0}", path_to_hex);
            }
        }

        private void exitStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
