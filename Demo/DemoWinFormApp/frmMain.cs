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
        private const string LicenseFile = "license.lic";
        byte[] _certPubicKeyData;
        public frmMain()
        {
            InitializeComponent();
        }


        private void frmMain_Shown(object sender, EventArgs e)
        {
            var licenseExists = File.Exists(LicenseFile);
            if (licenseExists)
            {
                _ValidateLicense();
            }
            else
            {
                GetLicenseAndValidate();
            }

            //licInfo.ShowLicenseInfo(_lic);
        }

        private void _ValidateLicense()
        {
            byte[] certPubKeyData = GetPublicKey();
            var licenseString = File.ReadAllText(LicenseFile);
            var license = DeserializeLicenseEntity<MyLicense>(licenseString);

            var RsaIsValid = LicenseHandler.CheckRSA(certPubKeyData, licenseString);

            if (RsaIsValid)
            {
                bool licenseIsStillValid = CheckLicenseStillValid(licenseString);
                if (licenseIsStillValid)
                {
                    bool licenseAlreadyActivated = CheckLicenseIsActivated();
                    if (licenseAlreadyActivated)
                    {
                        // Start Program normally
                    }
                    else
                    {
                        StartLicenseActivationProcess(license);
                    }
                }
                else
                {
                    MessageBox.Show("Lizenz abgelaufen, bitte fordern Sie eine neue Lizenz an");
                    GetLicenseAndValidate();
                }
            }
            else
            {
                MessageBox.Show("Lizenz ungültig, bitte fordern Sie eine neue Lizenz an");
                GetLicenseAndValidate();
            }
            //throw new NotImplementedException();
        }

        private void GetLicenseAndValidate()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Lizenz Datei (*.lic) | *.lic"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                File.Copy(dialog.FileName, LicenseFile);
                _ValidateLicense();
            }
            else
            {
                Application.Exit();
            }
        }

        private void StartLicenseActivationProcess(MyLicense license)
        {
            string userId = GetIdFromUser();
            bool userIdIsValid = ValidateUserId(userId, license.UID);

            if (userIdIsValid)
            {
                var serverBlacklistStatus = CheckUserIsNotOnBlacklist(license.UID);
                if (serverBlacklistStatus == ServerBlackListStatus.NOT_BANNED)
                {
                    PersistLicenseActivation();
                }
                else if (serverBlacklistStatus == ServerBlackListStatus.NO_CONNECTION)
                {
                    MessageBox.Show("Zur Lizenzaktivierung wird eine Internetverbindung benötigt");
                    Application.Exit();
                }
                else if (serverBlacklistStatus == ServerBlackListStatus.BANNED)
                {
                    MessageBox.Show("Aktivierung fehlgeschlagen");
                    Application.Exit();
                }
            }
            else
            {
                MessageBox.Show("Eingegebene Id stimmt mit Lizenz nicht überein");
                StartLicenseActivationProcess(license);
                return;
            }

            throw new NotImplementedException();
        }

        private void PersistLicenseActivation()
        {
            Properties.Settings.Default["IsActivated"] = true;
            Properties.Settings.Default.Save();
        }

        private ServerBlackListStatus CheckUserIsNotOnBlacklist(string uID)
        {
            // TODO: implement
            // Make a call to the server to check wheter user is banned or not
            return ServerBlackListStatus.NOT_BANNED;
        }

        private bool ValidateUserId(string userId, string xmlSha)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var userSha = SHA256_Util.GetSHA256(userId);
            var idIsValid = userId == xmlSha;

            return idIsValid;
        }

        private string GetIdFromUser()
        {
            var rawId = Interaction.InputBox(
                "Geben Sie zur Lizenzaktivieung ihre Nutzer-ID (Firmenname) ein",
                "ID-Abfrage",
                "Nutzer-ID"
            );
            var userId = rawId.Trim();
            return userId;
        }

        private bool CheckLicenseIsActivated()
        {
            var isActivated = bool.Parse(Properties.Settings.Default["IsActivated"].ToString());
            return isActivated;
        }

        private bool CheckLicenseStillValid(string licenseString)
        {
            var _lic = DeserializeLicenseEntity<MyLicense>(licenseString);

            DateTime ExpireDate = _lic.ExpirationDate.Date.Add(new TimeSpan(0, 23, 59, 59, 999));
            int remainingDays = (int)(ExpireDate - DateTime.Now).TotalDays;

            if (_lic.ExpirationDate == DateTime.MinValue)
            {
                // Lifetime License
                return true;
            }
            else if (remainingDays <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private T DeserializeLicenseEntity<T>(string licenseString) where T : LicenseEntity
        {
            T _lic;

            XmlDocument xmlDoc = new XmlDocument
            {
                PreserveWhitespace = true
            };
            // Load an XML file into the XmlDocument object.
            xmlDoc.LoadXml(Encoding.UTF8.GetString(Convert.FromBase64String(licenseString)));

            XmlNodeList nodeList = xmlDoc.GetElementsByTagName("Signature");
            xmlDoc.DocumentElement.RemoveChild(nodeList[0]);

            var _licXML = xmlDoc.OuterXml;

            //Deserialize license
            XmlSerializer _serializer = new XmlSerializer(typeof(LicenseEntity), new Type[] { typeof(MyLicense) });
            using (StringReader _reader = new StringReader(_licXML))
            {
                _lic = (T)_serializer.Deserialize(_reader);
            }
            return _lic;
        }

        private byte[] GetPublicKey()
        {
            byte[] certPubKeyData;

            Assembly _assembly = Assembly.GetExecutingAssembly();
            using (MemoryStream _mem = new MemoryStream())
            {
                _assembly.GetManifestResourceStream("DemoWinFormApp.LicenseVerify.cer").CopyTo(_mem);

                certPubKeyData = _mem.ToArray();
            }

            return certPubKeyData;
        }

        // TODO: Implement: replace with GET Request from the server
        private static ServerBlackListStatus CheckBlacklist()
        {
            return ServerBlackListStatus.NOT_BANNED;
        }

        private void ValidateLicense(ref MyLicense _lic, ref string _msg, ref LicenseStatus _status)
        {
            string licensePath = LicenseFile;

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
                    File.Copy(licensePath, Path.Combine(Application.StartupPath, LicenseFile), true);
                    ValidateLicense(ref _lic, ref _msg, ref _status);
                    return;
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
                MessageBox.Show("Lizenz ungültig, bitte beantragen Sie eine Lizenz");
                licensePath = RequestLicenseFile();

                if (licensePath == null)
                {
                    Application.Exit();
                }
                else
                {
                    ValidateLicense(ref _lic, ref _msg, ref _status);
                    return;
                }
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