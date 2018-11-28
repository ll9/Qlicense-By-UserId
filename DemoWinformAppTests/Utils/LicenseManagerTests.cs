using DemoWinFormApp.Utils;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoWinformAppTests.Utils
{
    [TestFixture]
    class LicenseManagerTests
    {
        private const string LicenseFile = "licensfile";
        LicenseManager _licenseManager;
        Mock<IFileHandler> _fileHandler;
       

        [SetUp]
        public void Init()
        {
            _fileHandler = new Mock<IFileHandler>();
            _licenseManager = new LicenseManager(LicenseFile, _fileHandler.Object);
        }

        [Test]
        public void CheckLicense_LicenseExists_ReturnTrue()
        {
            _fileHandler
                .Setup(fh => fh.FileExists(It.IsAny<string>()))
                .Returns(true);

            _licenseManager.CheckLicense();
            _fileHandler.Verify(f => f.FileExists(It.IsAny<string>()));
        }
    }
}
