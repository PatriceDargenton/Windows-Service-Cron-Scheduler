
using System.ComponentModel;

namespace winCron
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {

#if ReleaseInv // Compile with ReleaseInv
        public const bool invisibleUserService = true;
#else // Compile with Release
        public const bool invisibleUserService = false;
#endif 

        public ProjectInstaller()
        {
            InitializeComponent();

#pragma warning disable CS0162 // Inaccessible code
            if (invisibleUserService) { 
                this.serviceProcessInstaller1.Account = 
                    System.ServiceProcess.ServiceAccount.User;
                this.serviceInstaller1.Description = "Windows service cron scheduler (invisible tasks)";
                this.serviceInstaller1.DisplayName = "winCronInv";
                this.serviceInstaller1.ServiceName = "winCronInv";
            }
            else {
                this.serviceProcessInstaller1.Account = 
                    System.ServiceProcess.ServiceAccount.LocalSystem;
                this.serviceInstaller1.Description = "Windows service cron scheduler";
                this.serviceInstaller1.DisplayName = "winCron";
                this.serviceInstaller1.ServiceName = "winCron";
            }
#pragma warning restore CS0162 // Inaccessible code
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;

            

        }
    }
}
