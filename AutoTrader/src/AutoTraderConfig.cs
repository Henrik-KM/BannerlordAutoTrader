using System;
using System.IO;
using System.Text;
using System.Xml;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AutoTrader
{
    public static class AutoTraderConfig
    {
        private static PlatformFilePath _configFile;

        public static string AutoTraderGameVersion { get; } = "e1.7.1";

        public static int MaxKeepGrainsValue { get; set; } = 500;

        public static int BuyThresholdValue { get; set; } = 90;
        public static int SellThresholdValue { get; set; } = 100;
        public static bool SimpleTradingAI { get; set; } = true;
        public static bool UseWeightedValue { get; set; } = false;
        public static int MaxCapacityValue { get; set; } = 15;
        public static int KeepGrainsValue { get; set; } = 10;
        public static int KeepConsumablesValue { get; set; } = 4;
        public static int UseInventorySpaceValue { get; set; } = 90;
        public static int KeepWagesValue { get; set; } = 3;
        public static int SearchRadiusValue { get; set; } = 300;
        public static int WeaponTierValue { get; set; } = 2;
        public static int ArmorTierValue { get; set; } = 2;

        public static bool SellSmithingValue { get; set; } = false;
        public static bool ResupplyHardwoodValue { get; set; } = false;
        public static bool ResupplyValue { get; set; } = true;

        public static bool BuyHorsesValue { get; set; } = true;
        public static bool SellHorsesValue { get; set; } = false;
        public static bool BuyWeaponsValue { get; set; } = false;
        public static bool SellWeaponsValue { get; set; } = true;
        public static bool BuyArmorValue { get; set; } = false;
        public static bool SellArmorValue { get; set; } = true;
        public static bool BuyGoodsValue { get; set; } = true;
        public static bool SellGoodsValue { get; set; } = true;
        public static bool BuyConsumablesValue { get; set; } = true;
        public static bool SellConsumablesValue { get; set; } = true;
        public static bool BuyLivestockValue { get; set; } = false;
        public static bool SellLivestockValue { get; set; } = true;

        public static int Version { get; set; } = 2;
        public static bool DebugMode { get; set; } = false;

        public static void Initialize()
        {
            // Get the config file path
            AutoTraderConfig._configFile = new PlatformFilePath(EngineFilePaths.ConfigsPath, "AutoTraderConfig.xml");

            // If it does not exist, create it by an initial save
            if (!FileHelper.FileExists(AutoTraderConfig._configFile))
            {
                AutoTraderConfig.Save();
            }
            else
            {
                // Read it
                var content = FileHelper.GetFileContentString(AutoTraderConfig._configFile);
                var stringReader = new System.IO.StringReader(content);
                XmlTextReader textReader = new XmlTextReader(stringReader);

                bool found_version = false;
                while (textReader.Read())
                {
                    if (textReader.IsStartElement())
                    {
                        if (textReader.Name == "version")
                        {
                            found_version = true;
                        }
                    }
                }

                textReader.Close();

                if (!found_version)
                {
                    // Reset
                    AutoTraderConfig.Save();
                }

                content = FileHelper.GetFileContentString(AutoTraderConfig._configFile);
                stringReader = new System.IO.StringReader(content);
                textReader = new XmlTextReader(stringReader);

                while (textReader.Read())
                {
                    if (textReader.IsStartElement())
                    {
                        if (textReader.Name == "buyThresholdValue")
                        {
                            AutoTraderConfig.BuyThresholdValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "simpleTradingAI")
                        {
                            AutoTraderConfig.SimpleTradingAI = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "useWeightedValue")
                        {
                            AutoTraderConfig.UseWeightedValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellThresholdValue")
                        {
                            AutoTraderConfig.SellThresholdValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "maxCapacityValue")
                        {
                            AutoTraderConfig.MaxCapacityValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepGrainsValue")
                        {
                            AutoTraderConfig.KeepGrainsValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepConsumablesValue")
                        {
                            AutoTraderConfig.KeepConsumablesValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "useInventorySpaceValue")
                        {
                            AutoTraderConfig.UseInventorySpaceValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "searchRadiusValue")
                        {
                            AutoTraderConfig.SearchRadiusValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "weaponTierValue")
                        {
                            AutoTraderConfig.WeaponTierValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "armorTierValue")
                        {
                            AutoTraderConfig.ArmorTierValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepWagesValue")
                        {
                            AutoTraderConfig.KeepWagesValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "resupplyValue")
                        {
                            AutoTraderConfig.ResupplyValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellSmithingValue")
                        {
                            AutoTraderConfig.SellSmithingValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "resupplyHardwoodValue")
                        {
                            AutoTraderConfig.ResupplyHardwoodValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyHorsesValue")
                        {
                            AutoTraderConfig.BuyHorsesValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellHorsesValue")
                        {
                            AutoTraderConfig.SellHorsesValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyArmorValue")
                        {
                            AutoTraderConfig.BuyArmorValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellArmorValue")
                        {
                            AutoTraderConfig.SellArmorValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyWeaponsValue")
                        {
                            AutoTraderConfig.BuyWeaponsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellWeaponsValue")
                        {
                            AutoTraderConfig.SellWeaponsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyGoodsValue")
                        {
                            AutoTraderConfig.BuyGoodsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellGoodsValue")
                        {
                            AutoTraderConfig.SellGoodsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyConsumablesValue")
                        {
                            AutoTraderConfig.BuyConsumablesValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellConsumablesValue")
                        {
                            AutoTraderConfig.SellConsumablesValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyLivestockValue")
                        {
                            AutoTraderConfig.BuyLivestockValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellLivestockValue")
                        {
                            AutoTraderConfig.SellLivestockValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "debugMode")
                        {
                            AutoTraderConfig.DebugMode = Boolean.Parse(textReader.ReadString());
                        }
                    }
                }
            }
        }

        public static void Save()
        {
            // Open writer and write settings
            XmlDocument xmlDocument = new XmlDocument();
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.IndentChars = "  ";
            xmlWriterSettings.NewLineChars = "\r\n";
            xmlWriterSettings.NewLineHandling = NewLineHandling.Replace;
            StringBuilder sb = new StringBuilder();

            //using (XmlTextWriter textWriter = XmlWriter.Create(xmlDocument.CreateNavigator().AppendChild(), xmlWriterSettings))
            using (XmlWriter textWriter = XmlWriter.Create(sb, xmlWriterSettings))
            {
                textWriter.WriteStartElement("config");

                textWriter.WriteElementString("version", AutoTraderConfig.Version.ToString());

                textWriter.WriteElementString("simpleTradingAI", AutoTraderConfig.SimpleTradingAI.ToString());
                textWriter.WriteElementString("useWeightedValue", AutoTraderConfig.UseWeightedValue.ToString());
                textWriter.WriteElementString("buyThresholdValue", AutoTraderConfig.BuyThresholdValue.ToString());
                textWriter.WriteElementString("sellThresholdValue", AutoTraderConfig.SellThresholdValue.ToString());
                textWriter.WriteElementString("maxCapacityValue", AutoTraderConfig.MaxCapacityValue.ToString());
                textWriter.WriteElementString("keepGrainsValue", AutoTraderConfig.KeepGrainsValue.ToString());
                textWriter.WriteElementString("keepConsumablesValue", AutoTraderConfig.KeepConsumablesValue.ToString());
                textWriter.WriteElementString("useInventorySpaceValue", AutoTraderConfig.UseInventorySpaceValue.ToString());
                textWriter.WriteElementString("keepWagesValue", AutoTraderConfig.KeepWagesValue.ToString());
                textWriter.WriteElementString("searchRadiusValue", AutoTraderConfig.SearchRadiusValue.ToString());
                textWriter.WriteElementString("weaponTierValue", AutoTraderConfig.WeaponTierValue.ToString());
                textWriter.WriteElementString("armorTierValue", AutoTraderConfig.ArmorTierValue.ToString());

                textWriter.WriteElementString("resupplyValue", AutoTraderConfig.ResupplyValue.ToString());
                textWriter.WriteElementString("sellSmithingValue", AutoTraderConfig.SellSmithingValue.ToString());
                textWriter.WriteElementString("resupplyHardwoodValue", AutoTraderConfig.ResupplyHardwoodValue.ToString());

                textWriter.WriteElementString("buyHorsesValue", AutoTraderConfig.BuyHorsesValue.ToString());
                textWriter.WriteElementString("sellHorsesValue", AutoTraderConfig.SellHorsesValue.ToString());
                textWriter.WriteElementString("buyArmorValue", AutoTraderConfig.BuyArmorValue.ToString());
                textWriter.WriteElementString("sellArmorValue", AutoTraderConfig.SellArmorValue.ToString());
                textWriter.WriteElementString("buyWeaponsValue", AutoTraderConfig.BuyWeaponsValue.ToString());
                textWriter.WriteElementString("sellWeaponsValue", AutoTraderConfig.SellWeaponsValue.ToString());
                textWriter.WriteElementString("buyGoodsValue", AutoTraderConfig.BuyGoodsValue.ToString());
                textWriter.WriteElementString("sellGoodsValue", AutoTraderConfig.SellGoodsValue.ToString());
                textWriter.WriteElementString("buyConsumablesValue", AutoTraderConfig.BuyConsumablesValue.ToString());
                textWriter.WriteElementString("sellConsumablesValue", AutoTraderConfig.SellConsumablesValue.ToString());
                textWriter.WriteElementString("buyLivestockValue", AutoTraderConfig.BuyLivestockValue.ToString());
                textWriter.WriteElementString("sellLivestockValue", AutoTraderConfig.SellLivestockValue.ToString());

                textWriter.WriteElementString("debugMode", AutoTraderConfig.DebugMode.ToString());

                textWriter.WriteEndElement();
            }

            FileHelper.SaveFileString(AutoTraderConfig._configFile, sb.ToString());
        }
    }
}
