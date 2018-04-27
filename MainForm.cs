using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Configuration.Install;
using System.ServiceProcess;
using gisgmp_signer.Properties;
using System.IO.Pipes;
using System.Configuration;
using System.Collections.Specialized;

namespace gisgmp_signer
{
    public partial class MainForm : Form
    {
        private Signer signer;
        ServiceController serviceController;
        private Settings settings = Settings.Default;
        Thread logServiceThread;
        private bool stopLogServiceThread = false;

        private void Log(string msg)
        {
            Logger.Instance.Log(msg);
        }

        private void LogAsIs(string msg)
        {
            Logger.Instance.Log(msg, true);
        }

        public MainForm()
        {
            InitializeComponent();

            Logger.Instance.LogToMainForm(this);

            bool serviceRunning = true;

            using (ServiceController serviceController = new ServiceController("GISGMPXMLPacketSigner"))
            {
                try
                {
                    ServiceControllerStatus serviceControllerStatus = serviceController.Status;
                    // если ошибки небыло, то служба запущена и нужно получить лог этой службы
                    if (serviceControllerStatus == ServiceControllerStatus.Running)
                    {
                        logServiceThread = new Thread(LogServiceThread);
                        logServiceThread.IsBackground = true;
                        logServiceThread.Start();
                        serviceRunning = false;
                    }
                }
                catch
                { // служба не установлена
                    serviceRunning = false;
                }
            }

            if (!serviceRunning)
            {
                Log("начинаем проверку настроек");

                SettingsForm.CheckDirectory();

                if (settings.DSOV == String.Empty)
                {
                    Log("не настроен сертификат органа власти");
                }
                if (settings.DSSP == String.Empty)
                {
                    Log("не настроен сертификат для служебного пользования");
                }

                Log("проверка настроек завершена");
            }
        }

        private void bSettings_Click(object sender, EventArgs e)
        {
            (new SettingsForm()).ShowDialog();
        }

        private void bStartService_Click(object sender, EventArgs e)
        {
            serviceController = new ServiceController("GISGMPXMLPacketSigner");
            try
            {
                ServiceControllerStatus status = serviceController.Status;
                switch (status) {
                    case ServiceControllerStatus.Running:
                        miStartService.Enabled = false;
                        miStopService.Enabled = true;
                        break;
                    case ServiceControllerStatus.Stopped:
                        miStartService.Enabled = true;
                        miStopService.Enabled = false;
                        break;
                    case ServiceControllerStatus.Paused:
                    case ServiceControllerStatus.ContinuePending:
                    case ServiceControllerStatus.PausePending:
                    case ServiceControllerStatus.StartPending:
                    case ServiceControllerStatus.StopPending:
                        miStartService.Enabled = false;
                        miStopService.Enabled = false;
                        break;
                }
            }
            catch //(Exception E)
            {
                miStartService.Enabled = true;
                miStopService.Enabled = false;
                //                Log(E.Message);
            }

            cmsServiceMenu.Show(bStartService, new Point(0, -77/*-cmsServiceMenu.Size.Height*/));
        }

        private void bSign_Click(object sender, EventArgs e)
        {
            if ((Settings.Default.FolderIn == String.Empty) || (Settings.Default.FolderOut == String.Empty))
            {
                Log("не указан каталог подписываемых и/или подписанных файлов");
                return;
            }

            string[] files = Directory.GetFiles(Settings.Default.FolderIn, "*.xml");

            if (files.Length > 0)
            {
                new Thread(SignFilesThread).Start();
            } else
            {
                Log("нет файлов для подписания");
            }
        }

        private void SignFilesThread()
        {
            signer = new Signer();
            signer.SignAllFiles(Settings.Default.FolderIn);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.Instance.LogToMainForm(null);
            stopLogServiceThread = true;
        }

        private void miStartService_Click(object sender, EventArgs e)
        {
            try
            {
                ServiceControllerStatus Status = serviceController.Status;
            }
            catch //(Exception E)
            {
                ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                //                Log(E.Message);
            }
            /*ServiceController controller = new ServiceController("GisGMP packet signer");*/
            serviceController.Start();
            if (logServiceThread == null)
            {
                Thread.Sleep(1000);
                logServiceThread = new Thread(LogServiceThread);
                logServiceThread.IsBackground = true;
                logServiceThread.Start();
            }
        }

        private void miStopService_Click(object sender, EventArgs e)
        {
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                new Thread(StopServiceThread).Start();
//                Log("Служба остановлена");
                
//                miStartService.Enabled = false;
//                miStopService.Enabled = false;
            }
        }

        private void StopServiceThread()
        {
            serviceController.Stop();
            ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location /*+ " /service"*/ });
            // miStartService.Enabled = true;
            // this.miStartService.BeginInvoke((System.Windows.Forms.MethodInvoker)(() => mainForm.tbLog.AppendText(now + " - " + str + Environment.NewLine)));
        }

        private void LogServiceThread()
        {
            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "GISGMPXMLPacketSignerPipe", PipeDirection.InOut, PipeOptions.Asynchronous);

            if (pipeClient.IsConnected != true)
            {
                pipeClient.Connect();
            }

            // Logger.Instance.logToFile = false;
            StreamReaderWriter stream = new StreamReaderWriter(pipeClient);

            if (stream.ReadLine() == "How are you?")
            {
                stream.WriteLine("I am Client! ept!");
                stream.Flush();
                pipeClient.WaitForPipeDrain();

                /*string s = stream.ReadLine();*/

                tbLog.BeginInvoke((MethodInvoker)(() => tbLog.Clear()));

                Logger.Instance.logToFile = false;

                while (pipeClient.IsConnected && !stopLogServiceThread)
                {
                    string str = stream.ReadLine();
                    if (str != String.Empty)
                    {
                        LogAsIs(str);
                    }
                }

                if (pipeClient.IsConnected)
                {
                    stream.WriteLine("Goodbye!");
                    stream.Flush();
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }

        }

        private void miSettingsService_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Для настройки службы необходимо отредактировать файл " + Path.GetFileName(Application.ExecutablePath) + ".config в каталоге с программой");
        }
    }
}
