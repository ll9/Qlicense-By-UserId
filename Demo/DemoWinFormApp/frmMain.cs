using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using QLicense;
using DemoLicense;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using DemoWinFormApp.Utils;

namespace DemoWinFormApp
{
    public enum ServerBlackListStatus
    {
        NOT_BANNED,
        NO_CONNECTION,
        BANNED
    }

    public partial class frmMain : Form
    {
        private LicenseManager _licenseManager;
        private const string LicenseFile = "license.lic";
        byte[] _certPubicKeyData;

        public frmMain()
        {
            _licenseManager = new LicenseManager(LicenseFile);

            InitializeComponent();
        }


        private void frmMain_Shown(object sender, EventArgs e)
        {
            _licenseManager.CheckLicense();

            //licInfo.ShowLicenseInfo(_lic);
        }




        private void DeleteLicenseButton_Click_1(object sender, EventArgs e)
        {
            File.Delete(LicenseFile);
        }

        private void ResetSettingsButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
        }
    }
}