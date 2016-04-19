using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Forms;

namespace ProgrammerApp
{
    public partial class MainForm : Form
    {
        BackgroundWorker progressWorker;

        public string path_to_hex = "";
        public static int num_of_progs = 2;
        public static USBTinyISP[] progs = new USBTinyISP[num_of_progs];
        public static Label[] prog_labels = new Label[num_of_progs];
        public static RichTextBox[] prog_textBoxes = new RichTextBox[num_of_progs];
        public static ToolStripMenuItem[] prog_menuitems = new ToolStripMenuItem[num_of_progs];
        public static ToolStripMenuItem refreshMenuItem = new ToolStripMenuItem();
        public static System.Windows.Forms.Timer progressTimer = new System.Windows.Forms.Timer();

        public MainForm()
        {
            InitializeComponent();
            progressWorker = new BackgroundWorker();
            progressWorker.WorkerReportsProgress = true;
            progressWorker.DoWork += new DoWorkEventHandler(progressWork);
            progressWorker.ProgressChanged += new ProgressChangedEventHandler(progressReporter);
            //DriveInfo[] allDrives = DriveInfo.GetDrives();
        }

        public void reinitializeComponent()
        {
            this.Controls.Clear();
            InitializeComponent();
            connectProgs();
        }

        public static void connectProgrammers(int num_of_programmers)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            refreshMenuItem.Text = "Refresh";
            refreshMenuItem.Click += new EventHandler(button1_Click);
            String path = System.IO.Directory.GetCurrentDirectory() + "\\batches\\hex_path.txt";
            if (!File.Exists(path))
            {
                File.WriteAllText(path, String.Empty);
            }
            String path_to_hex = File.ReadAllText(path);
            Console.WriteLine("Path length " + path_to_hex.Length);
            if (path_to_hex.Length == 0)
            {
                hexToolStripMenuItem_Click(this, null);
            }

            Console.WriteLine("Hex path is " + path_to_hex);

            connectProgs();
            message(String.Format("{0} programmers connected.", num_of_progs));
        }

        public void connectProgs()
        {
            USBTinyISP tempProg = new USBTinyISP();
            String root = System.IO.Directory.GetCurrentDirectory();
            String target = root + "\\batches\\check.bat";
            String args = String.Format("c");

            tempProg.performBat(target, args);

            string path = root + String.Format("\\batches\\check_results\\check_results_p{0}.txt", args);
       
            
            if (File.Exists(path))
            {
                var task = Task.Run(() => tempProg.IsFileReady(path));
                if (task.Wait(TimeSpan.FromSeconds(2)))
                {
                    if(task.Result)
                    {
                        string text = System.IO.File.ReadAllText(path);
                        num_of_progs = Regex.Matches(text, "Found USBtinyISP").Count;
                    }
                    else
                    {
                        message("File Error: Could not open");
                    }
                }
                else
                {
                    message("File Error: Timed out");
                    num_of_progs = 0;
                }
                task.Dispose();
            }
            else
            {
                num_of_progs = 0;
            }

            if (num_of_progs == 0)
            {
                button1.Text = "Refresh";
                deviceToolStripMenuItem.DropDownItems.Add(refreshMenuItem);
            }
            else
            {
                button1.Text = "Program";
                //button1.Text = "Upload";
                //deviceToolStripMenuItem.DropDownItems.Remove(refreshMenuItem);
            }

            for (int i = 0; i < num_of_progs; i++)
            {
                if (prog_labels[i] == null)
                {
                    addProgrammer(i);
                }

                if (!progs[i].active)
                {
                    return;
                }
                if (!progs[i].connected())
                {
                    button1.Text = "Refresh";
                    prog_textBoxes[i].ForeColor = Color.White;
                    prog_textBoxes[i].BackColor = Color.Firebrick;
                }
                else
                {
                    prog_textBoxes[i].ForeColor = Color.White;
                    prog_textBoxes[i].BackColor = Color.DarkCyan;
                }
                prog_textBoxes[i].Text = progs[i].message;
            }
            Console.WriteLine("{0} progs found", num_of_progs);
        }

        private void addProgrammer(int i)
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

            prog_menuitems[i] = new ToolStripMenuItem();
            prog_menuitems[i].Text = "USBTinyISP_0" + i;
            prog_menuitems[i].Checked = true;
            prog_menuitems[i].Click += new EventHandler(delegate (Object o, EventArgs a)
            {
                prog_menuitems[i].Checked = !prog_menuitems[i].Checked;
                progs[i].active = prog_menuitems[i].Checked;
                if (!prog_menuitems[i].Checked)
                {
                    prog_textBoxes[i].ForeColor = Color.White;
                    prog_textBoxes[i].BackColor = Color.LightGray;
                    prog_textBoxes[i].Text = "Inactive";
                }
                else
                {
                    connectProgs();
                }

            });
            deviceToolStripMenuItem.DropDownItems.Add(prog_menuitems[i]);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            connectProgs();
            bool hasBoard = false;
            for (int i = 0; i < num_of_progs; i++)
            {
                if (progs[i].connected())
                {
                    hasBoard = true;
                }
            }
            if(!hasBoard)
            {
                button1.Text = "Refresh";
                return;
            }

            button1.Text = "Uploading...";
            progressWorker.RunWorkerAsync(100);
            if (num_of_progs == 0)
            {
                reinitializeComponent();
            }
            else
            {
                performCore();
            }
            button1.Text = "Program";
        }


        private void progressWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            
            worker.ReportProgress(100);
        }

        private void progressReporter(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void performCore()
        {
            path_to_hex = File.ReadAllText(System.IO.Directory.GetCurrentDirectory() + "\\batches\\hex_path.txt");

            if (path_to_hex.Length == 0)
            {
                //Console.WriteLine(path_to_hex);
                MessageBox.Show("No HEX File Selected", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                hexToolStripMenuItem_Click(this, null);
                return;
            }
            String root = System.IO.Directory.GetCurrentDirectory();
            bool success = false;
            for (int i = 0; i < num_of_progs; i++)
            {
                if (progs[i].active && progs[i].hasBoard)
                {
                    if (!progs[i].program(path_to_hex))
                    {
                        prog_textBoxes[i].ForeColor = Color.White;
                        prog_textBoxes[i].BackColor = Color.Firebrick;
                    }
                    else
                    {
                        prog_textBoxes[i].ForeColor = Color.White;
                        prog_textBoxes[i].BackColor = Color.ForestGreen;
                        success = true;
                    }
                    prog_textBoxes[i].Text = progs[i].message;
                }
            }
            if (!success)
            {
                message("Error - No Board");
            }
            else
            {
                for (int i = 0; i < num_of_progs; i++)
                {
                    if (progs[i].hasSuccess)
                    {
                        message(String.Format("Uploaded {0} on P{1}", path_to_hex, progs[i].id));
                    }
                }
            }
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
                message(String.Format("Path to hex is name {0}", path_to_hex));
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

        private void message(String text)
        {
            if (richTextBox1.Text.Length > richTextBox1.MaxLength-100)
            {
                richTextBox1.Text = "";
            }
            DateTime time = new DateTime(3057, 3, 14);
            richTextBox1.Text += String.Format("{0}: {1}\n", DateTime.Now.ToString("hh:mm:sstt"), text);
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("USBTinyISP Gang Programmer Tool v1.1 \nCCI Group© 2016", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            String root = System.IO.Directory.GetCurrentDirectory();
            String path = root + "\\srcs\\help.html";
            Console.WriteLine(path);
            System.Diagnostics.Process.Start(path);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {

        }
    }
}
