using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using QLicense;
using DemoLicense;

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

            switch (_status)
            {
                case LicenseStatus.VALID:
                    DateTime ExpireDate = _lic.ExpirationDate.Date.Add(new TimeSpan(0, 23, 59, 59, 999));
                    int remainingDays = (int)(ExpireDate - DateTime.Now).TotalDays;

                    if (_lic.ExpirationDate == DateTime.MinValue)
                    {
                        // Lifetime License
                    }
                    else if (remainingDays <= 0)
                    {
                        MessageBox.Show("Your license expired. Please renew");
                        OpenLicenseActivationForm();
                    }
                    else if (remainingDays <= 14)
                    {
                        MessageBox.Show($"Your license will expire in {remainingDays} days.\nPlease renew your license in time.");
                    }
                    //TODO: If license is valid, you can do extra checking here
                    //TODO: E.g., check license expiry date if you have added expiry date property to your license entity
                    //TODO: Also, you can set feature switch here based on the different properties you added to your license entity 

                    //Here for demo, just show the license information and RETURN without additional checking       
                    licInfo.ShowLicenseInfo(_lic);

                    return;

                default:
                    //for the other status of license file, show the warning message
                    //and also popup the activation form for user to activate your application
                    MessageBox.Show(_msg, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    OpenLicenseActivationForm();
                    break;
            }
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
