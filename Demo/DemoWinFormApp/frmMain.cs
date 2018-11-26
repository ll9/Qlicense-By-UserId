using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using QLicense;
using DemoLicense;
using Microsoft.VisualBasic;
using System.Diagnostics;

namespace DemoWinFormApp
{
    public partial class frmMain : Form
    {

        byte[] _certPubicKeyData;
        public frmMain()
        {
            InitializeComponent();
        }


        private void frmMain_Shown(object sender, EventArgs e)
        {
            if (Debugger.IsAttached)
                Properties.Settings.Default.Reset();

            //Initialize variables with default values
            MyLicense _lic = null;
            string _msg = string.Empty;
            LicenseStatus _status = LicenseStatus.UNDEFINED;

            //Read public key from assembly
            Assembly _assembly = Assembly.GetExecutingAssembly();
            using (MemoryStream _mem = new MemoryStream())
            {
                _assembly.GetManifestResourceStream("DemoWinFormApp.LicenseVerify.cer").CopyTo(_mem);

                _certPubicKeyData = _mem.ToArray();
            }

            ValidateLicense(ref _lic, ref _msg, ref _status);

            var isActivated = bool.Parse(Properties.Settings.Default["IsActivated"].ToString());
            if (!isActivated)
            {
                string uid;
                do
                {
                    uid = Interaction.InputBox(
                        "Geben Sie zur Lizenzaktivieung ihre Nutzer-ID (Firmenname) ein",
                        "ID-Abfrage",
                        "Nutzer-ID");

                    if (string.IsNullOrEmpty(uid))
                    {
                        Application.Exit();
                    }
                    if (SHA256_Util.GetSHA256(uid) != _lic.UID)
                    {
                        MessageBox.Show("Id stimmt nicht überein");
                    }
                } while (SHA256_Util.GetSHA256(uid) != _lic.UID);
            }

            try
            {
                var isOnBlackList = CheckBlacklist();
                if (isOnBlackList)
                {
                    MessageBox.Show("Lizenzaktivierung fehlgeschlagen");
                    Application.Exit();
                }
            }
            catch
            {
                // No Internet
                MessageBox.Show("Zur Lizenzaktivierung wird eine Internetverbindung benötigt\n" + 
                    "Führen Sie die Aktivierung bei bestehender Internetverbindung erneut aus");
                Application.Exit();
            }

            Properties.Settings.Default["IsActivated"] = true;
            Properties.Settings.Default.Save();
        }

        // TODO: Implement: replace with GET Request from the server
        private static bool CheckBlacklist()
        {
            return false;
        }

        private void ValidateLicense(ref MyLicense _lic, ref string _msg, ref LicenseStatus _status, string licensePath = "license.lic")
        {

            //Check if the XML license file exists
            if (!File.Exists(licensePath))
            {
                MessageBox.Show("Software noch nicht aktiviert. Geben Sie zur Aktivierung den Pfad" +
                    " zur Lizenzdatei an:");

                licensePath = RequestLicenseFile();

                if (licensePath == null)
                {
                    Application.Exit();
                }
                else
                {
                    ValidateLicense(ref _lic, ref _msg, ref _status, licensePath);
                }

            }

            _lic = (MyLicense)LicenseHandler.ParseLicenseFromBASE64String(
                typeof(MyLicense),
                File.ReadAllText(licensePath),
                _certPubicKeyData,
                out _status,
                out _msg);


            if (_status == LicenseStatus.INVALID)
            {
                MessageBox.Show("izenz ungültig, bitte beantragen Sie eine Lizenz");
                licensePath = RequestLicenseFile();

                if (licensePath == null)
                {
                    Application.Exit();
                }
                else
                {
                    ValidateLicense(ref _lic, ref _msg, ref _status, licensePath);
                }
            }

            DateTime ExpireDate = _lic.ExpirationDate.Date.Add(new TimeSpan(0, 23, 59, 59, 999));
            int remainingDays = (int)(ExpireDate - DateTime.Now).TotalDays;

            if (_lic.ExpirationDate == DateTime.MinValue)
            {
                // Lifetime License
            }
            else if (remainingDays <= 0)
            {
                MessageBox.Show("Your license expired. Please renew");
                // TODO RENEW
                licensePath = RequestLicenseFile();

                if (licensePath == null)
                {
                    Application.Exit();
                }
                else
                {
                    ValidateLicense(ref _lic, ref _msg, ref _status, licensePath);
                }
            }
            else if (remainingDays <= 14)
            {
                MessageBox.Show($"Your license will expire in {remainingDays} days.\nPlease renew your license in time.");
            }
        }

        private static string RequestLicenseFile()
        {

            var dialog = new OpenFileDialog()
            {
                Filter = "Lizenz Datei (*.lic) | *.lic"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }

            return null;
        }

        private void OpenLicenseActivationForm()
        {
            using (frmActivation frm = new frmActivation())
            {
                frm.CertificatePublicKeyData = _certPubicKeyData;
                frm.ShowDialog();

                //Exit the application after activation to reload the license file 
                //Actually it is not nessessary, you may just call the API to reload the license file
                //Here just simplied the demo process

                Application.Exit();
            }
        }
    }
}
