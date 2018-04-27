using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;

namespace gisgmp_signer
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string[] cmds = Environment.GetCommandLineArgs();

            if (cmds.Count() > 1)
            {
                string inFile = String.Empty, 
                    outFile = String.Empty;
                for (int i = 1; i < cmds.Count(); i++)
                {
                    if (cmds[i].StartsWith("/in:"))
                    {
                        string[] separator = { "/in:" };
                        string[] s = cmds[i].Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        inFile = s[0];
                    }
                    if (cmds[i].StartsWith("/out:"))
                    {
                        string[] separator = { "/out:" };
                        string[] s = cmds[i].Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        outFile = s[0];
                    }
                    if (cmds[i].StartsWith("/service"))
                    {
                        // Стартанем как служба
                        ServiceBase[] ServicesToRun = new ServiceBase[]
                        {
                            new Service1()
                        };
                        ServiceBase.Run(ServicesToRun);
                    }
                }

                if (inFile != String.Empty)
                {
                    try
                    {
                        var signer = new Signer(inFile, outFile);
                        Environment.ExitCode = signer.SignFile() ? 1 : 2;
                    }
                    catch (Exception e)
                    {
                        StreamWriter logFile;
                        logFile = new StreamWriter("signer.log", true, Encoding.UTF8);
                        logFile.WriteLine(DateTime.Now.ToString("HH:mm:ss dd.MM.yyyy => ") + e.Message);
                        logFile.Close();
                        Environment.ExitCode = 3;
                    }
                }
                else
                {

                }
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
