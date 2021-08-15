using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using AutoTrader;
using TaleWorlds.Library;
using TaleWorlds.Engine;

namespace AutoTraderTests
{
    [TestClass]
    public class AutoTraderLogicTest
    {
        PlatformFilePath _configFile;

        [TestInitialize]
        public void testInit()
        {
            PlatformFileHelperPC platformFileHelper = new PlatformFileHelperPC("AutoTraderTest");
            Common.PlatformFileHelper = platformFileHelper;

            _configFile = new PlatformFilePath(EngineFilePaths.ConfigsPath, "AutoTraderConfig.xml");

            // Remove config file
            if (FileHelper.FileExists(_configFile))
                FileHelper.DeleteFile(_configFile);
            Assert.IsTrue(!FileHelper.FileExists(_configFile));
            AutoTraderConfig.Initialize();
        }

        [TestMethod]
        public void TestConfigInit()
        {
            
        }
    }
}
