using HarmonyLib;
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

            // If exactly 2 parts: 'revive <playerName>', do direct single revive
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
                    if (__instance.groupCredits < 100)
                    {
                        __result = CreateTerminalNode($"Not enough credits {__instance.groupCredits}/100", true);
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
