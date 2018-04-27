using gisgmp_signer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace gisgmp_signer
{
    public partial class SettingsForm : Form
    {
        private static Settings settings = Settings.Default;

        private static bool CheckAndCreateDirectory(String folder, String NotSetDirectoryMsg)
        {
            if (folder == String.Empty)
            {
                Logger.Instance.Log(NotSetDirectoryMsg);
                return false;
            }
            else
            {
                if (!Directory.Exists(folder))
                {
                    try
                    {
                        Directory.CreateDirectory(folder);
                    }
                    catch (Exception E)
                    {
                        Logger.Instance.Log(E.Message);
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool CheckDirectory()
        {
            bool checkIsOk = true;
            if (!CheckAndCreateDirectory(settings.FolderLog, "не настроена директория логов")) checkIsOk = false;
            if (!CheckAndCreateDirectory(settings.FolderIn, "не настроена директория файлов для подписания")) checkIsOk = false;
            if (!CheckAndCreateDirectory(settings.FolderOut, "не настроена директория подписанных файлов")) checkIsOk = false;
            if (!CheckAndCreateDirectory(settings.FolderError, "не настроена директория пропущенных файлов")) checkIsOk = false;
            return checkIsOk;
        }

        public SettingsForm()
        {
            InitializeComponent();
            propertyGrid1.SelectedObject = new Settings();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            (propertyGrid1.SelectedObject as Settings).Save();
            Settings.Default.Reload();
            CheckDirectory();
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
