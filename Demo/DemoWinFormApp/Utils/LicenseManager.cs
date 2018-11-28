using DemoLicense;
using Microsoft.VisualBasic;
using QLicense;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace DemoWinFormApp.Utils
{
    public class LicenseManager
    {
        private string _licenseFile;
        private IFileHandler _fileHandler;


        public LicenseManager(string licenseFile, IFileHandler fileHandler = null)
        {
            _licenseFile = licenseFile;
            _fileHandler = fileHandler ?? new FileHandler();
        }
        public void CheckLicense()
        {
            var licenseExists = _fileHandler.FileExists(_licenseFile);
            if (licenseExists)
            {
                ValidateLicense();
            }
            else
            {
                MessageBox.Show("Geben Sie den Pfad zur Lizenzdatei an");
                GetLicenseAndValidate();
            }
        }

        public void ValidateLicense()
        {
            byte[] certPubKeyData = _fileHandler.GetPublicKey();
            var licenseString = _fileHandler.ReadAllText(_licenseFile);
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

        public void GetLicenseAndValidate()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Lizenz Datei (*.lic) | *.lic"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                File.Copy(dialog.FileName, _licenseFile);
                ValidateLicense();
            }
            else
            {
                Application.Exit();
            }
        }

        public void StartLicenseActivationProcess(MyLicense license)
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

        public void PersistLicenseActivation()
        {
            Properties.Settings.Default["IsActivated"] = true;
            Properties.Settings.Default.Save();
        }

        public ServerBlackListStatus CheckUserIsNotOnBlacklist(string uID)
        {
            // TODO: implement
            // Make a call to the server to check wheter user is banned or not
            return ServerBlackListStatus.NOT_BANNED;
        }

        public bool ValidateUserId(string userId, string xmlSha)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var userSha = SHA256_Util.GetSHA256(userId);
            var idIsValid = userId == xmlSha;

            return idIsValid;
        }

        public string GetIdFromUser()
        {
            var rawId = Interaction.InputBox(
                "Geben Sie zur Lizenzaktivieung ihre Nutzer-ID (Firmenname) ein",
                "ID-Abfrage",
                "Nutzer-ID"
            );
            var userId = rawId.Trim();
            return userId;
        }

        public bool CheckLicenseIsActivated()
        {
            var isActivated = bool.Parse(Properties.Settings.Default["IsActivated"].ToString());
            return isActivated;
        }

        public bool CheckLicenseStillValid(string licenseString)
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

        public T DeserializeLicenseEntity<T>(string licenseString) where T : LicenseEntity
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

        // TODO: Implement: replace with GET Request from the server
        public static ServerBlackListStatus CheckBlacklist()
        {
            return ServerBlackListStatus.NOT_BANNED;
        }

    }
}
