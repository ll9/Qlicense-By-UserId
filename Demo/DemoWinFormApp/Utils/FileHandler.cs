using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DemoWinFormApp.Utils
{
    public class FileHandler : IFileHandler
    {
        public bool FileExists(string file)
        {
            return File.Exists(file);
        }
    }
}
