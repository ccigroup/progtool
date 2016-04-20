using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace ProgrammerApp
{
    //enum 
    public class USBTinyISP
    {
        public int id;
        public String status;
        public String message;
        public bool hasBoard;
        public bool active;
        public bool hasSuccess;
        public int attempts;

        public USBTinyISP()
        {
            this.id = 0;
            this.status = "";
            this.message = "";
            this.hasBoard = false;
            this.active = true;
            this.hasSuccess = false;
            this.attempts = 0;
        }
        public USBTinyISP(int id)
        {
            this.id = id;
            this.status = "";
            this.message = "";
            this.hasBoard = false;
            this.active = true;
            this.hasSuccess = false;
            this.attempts = 0;
        }

        public void performBat(String target, String args)
        {
           
            target = target.Replace(@" ", "\" \"");
            String path_to_avrdude = target + "\\..\\..\\tools\\avr\\bin\\";
            String command = "/c " + target + " " + args + " " + path_to_avrdude;
            Console.WriteLine("Running: " + command);
            System.Diagnostics.ProcessStartInfo processInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            System.Diagnostics.Process process = System.Diagnostics.Process.Start(processInfo);

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            //Console.WriteLine("Output: {0}", output);
            //Console.WriteLine("Error: {0}", error);

            process.WaitForExit();
        }

        public bool connected()
        {
            Console.WriteLine("{0}> check", this.id);
            String root = System.IO.Directory.GetCurrentDirectory();
            String target = root + "\\batches\\check.bat";
            String args = String.Format("{0}", this.id);
            string path = root + String.Format("\\batches\\check_results\\check_results_p{0}.txt", this.id);
            var task = Task.Run(() => WaitForFile(path));
            string text = "";
            if (task.Wait(TimeSpan.FromSeconds(5)))
            {
                if (task.Result)
                {
                    File.WriteAllText(path, String.Empty);
                }
            }

            this.performBat(target, args);

            if (!File.Exists(path))
            {
                this.message = "Results Not Found";
            }
            else {
                task = Task.Run(() => WaitForFile(path));
                text = "";
                if (task.Wait(TimeSpan.FromSeconds(5)))
                {
                    if (task.Result)
                    {
                        text = System.IO.File.ReadAllText(path);
                    }
                    else
                    {
                        this.message = "File Error: Could not open";
                        Console.WriteLine(this.message);
                    }
                }
                else
                {
                    this.message = "File Error: Timed out";
                    Console.WriteLine(this.message);
                }
                task.Dispose();
                

                Console.WriteLine("Contents of WriteText.txt = {0}", text);
                if (text.IndexOf("Error") != -1)
                {
                    this.message = "Programmer not found.";
                }
                else if (text.IndexOf("initialization failed, rc=-1") != -1)
                {
                    this.message = "Board not found.";
                }
                else if (text.IndexOf("Fuses OK") != -1)
                {
                    this.message = "Board connected.";
                    this.hasBoard = true;
                    return true;
                }
                else
                {
                    this.message = "Error";
                    return this.connected();
                }
            }
            return false;
        }

        public bool program(String path_to_hex)
        {
            String root = System.IO.Directory.GetCurrentDirectory();
            String target = root + "\\batches\\core.bat";
            path_to_hex = path_to_hex.Replace(@" ", "\" \"");
            String args = String.Format("{0} {1}", this.id, path_to_hex);
            string path = root + String.Format("\\batches\\core_results\\core_results_p{0}.txt", this.id);
            var task = Task.Run(() => WaitForFile(path));
            if (task.Wait(TimeSpan.FromSeconds(5)))
            {
                if (task.Result)
                {
                    File.WriteAllText(path, String.Empty);
                }
            }

            this.performBat(target, args);

            Console.WriteLine("Core results path:" + path);

            if (File.Exists(path))
            {
                task = Task.Run(() => WaitForFile(path));
                if (task.Wait(TimeSpan.FromSeconds(5)))
                {
                    if (task.Result)
                    {
                        string text = System.IO.File.ReadAllText(path);
                        Console.WriteLine("Core Results: " + text);
                        if (text.IndexOf("initialization failed, rc=-1") != -1)
                        {
                            this.message = "Board not found.";
                        }
                        else if (text.IndexOf("invalid file format") != -1)
                        {
                            this.message = "File format error.";
                        }
                        else
                        {
                            this.message = "Upload successful";
                            this.attempts = 0;
                            this.hasSuccess = true;
                            return true;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Results not Found*");
            }
            this.hasSuccess = false;
            return false;
        }


        public bool WaitForFile(String path)
        {
            while (true)
            {
                // If the file can be opened for exclusive access it means that the file
                // is no longer locked by another process.
                try
                {
                    using (FileStream inputStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        if (inputStream.Length > 0)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    //throw new IOException("Waiting for file");
                }
            }
        }
    }
}
