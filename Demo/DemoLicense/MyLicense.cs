using Microsoft.VisualBasic;
using QLicense;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DemoLicense
{
    public class MyLicense : QLicense.LicenseEntity
    {
        [DisplayName("Enable Feature 01")]
        [Category("License Options")]        
        [XmlElement("EnableFeature01")]
        [ShowInLicenseInfo(true, "Enable Feature 01", ShowInLicenseInfoAttribute.FormatType.String)]
        public bool EnableFeature01 { get; set; }

        [DisplayName("Enable Feature 02")]
        [Category("License Options")]        
        [XmlElement("EnableFeature02")]
        [ShowInLicenseInfo(true, "Enable Feature 02", ShowInLicenseInfoAttribute.FormatType.String)]
        public bool EnableFeature02 { get; set; }


        [DisplayName("Enable Feature 03")]
        [Category("License Options")]        
        [XmlElement("EnableFeature03")]
        [ShowInLicenseInfo(true, "Enable Feature 03", ShowInLicenseInfoAttribute.FormatType.String)]
        public bool EnableFeature03 { get; set; }

        [DisplayName("Expiration Date")]
        [Category("License Options")]
        [XmlElement("Expiration Date")]
        [ShowInLicenseInfo(true, "Expiration Date", ShowInLicenseInfoAttribute.FormatType.Date)]
        public DateTime ExpirationDate { get; set; }

        public MyLicense()
        {
            //Initialize app name for the license
            this.AppName = "DemoWinFormApp";
        }

        public override LicenseStatus DoExtraValidation(out string validationMsg)
        {
            LicenseStatus _licStatus = LicenseStatus.UNDEFINED;
            validationMsg = string.Empty;



            switch (this.Type)
            {
                case LicenseTypes.Single:
                    //For Single License, check whether UID is matched
                    var isActivated = bool.Parse(Properties.Settings.Default["IsActivated"].ToString());
                    if (isActivated)
                    {
                        _licStatus = LicenseStatus.VALID;
                    }
                    else
                    {
                        var uid = Interaction.InputBox(
                            "Geben Sie zur Lizenzaktivieung ihre Nutzer-ID (Firmenname) ein", 
                            "ID-Abfrage", 
                            "Nutzer-ID");

                        var hash = SHA256_Util.GetSHA256(uid);

                        if (UID == hash)
                        {
                            _licStatus = LicenseStatus.VALID;
                            // TODO: Call home here
                            Properties.Settings.Default["IsActivated"] = true.ToString();
                            Properties.Settings.Default.Save();
                        }
                        else
                        {
                            validationMsg = "Eingegebene ID stimmt nicht mit der Lizenz-ID überein";
                            _licStatus = LicenseStatus.INVALID;
                        }
                    }
                    break;
                case LicenseTypes.Volume:
                    //No UID checking for Volume License
                    _licStatus = LicenseStatus.VALID;
                    break;
                default:
                    validationMsg = "Invalid license";
                    _licStatus= LicenseStatus.INVALID;
                    break;
            }

            return _licStatus;
        }
    }
}
