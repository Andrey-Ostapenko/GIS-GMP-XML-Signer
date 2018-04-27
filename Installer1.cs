using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;

namespace gisgmp_signer
{
    [RunInstaller(true)]
    public partial class Installer1 : Installer
    {
        ServiceInstaller serviceInstaller;
        ServiceProcessInstaller processInstaller;

        public Installer1()
        {
            InitializeComponent();
            serviceInstaller = new ServiceInstaller { StartType = ServiceStartMode.Automatic };
            processInstaller = new ServiceProcessInstaller { Account = ServiceAccount.LocalSystem };
            /**processInstaller = new ServiceProcessInstaller {
                Account = ServiceAccount.User,
                Username = "е.сергеев@finu.local",
                Password = "XC23p5df"
            };/**/
            serviceInstaller.ServiceName = "GISGMPXMLPacketSigner";
            serviceInstaller.DisplayName = "Служба подписания XML пакетов ГИС ГМП";
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        private void Installer1_BeforeInstall(object sender, InstallEventArgs e)
        {
            string parameter = "/service";
            var assemblyPath = Context.Parameters["assemblypath"];
            assemblyPath = @"""" + assemblyPath + "\" " + parameter + "";
            Context.Parameters["assemblypath"] = assemblyPath;
            // base.OnBeforeInstall(savedState);
            /*
                protected override void OnBeforeInstall(IDictionary savedState)
        {                
                string parameter = "YOUR COMMAND LINE PARAMETER VALUE GOES HERE";
                var assemblyPath = Context.Parameters["assemblypath"];
                assemblyPath += @""" "" " + parameter + "";
                Context.Parameters["assemblypath"] = assemblyPath;
                base.OnBeforeInstall(savedState);
        }*/
        }
    }
}
