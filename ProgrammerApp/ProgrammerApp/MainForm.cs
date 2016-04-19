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
        public static BackgroundWorker uploadWorker;

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
            uploadWorker = new BackgroundWorker();
            uploadWorker.WorkerReportsProgress = true;
            uploadWorker.ProgressChanged += worker_ProgressChanged;
            uploadWorker.DoWork += worker_DoWork;
            uploadWorker.RunWorkerCompleted += worker_RunWorkerCompleted;
            refreshMenuItem.Click += new EventHandler(connectProgs);
        }

        public void reinitializeComponent()
        {
            this.Controls.Clear();
            InitializeComponent();
            connectProgs();
            Form1_Load(this, null);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
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
            Console.WriteLine("{0} progs found", num_of_progs);
        }

        private void connectProgs(object sender, EventArgs e)
        {
            reinitializeComponent();
            connectProgs();
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
                var task = Task.Run(() => tempProg.WaitForFile(path));
                if (task.Wait(TimeSpan.FromSeconds(2)))
                {
                    if (task.Result)
                    {
                        string text = System.IO.File.ReadAllText(path);
                        num_of_progs = Regex.Matches(text, "Found USBtinyISP").Count;
                        if (num_of_progs == 0)
                        {
                            num_of_progs = 0;
                            button1.Text = "Refresh";
                            refreshMenuItem.Text = "Refresh";
                            toolStripMenuItem2.Enabled = false;
                            deviceToolStripMenuItem.DropDownItems.Add(refreshMenuItem);
                        }
                        else {
                            deviceToolStripMenuItem.DropDownItems.Remove(refreshMenuItem);
                            for (int i = 0; i < num_of_progs; i++)
                            {
                                Thread checkerThread = new Thread(new ThreadStart(() =>
                                {
                                    button1.Text = "Program";
                                    toolStripMenuItem2.Enabled = true;
                                    addProgrammer(i);

                                    if (progs[i].active)
                                    {
                                        if (progs[i].connected())
                                        {
                                            prog_textBoxes[i].Invoke((MethodInvoker)(() =>
                                            {
                                                prog_textBoxes[i].ForeColor = Color.White;
                                                prog_textBoxes[i].BackColor = Color.DarkCyan;
                                            }));
                                        }
                                        else
                                        {
                                            prog_textBoxes[i].Invoke((MethodInvoker)(() =>
                                            {
                                                prog_textBoxes[i].ForeColor = Color.White;
                                                prog_textBoxes[i].BackColor = Color.Firebrick;
                                            }));
                                        }
                                    }
                                    prog_textBoxes[i].Text = progs[i].message;
                                }));
                                checkerThread.Start();
                            }
                        }
                    }
                    else
                    {
                        message("File Error: Could not gain access.");
                    }
                }
                else
                {
                    message("File Error: Timed out.");
                    num_of_progs = 0;
                }
                task.Dispose();
            }
            else
            {
                Console.WriteLine(String.Format("File Error: {0} not nound.", path));
            } 
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
            if (num_of_progs == 0)
            {
                connectProgs(this, null);
            }
            else
            {
                uploadWorker.RunWorkerAsync();
            }
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

        private bool performCore()
        {
            path_to_hex = File.ReadAllText(System.IO.Directory.GetCurrentDirectory() + "\\batches\\hex_path.txt");

            if (path_to_hex.Length == 0)
            {
                MessageBox.Show("No HEX File Selected", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                hexToolStripMenuItem_Click(this, null);
                return false;
            }
            String root = System.IO.Directory.GetCurrentDirectory();

            bool success = false;
            for (int i = 0; i < num_of_progs; i++)
            {
                Thread uploadThread = new Thread(new ThreadStart(() =>
                {
                    if (progs[i].active && progs[i].hasBoard && progs[i].program(path_to_hex))
                    {
                        prog_textBoxes[i].Invoke((MethodInvoker)(() =>
                        {
                            prog_textBoxes[i].ForeColor = Color.White;
                            prog_textBoxes[i].BackColor = Color.DarkCyan;
                            prog_textBoxes[i].Text = progs[i].message;
                        }));
                        success = true;
                    }
                }));
                uploadThread.Start();
            }
            if (!success)
            {
                message("Error: No Board(s)");
                return false;
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
            return true;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            connectProgs();
            uploadWorker.ReportProgress(20);
            if (num_of_progs > 0)
            {
                performCore();
            }
            else
            {
                uploadWorker.ReportProgress(0);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            message("Upload Successful");
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
            richTextBox1.Invoke((MethodInvoker)(() =>
            {
                if (richTextBox1.Text.Length > richTextBox1.MaxLength - 100)
                {
                    richTextBox1.Text = "";
                }
                DateTime time = new DateTime(3057, 3, 14);
                richTextBox1.Text += String.Format("{0}: {1}\n", DateTime.Now.ToString("hh:mm:sstt"), text);
            }));
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
