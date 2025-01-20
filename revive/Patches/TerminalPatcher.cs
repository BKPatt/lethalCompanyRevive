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

            // 'revive <playerName>'
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

                    // Dynamic revive cost now
                    int cost = 100; // fallback
                    if (TimeOfDay.Instance != null && StartOfRound.Instance != null)
                    {
                        int totalPlayers = StartOfRound.Instance.connectedPlayersAmount + 1;
                        float quota = TimeOfDay.Instance.profitQuota;
                        int dynamicCost = (int)(quota / totalPlayers);
                        cost = dynamicCost < 1 ? 1 : dynamicCost;
                    }

                    if (__instance.groupCredits < cost)
                    {
                        __result = CreateTerminalNode($"Not enough credits {__instance.groupCredits}/{cost}", true);
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

        static TerminalNode CreateTerminalNode(string txt, bool clear)
        {
            TerminalNode n = ScriptableObject.CreateInstance<TerminalNode>();
            n.displayText = txt;
            n.clearPreviousText = clear;
            return n;
        }
    }
}
