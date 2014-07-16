using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Windows.Forms;
using Adroiti.ErrorHandler;
using Adroiti.Utilities.UI.UserFormEvents;
using Adroiti.Utilities.UI.Utilities;
using Adroiti.Utilities.Validation;
using DevExpress.Data;
using VividGemz.Common.Configuration;
using VividGemz.Common.Database;
using VividGemz.Common.Database.Utils;
using VividGemz.Common.Utilities;
using VividGemz.SkuInfoTool.Forms;
using VividGemz.SkuInfoTool.Registration;

namespace VividGemz.SkuInfoTool  /*     <--- Add space to force new ClickOnce release */
{
    static class Program
    {
        private static UserFormEventMessageFilter userFormEventMessageFilter;

        /// <summary>
        /// The main entry point for the application.
        /// </summary> 
        [STAThread]
        static void Main()
        {
            ExceptionHandler.AddHandler();

            CurrencyDataController.DisableThreadingProblemsDetection = true;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.CurrentCulture = CultureInfo.CreateSpecificCulture(ConfigurationManager.AppSettings["CultureToUse"]);

            var configInfo = ListingToolsConfigInfo.GetInstance();
            configInfo.SetConfigurationForEnvironment();
            configInfo.ReloadInvalidCommonConfigFile();

            var dbConfigInfo = DBConfigInfo.GetInstance();
            dbConfigInfo.GetConfigFromFile();

            if (!dbConfigInfo.CheckDBConfig())
            {
                return;
            }

            MetadataTypesRegister.InstallForThisAssembly();
            WindsorContainerCreator.Get();

            var skuInfoDataContext = new SkuInfoDataClassesDataContext(dbConfigInfo.SkuInfoConnection);
            dbConfigInfo.SkuInfoDataContext = skuInfoDataContext;

            if (ConfigurationManager.AppSettings["DebugSql"] != null)
            {
                skuInfoDataContext.Log = new DebuggerWriter();
            }

            var userFormEventConsumer = new UserFormEventConsumer(new InjectableSkuInfoDataContext(dbConfigInfo.SkuInfoConnection));
            userFormEventMessageFilter = new UserFormEventMessageFilter(userFormEventConsumer);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Application.AddMessageFilter(userFormEventMessageFilter);

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, policyErrors) => true;            

            Application.Run(new SkuInfoForm());
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            userFormEventMessageFilter.Save();
        }

        public static bool CheckDBConfig(this DBConfigInfo dbConfigInfo)
        {
            if (!DBConnectionUtility.CheckSqlConnection(dbConfigInfo.SkuInfoConnection))
            {
                UserXtraDialog.ShowError("Failed to establish connection to SIT database.\n\nPlease verify the correct connection is used.", "Connection attempt failed");

                return dbConfigInfo.UpdateDBConfig();
            }

            return true;
        }
    }
}