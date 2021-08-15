using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using AutoTrader;
using TaleWorlds.Library;
using TaleWorlds.Engine;

namespace AutoTraderTests
{
    [TestClass]
    public class AutoTraderConfigTest
    {
        PlatformFilePath _configFile;

        [TestInitialize]
        public void testInit() {
            PlatformFileHelperPC platformFileHelper = new PlatformFileHelperPC("AutoTraderTest");
            Common.PlatformFileHelper = platformFileHelper;

            _configFile = new PlatformFilePath(EngineFilePaths.ConfigsPath, "AutoTraderConfig.xml");

            // Remove config file
            if(FileHelper.FileExists(_configFile))
                FileHelper.DeleteFile(_configFile);
            Assert.IsTrue(!FileHelper.FileExists(_configFile));
        }

        [TestMethod]
        public void TestConfigInit()
        {
            AutoTraderConfig.Initialize();
        }

        [TestMethod]
        public void TestWriteConfig()
        {
            AutoTraderConfig.Initialize();

            Assert.AreEqual(AutoTraderConfig.ArmorTierValue, 2);

            AutoTraderConfig.ArmorTierValue = 3;

            Assert.AreEqual(AutoTraderConfig.ArmorTierValue, 3);

            AutoTraderConfig.Save();

            AutoTraderConfig.Initialize();

            Assert.AreEqual(AutoTraderConfig.ArmorTierValue, 3);
        }
    }
}
