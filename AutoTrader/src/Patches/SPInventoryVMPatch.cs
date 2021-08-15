using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AutoTrader.Patches
{
    /*[HarmonyPatch(typeof(SPInventoryVM), "OnTotalAmountChange")]
    class OnTotalAmountChangePatch
    {
		private static MethodInfo ExecuteRemoveZeroCountsMethod { get; } = typeof(SPInventoryVM).GetMethod("ExecuteRemoveZeroCounts", BindingFlags.Instance | BindingFlags.NonPublic);

		private static void Postfix(SPInventoryVM __instance, int newTotalAmount)
		{
			try
			{
				//MethodInfo ExecuteRemoveZeroCountsMethod = typeof(SPInventoryVM).GetMethod("ExecuteRemoveZeroCounts", BindingFlags.Instance | BindingFlags.NonPublic);
				ExecuteRemoveZeroCountsMethod.Invoke(__instance, new object[] {});
			}
			catch (Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage("AutoTrader setup failed partially. Crashes might occur when autotrading.", Color.FromUint(4282569842U)));
			}
		}
	}*/

	[HarmonyPatch(typeof(SPInventoryVM), "AfterTransfer")]
	class AfterTransferPatch
	{
		private static MethodInfo ExecuteRemoveZeroCountsMethod { get; } = typeof(SPInventoryVM).GetMethod("ExecuteRemoveZeroCounts", BindingFlags.Instance | BindingFlags.NonPublic);

		private static void Postfix(SPInventoryVM __instance, InventoryLogic inventoryLogic, List<TransferCommandResult> results)
		{
			try
			{
				//MethodInfo ExecuteRemoveZeroCountsMethod = typeof(SPInventoryVM).GetMethod("ExecuteRemoveZeroCounts", BindingFlags.Instance | BindingFlags.NonPublic);
				ExecuteRemoveZeroCountsMethod.Invoke(__instance, new object[] { });
			}
			catch (Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage("AutoTrader setup failed partially. Crashes might occur when autotrading.", Color.FromUint(4282569842U)));
			}
		}
	}
}
