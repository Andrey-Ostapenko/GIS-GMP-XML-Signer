using gisgmp_signer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace gisgmp_signer
{
    partial class Service1 : ServiceBase
    {
        private Settings settings = Settings.Default;
        private Thread pipeThread;
        private bool stopPipeThread = false;
        private NamedPipeServerStream pipeServer = null;
        private StreamReaderWriter stream;
        private FileSystemWatcher watcher;
        private Signer signer;
        private bool stopWatcher = false;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            (new Thread(InitThread)).Start();
        }

        private void InitThread()
        {
            pipeThread = new Thread(PipeThread);
            pipeThread.IsBackground = true;
            pipeThread.Start();

            Logger.Instance.LogToService1(this);

            bool settingsIsOk;

            Log("Запуск службы");

            Log(" проверка настроек");

            settingsIsOk = SettingsForm.CheckDirectory();

            if (settings.DSOV == String.Empty)
            {
                Log("  не настроен сертификат органа власти");
                settingsIsOk = false;
            }
            if (settings.DSSP == String.Empty)
            {
                Log("  не настроен сертификат для служебного пользования");
                settingsIsOk = false;
            }

            try
            {
                signer = new Signer();
            }
            catch (Exception e)
            {
                settingsIsOk = false;
                Log("  " + e.Message);
                Log("  Сертификаты должны быть установлены в личное хранилище локального компьютера!");
            }

            // Log("проверка настроек завершена");

            if (!settingsIsOk)
            {
                Log("Служба не будет запущена!");
                Stop();
            }
            else
            {
                Log("Служба запущена успешно");

                signer.SignAllFiles(settings.FolderIn);

                watcher = new FileSystemWatcher(settings.FolderIn, "*.xml");
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Created += Watcher_CrRn;
                watcher.Renamed += Watcher_CrRn;

                (new Thread(WatcherThread)).Start();
            }
        }

        private void WatcherThread()
        {
            watcher.EnableRaisingEvents = true;
            while (!stopWatcher)
            {
                Thread.Sleep(0);
            }
            watcher.EnableRaisingEvents = false;
        }

        private void Watcher_CrRn(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(1000);
            signer.SignFileAndMoveIt(e.Name);
        }

        protected override void OnStop()
        {
            // TODO: Добавьте код, выполняющий подготовку к остановке службы.
            Log("Служба остановлена");

            stopWatcher = true;
            stopPipeThread = true;

            Thread.Sleep(2000);
        }

        private void Log(string msg)
        {
            Logger.Instance.Log(msg);
        }

        public void PoolToPipe(string str)
        {
            try
            {
                if (pipeServer.IsConnected)
                {
                    stream.WriteLine(str);
                    stream.Flush();
                    pipeServer.WaitForPipeDrain();
                }
            }
            catch (IOException E)
            {
               Log("Не удалось передать сообщение клиенту. Клиент отключился. " + E.Message);
            }
        }

        private void PipeThread()
        {
            while (!stopPipeThread)
            {
                try
                {
                    pipeServer = new NamedPipeServerStream("GISGMPXMLPacketSignerPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                    pipeServer.WaitForConnection();

                    try
                    {
                        stream = new StreamReaderWriter(pipeServer);
                        // жмём руки
                        PoolToPipe("How are you?");
                        if (stream.ReadLine() == "I am Client! ept!")
                        {
                            PoolToPipe(Logger.Instance.GetFullLog());
                            // теперь можно слать текущие сообщения
                            do
                            {
                                Thread.Sleep(0);
                                if (stream.ReadLine() == "Goodbye!")
                                {
                                    pipeServer.Disconnect();
                                }
                            } while (pipeServer.IsConnected);
                        }
                    }
                    catch (Exception E)
                    {
                        Log("Ошибка stream: " + E.Message);
                    }
                    finally
                    {
                        if (stream != null) stream.Dispose();
                    }
                }
                catch (Exception E)
                {
                    Log("Ошибка pipeServer: " + E.Message);
                }
                finally
                {
                    if (pipeServer != null) pipeServer.Dispose();
                }
            }
        }
    }
}
