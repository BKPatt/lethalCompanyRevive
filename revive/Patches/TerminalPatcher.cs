﻿using HarmonyLib;
using lethalCompanyRevive.Managers;
using lethalCompanyRevive.Helpers;
using GameNetcodeStuff;
using UnityEngine;
using System;

namespace lethalCompanyRevive.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatcher
    {
        static UpgradeBus upgradeBus;

        [HarmonyPostfix]
        [HarmonyPatch("ParsePlayerSentence")]
        static void CustomParser(ref Terminal __instance, ref TerminalNode __result)
        {
            string text = __instance.screenText.text
                .Substring(__instance.screenText.text.Length - __instance.textAdded)
                .Trim();

            string[] parts = text.Split(' ');
            if (parts.Length == 2 && parts[0].Equals("revive", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string playerName = parts[1];
                    PlayerControllerB p = Helper.GetPlayer(playerName);
                    if (p == null)
                    {
                        __result = CreateTerminalNode($"Player '{playerName}' does not exist.", true);
                        return;
                    }
                    if (!p.isPlayerDead)
                    {
                        __result = CreateTerminalNode($"Player '{playerName}' is not dead.", true);
                        return;
                    }
                    int dynamicCost = ComputeConsoleCost();
                    if (__instance.groupCredits < dynamicCost)
                    {
                        __result = CreateTerminalNode($"Not enough credits {__instance.groupCredits}/{dynamicCost}", true);
                        return;
                    }
                    ulong pid = p.playerClientId;
                    if (upgradeBus == null)
                    {
                        GameObject busObj = GameObject.Find("UpgradeBus");
                        if (busObj != null) upgradeBus = busObj.GetComponent<UpgradeBus>();
                    }
                    if (upgradeBus != null)
                    {
                        __result = upgradeBus.ConstructNode();
                        upgradeBus.HandleReviveRequest(pid);
                    }
                    else
                    {
                        __result = CreateTerminalNode("UpgradeBus not found.", true);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Revive error: {e}");
                }
            }
        }

        static int ComputeConsoleCost()
        {
            string algo = Plugin.cfg.ReviveCostAlgorithm.Value.ToLower();
            int baseCost = Plugin.cfg.BaseReviveCost.Value;
            switch (algo)
            {
                case "flat":
                    return baseCost;
                case "exponential":
                    return baseCost;
                case "quota":
                default:
                    if (TimeOfDay.Instance == null || StartOfRound.Instance == null)
                        return 100;
                    float quota = TimeOfDay.Instance.profitQuota;
                    int totalPlayers = StartOfRound.Instance.connectedPlayersAmount + 1;
                    int cost = (int)(quota / totalPlayers);
                    if (cost < 1) cost = 1;
                    return cost;
            }
        }

        static TerminalNode CreateTerminalNode(string txt, bool clear)
        {
            TerminalNode n = ScriptableObject.CreateInstance<TerminalNode>();
            n.displayText = txt;
            n.clearPreviousText = clear;
            return n;
        }
    }
}
