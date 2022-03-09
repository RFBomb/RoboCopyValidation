using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
//
namespace RoboCopyValidation
{
    [TestClass]
    public class RoboCopyValidationUnitTests
    {
        static RoboSharpConfiguration Configuration { get; } = new RoboSharpConfiguration();
        string RoboCopyPath => Configuration.RoboCopyExe;

        List<string> LogLines { get; set; }
        List<string> ErrLines { get; set; }

        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine("Settings Up RoboCopy Process");
            LogLines = new List<string>();
            ErrLines = new List<string>();
            using (Process RC = new Process())
            {
                RC.StartInfo.FileName = RoboCopyPath;
                RC.StartInfo.RedirectStandardOutput = true;
                RC.StartInfo.RedirectStandardError = true;
                RC.StartInfo.UseShellExecute = false;
                RC.OutputDataReceived += RC_OutputDataReceived;
                RC.ErrorDataReceived += RC_ErrorDataReceived;
                Console.WriteLine($"- RoboCopy Executable Path: {RoboCopyPath}");

                //Setup Command Options
                string source = Path.Combine(Directory.GetCurrentDirectory(), "SourceFiles");
                string dest = "C:\\DEST";
                RC.StartInfo.Arguments = $"{source} {dest} /S /V /R:0 /W:3";

                DirectoryInfo d = new DirectoryInfo(dest);
                Directory.CreateDirectory(dest);
                if (d.Exists)
                {
                    Console.WriteLine($"Destination Exists : Deleting it to ensure fresh run.");
                    d.Delete(true);
                }

                //Prep the locked file
                string LockedFile = Path.Combine(dest, "1024_Bytes.txt");
                //FileInfo LFile = new FileInfo(LockedFile);
                //LFile.Create();

                Console.WriteLine($"Running the Test");
                Console.WriteLine($"- Creating Destination Directory");
                d.Create();
                Console.WriteLine($"- Locking File Path: {LockedFile}");
                var FStream = new StreamWriter(LockedFile);
                FStream.WriteLine("This File Is Locked");
                //Start
                Console.WriteLine($"- Starting RoboCopy Process");
                bool started = RC.Start();
                Assert.IsTrue(started);
                RC.BeginOutputReadLine();
                RC.BeginErrorReadLine();
                RC.WaitForExit();
                Console.WriteLine($"- RoboCopy Process has Exited");
                //Close stream
                FStream.Close();
                Console.WriteLine("- Unlocking the Locked file path.");

                try
                {
                    //If any of the log lines contains 'ERROR', then this will pass the test.
                    //If no error is reported, Assert will throw.
                    Assert.IsTrue(LogLines.Any(str => str.Contains(Configuration.ErrorToken)));
                    WriteLogLines(null);
                }
                catch (Exception e)
                {
                    WriteLogLines(e);
                }
            }
        }

        private void WriteLogLines(Exception e)
        {
            if (e != null)
            {
                Console.WriteLine($"- Assertion Failed: No Error reported in the log.");
            }

            if (ErrLines.Count > 0)
            {
                Console.WriteLine($"- Error Data Received from Process: \n");
                foreach (string s in LogLines)
                    Console.WriteLine(s);
            }
            else
            {
                Console.WriteLine($"- No Error Data Received from Process.");
            }


            Console.WriteLine($"- Writing Log Lines: \n");
            foreach (string s in LogLines)
                Console.WriteLine(s);
            
            if (e != null) throw e;
        }

        private void RC_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e?.Data != null) 
                ErrLines.Add(e.Data);
        }

        private void RC_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e?.Data != null) 
                LogLines.Add(e.Data);
        }
    }

}
