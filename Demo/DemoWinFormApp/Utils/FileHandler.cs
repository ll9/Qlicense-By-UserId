using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DemoWinFormApp.Utils
{
    public class FileHandler : IFileHandler
    {

        public bool FileExists(string file)
        {
            return File.Exists(file);
        }

        public byte[] GetPublicKey()
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

        public string ReadAllText(string file)
        {
            return File.ReadAllText(file);
        }
    }
}
