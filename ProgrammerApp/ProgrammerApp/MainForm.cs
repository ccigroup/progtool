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
        public string path_to_avrdude = "";
        public static int num_of_progs = 0;
        public static USBTinyISP[] progs;
        public static Label[] prog_labels;
        public static RichTextBox[] prog_textBoxes;
        public static ToolStripMenuItem[] prog_menuitems;
        public static System.Windows.Forms.Timer progressTimer = new System.Windows.Forms.Timer();

        public MainForm()
        {
            InitializeComponent();
            uploadWorker = new BackgroundWorker();
            uploadWorker.WorkerReportsProgress = true;
            uploadWorker.ProgressChanged += worker_ProgressChanged;
            uploadWorker.DoWork += worker_DoWork;
            uploadWorker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        public void reinitializeComponent()
        {
            this.Controls.Clear();
            InitializeComponent();
            Form1_Load(this, null);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            String path = System.IO.Directory.GetCurrentDirectory() + "\\batches\\avrdude_path.txt";
            if (!File.Exists(path))
            {
                File.WriteAllText(path, String.Empty);
            }
            path_to_avrdude = File.ReadAllText(path);
            //Console.WriteLine("Path length " + path_to_avrdude.Length);
            if (path_to_avrdude.Length == 0)
            {
                hexToolStripMenuItem_Click(this, null);
            }
            //Console.WriteLine("Avdude path is " + path_to_avrdude);

            path = System.IO.Directory.GetCurrentDirectory() + "\\batches\\hex_path.txt";
            if (!File.Exists(path))
            {
                File.WriteAllText(path, String.Empty);
            }
            path_to_hex = File.ReadAllText(path);
            //Console.WriteLine("Path length " + path_to_hex.Length);
            if (path_to_hex.Length == 0)
            {
                hexToolStripMenuItem_Click(this, null);
            }
            //Console.WriteLine("Hex path is " + path_to_hex);

            connectProgs();
            message(String.Format("{0} programmers connected.", num_of_progs));
            //Console.WriteLine("{0} progs found", num_of_progs);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reinitializeComponent();
        }

        public void connectProgs()
        {
            int tempId = 0;
            USBTinyISP tempProg = new USBTinyISP(tempId, path_to_avrdude);
            String root = System.IO.Directory.GetCurrentDirectory();
            String target = root + "\\batches\\check.bat";
            String args = String.Format("{0}", tempId);

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
                            toolStripMenuItem2.Enabled = false;
                            
                        }
                        else {
                            //deviceToolStripMenuItem.DropDownItems.Remove(refreshMenuItem);

                            progs = new USBTinyISP[num_of_progs];
                            prog_labels = new Label[num_of_progs];
                            prog_textBoxes = new RichTextBox[num_of_progs];
                            prog_menuitems = new ToolStripMenuItem[num_of_progs];

                            for (int i = 0; i < num_of_progs; i++)
                            {
                                //Thread checkerThread = new Thread(new ThreadStart(() =>
                                {
                                    button1.Text = "Program";
                                    toolStripMenuItem2.Enabled = true;
                                    int index = text.IndexOf("Found USBtinyISP");
                                    //Console.WriteLine("Index is {0}", index);
                                    int id = 0;
                                    if (index < 0) id = i;
                                    else
                                    {
                                        text = text.Substring(index+48);
                                        //Console.WriteLine("Parsed text is \n {0}", text);
                                        id  = Int32.Parse(text.Substring(0, 4));
                                    }
                                    //Console.WriteLine("id is {0}", id);

                                    addProgrammer(i, id);

                                    if (progs[i].active)
                                    {
                                        if (progs[i].connected())
                                        {
                                            prog_textBoxes[i].Invoke((MethodInvoker)(() =>
                                            {
                                                prog_textBoxes[i].ForeColor = Color.White;
                                                prog_textBoxes[i].BackColor = Color.DarkCyan;
                                                prog_textBoxes[i].Text = progs[i].message;
                                                
                                            }));
                                        }
                                        else
                                        {
                                            prog_textBoxes[i].Invoke((MethodInvoker)(() =>
                                            {
                                                prog_textBoxes[i].ForeColor = Color.White;
                                                prog_textBoxes[i].BackColor = Color.Firebrick;
                                                 prog_textBoxes[i].Text = progs[i].message;
                                            }));
                                        }
                                    }
                                }//));
                                //checkerThread.Start();
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
                //Console.WriteLine(String.Format("File Error: {0} not nound.", path));
            } 
        }

        private void addProgrammer(int i, int id)
        {
            progs[i] = new USBTinyISP(id, path_to_avrdude);

            prog_labels[i] = new Label();
            Point loc = new Point(5, this.Height - 35);
            prog_labels[i].Location = loc;
            prog_labels[i].AutoSize = true;
            prog_labels[i].ForeColor = SystemColors.ControlText;
            prog_labels[i].Text = String.Format("P{0}:", i);
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
                    //connectProgs();
                }

            });
            deviceToolStripMenuItem.DropDownItems.Add(prog_menuitems[i]);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (num_of_progs == 0)
            {
                refreshToolStripMenuItem_Click(this, null);
            }
            else
            {
                button1.Text = "Please Wait";
                if(!uploadWorker.IsBusy)
                {
                    uploadWorker.RunWorkerAsync();
                }
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
            uploadWorker.ReportProgress(10);
            int progress = 10;
            Parallel.For(0, num_of_progs,
                   i =>
                   {
                       progress += 80 / num_of_progs;
                       uploadWorker.ReportProgress(progress);
                       //Console.WriteLine("Creating thread for P{0}", progs[i].id);
                       bool success = false;
                       //Console.WriteLine("<<<<{0}>>>...", i);
                       if (progs[i].connected()) {
                           while (!success && progs[i].attempts < 5)
                           {
                               if (i == 0)
                               {
                                   //Console.WriteLine("P{0} progress is {1}", progs[i].id, progress);
                                   uploadWorker.ReportProgress(progress);
                               }
                               if (progs[i].active && progs[i].hasBoard && progs[i].program(path_to_hex))
                               {
                                   //Console.WriteLine("P{0} is active and has a board and programmed successfully...", progs[i].id);
                                   prog_textBoxes[i].Invoke((MethodInvoker)(() =>
                                   {
                                       prog_textBoxes[i].ForeColor = Color.White;
                                       prog_textBoxes[i].BackColor = Color.ForestGreen;
                                       prog_textBoxes[i].Text = progs[i].message;
                                   }));
                                   success = true;
                               }
                               else
                               {
                                   progs[i].attempts++;
                                   prog_textBoxes[i].Invoke((MethodInvoker)(() =>
                                   {
                                       prog_textBoxes[i].ForeColor = Color.White;
                                       prog_textBoxes[i].BackColor = Color.Firebrick;
                                       prog_textBoxes[i].Text = progs[i].message;
                                       message(String.Format("Retrying upload on P{0}: Attempt {1}", progs[i].id, progs[i].attempts));
                                   }));
                                   //Console.WriteLine("P{0} failed.", progs[i].id);
                               }
                               //Console.WriteLine("<<<LOOP>>>");
                           }
                           //Console.WriteLine("Processing {0} on thread {1}", i, Thread.CurrentThread.ManagedThreadId);
                           if (progs[i].hasSuccess)
                           {
                               message(String.Format("Uploaded {0} on P{1}", path_to_hex, progs[i].id));
                           }
                           else
                           {
                               message(String.Format("Failed to upload on P{0}", progs[i].id));
                           }
                       }
                       else 
                        {
                            prog_textBoxes[i].Invoke((MethodInvoker)(() =>
                            {
                                prog_textBoxes[i].ForeColor = Color.White;
                                prog_textBoxes[i].BackColor = Color.Firebrick;
                                prog_textBoxes[i].Text = progs[i].message;
                            }));
                        }
                   });
            return true;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
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
            //Console.WriteLine("Progress changed to {0}", e.ProgressPercentage);
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Text = "Program";
            progressBar1.Value = 100;
            Thread.Sleep(1000);
            progressBar1.Value = 0;
        }


        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            
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
                richTextBox1.Text += String.Format("{0}: {1}\n", DateTime.Now.ToString("hh:mm:sstt"), text);
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
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
            //Console.WriteLine(path);
            System.Diagnostics.Process.Start(path);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            String root = System.IO.Directory.GetCurrentDirectory();
            String path = root + "\\srcs\\log.txt";

            String text = richTextBox1.Text.Replace("\n", Environment.NewLine);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, String.Empty);
            }
            if (File.Exists(path))
            {
                File.AppendAllText(path, String.Format("Export {0}", DateTime.Now.ToString() + Environment.NewLine));
                File.AppendAllText(path, String.Format("{0}", text + Environment.NewLine));
                message("Log Exported to \\src\\log.txt");
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            // Displays an OpenFileDialog so the user can select a Cursor.
            OpenFileDialog openFileDialog2 = new OpenFileDialog();
            //openFileDialog1.Filter = "";
            openFileDialog2.Title = "Select AVRDUde";

            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                path_to_avrdude = openFileDialog2.FileName;
                String path = System.IO.Directory.GetCurrentDirectory() + "\\batches\\avrdude_path.txt";
                File.WriteAllText(path, path_to_avrdude);
                message(String.Format("Path to avrdude is {0}", path_to_avrdude));
            }
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
                ////Console.WriteLine("New path to hex is {0}", path_to_hex);
                message(String.Format("Path to hex is  {0}", path_to_hex));
            }
        }

    }
}
