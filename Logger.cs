using gisgmp_signer.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace gisgmp_signer
{
    class Logger
    {
        private static Logger instance;

        public static Logger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Logger();
                }
                return instance;
            }
        }


        private StreamWriter logFile;
        private MainForm mainForm;
        private Service1 service1;
        private Settings settings = Settings.Default;
        public string logFileFullName;
        public bool logToFile = true;

        private Logger(String logFileName = "")
        {
            if (logFileName == "") {
                logFileName = "signer_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            }
            logFileName += ".log";

            string folderLog = Settings.Default.FolderLog;
            if (folderLog.StartsWith("."))
            {
                folderLog = Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + folderLog;
            }
            if (folderLog == String.Empty)
            {
                folderLog = Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar;
            }
            logFileFullName = folderLog != String.Empty && Directory.Exists(folderLog)
                ? folderLog + Path.DirectorySeparatorChar + logFileName
                : logFileName;

            logFile = new StreamWriter(logFileFullName, false, Encoding.UTF8);
            logFile.AutoFlush = true;
        }

        public void LogToService1(Service1 _service1)
        {
            service1 = _service1;
        }

        public void LogToMainForm(MainForm _mainForm)
        {
            mainForm = _mainForm;
        }

        public string Log(string str, bool asIs = false)
        {
            string fullString = asIs ? str : DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + " - " + str;
            if (mainForm != null)
            {
                Thread.Sleep(0);
                if (mainForm.tbLog.InvokeRequired)
                    mainForm.tbLog.BeginInvoke((MethodInvoker)(() => mainForm.tbLog.AppendText(fullString + Environment.NewLine)));
                else
                    mainForm.tbLog.AppendText(fullString + Environment.NewLine);
            }
            if (service1 != null)
            {
                service1.PoolToPipe(fullString);
            }
            if (logToFile)
            {
                logFile.WriteLine(fullString);
            }
            return fullString;
        }

        public string GetFullLog()
        {
            return new StreamReader(new FileStream(logFileFullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)).ReadToEnd();
        }
    }
}
