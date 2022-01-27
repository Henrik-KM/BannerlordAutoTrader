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
            Assert.IsTrue(FileHelper.FileExists(_configFile));
        }

        [TestMethod]
        public void TestSimpleWorthCheck()
        {
            AutoTraderLogic.IsBuying = true;
            AutoTraderConfig.BuyThresholdValue = 80;
            Assert.IsTrue(AutoTraderLogic.SimpleWorthCheck(100, 79));
            Assert.IsTrue(AutoTraderLogic.SimpleWorthCheck(100, 1));
            Assert.IsFalse(AutoTraderLogic.SimpleWorthCheck(100, 81));
            Assert.IsFalse(AutoTraderLogic.SimpleWorthCheck(100, 200));

            AutoTraderLogic.IsBuying = false;
            AutoTraderConfig.SellThresholdValue = 120;
            Assert.IsTrue(AutoTraderLogic.SimpleWorthCheck(100, 121));
            Assert.IsTrue(AutoTraderLogic.SimpleWorthCheck(100, 200));
            Assert.IsFalse(AutoTraderLogic.SimpleWorthCheck(100, 119));
            Assert.IsFalse(AutoTraderLogic.SimpleWorthCheck(100, 1));
        }

    }
}
