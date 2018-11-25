﻿using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace QLicense.Windows.Controls
{
    public delegate void LicenseSettingsValidatingHandler(object sender, LicenseSettingsValidatingEventArgs e);
    public delegate void LicenseGeneratedHandler(object sender, LicenseGeneratedEventArgs e);

    public partial class LicenseSettingsControl : UserControl
    {

        public event LicenseSettingsValidatingHandler OnLicenseSettingsValidating;
        public event LicenseGeneratedHandler OnLicenseGenerated;

        protected LicenseEntity _lic;

        public LicenseEntity License
        {
            set
            {
                _lic = value;
                pgLicenseSettings.SelectedObject = _lic;
            }
        }

        public byte[] CertificatePrivateKeyData { set; private get; }

        public SecureString CertificatePassword { set; private get; }

        public bool AllowVolumeLicense
        {
            get
            {
                return grpbxLicenseType.Enabled;
            }
            set
            {
                if (!value)
                {
                    rdoSingleLicense.Checked = true;
                }

                grpbxLicenseType.Enabled = value;
            }
        }

        public LicenseSettingsControl()
        {
            InitializeComponent();
        }


        private void LicenseTypeRadioButtons_CheckedChanged(object sender, EventArgs e)
        {
            txtUID.Text = string.Empty;

            txtUID.Enabled = rdoSingleLicense.Checked;
        }

        private void btnGenLicense_Click(object sender, EventArgs e)
        {
            if (_lic == null) throw new ArgumentException("LicenseEntity is invalid");

            if (rdoSingleLicense.Checked)
            {
                if (string.IsNullOrEmpty(txtUID.Text.Trim()))
                {
                    MessageBox.Show("Benutzer ID / Firmenname darf nicht leer sein");
                }
                else
                {
                    _lic.Type = LicenseTypes.Single;
                    var uid = txtUID.Text.Trim();

                    var sha = SHA256_Util.GetSHA256(uid);
                    _lic.UID = sha;
                }
            }
            else if (rdoVolumeLicense.Checked)
            {
                _lic.Type = LicenseTypes.Volume;
                _lic.UID = string.Empty;
            }


            if (OnLicenseSettingsValidating != null)
            {
                LicenseSettingsValidatingEventArgs _args = new LicenseSettingsValidatingEventArgs() { License = _lic, CancelGenerating = false };

                OnLicenseSettingsValidating(this, _args);

                if (_args.CancelGenerating)
                {
                    return;
                }
            }

            if (OnLicenseGenerated != null)
            {
                string _licStr = LicenseHandler.GenerateLicenseBASE64String(_lic, CertificatePrivateKeyData, CertificatePassword);

                OnLicenseGenerated(this, new LicenseGeneratedEventArgs() { LicenseBASE64String = _licStr });
            }
        }
    }
}
