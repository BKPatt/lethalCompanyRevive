using HarmonyLib;
using lethalCompanyRevive.Managers;
using UnityEngine;
using lethalCompanyRevive.Helpers;
using GameNetcodeStuff;
using System;

namespace lethalCompanyRevive.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatcher
    {
        private static UpgradeBus upgradeBus;

        [HarmonyPostfix]
        [HarmonyPatch("ParsePlayerSentence")]
        private static void CustomParser(ref Terminal __instance, ref TerminalNode __result)
        {
            string text = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
            string[] commandParts = text.Split(' ');

            if (commandParts[0] == "revive" && commandParts.Length == 2)
            {
                try
                {
                    string playerName = commandParts[1];
                    PlayerControllerB player = Helper.GetPlayer(playerName);

                    Debug.Log($"Attempting to revive player with name: {playerName}");
                    Debug.Log(player);

                    if (player == null)
                    {
                        __result = CreateTerminalNode($"Player '{playerName}' does not exist.", true);
                        return;
                    }
                    if (!player.isPlayerDead)
                    {
                        __result = CreateTerminalNode($"Player '{playerName}' is not dead.", true);
                        return;
                    }
                    if (__instance.groupCredits < 100)

                    {
                        __result = CreateTerminalNode($"Not enough credits {__instance.groupCredits}/100", true);
                        return;
                    }

                    ulong playerId = player.playerClientId;
                    if (upgradeBus == null)
                    {
                        GameObject upgradeBusObject = GameObject.Find("UpgradeBus");
                        if (upgradeBusObject != null)
                        {
                            upgradeBus = upgradeBusObject.GetComponent<UpgradeBus>();
                        }
                    }

                    __result = UpgradeBus.Instance.ConstructNode();
                    UpgradeBus.Instance.HandleReviveRequest(playerId);
                }
                catch (Exception error)
                {
                    Debug.Log(error);
                }
            }
        }

        public static PlayerControllerB GetPlayerByName(string playerName)
        {
            Debug.Log($"GetPlayerByName");
            PlayerControllerB[] allPlayers = UnityEngine.Object.FindObjectsOfType<PlayerControllerB>();
            foreach (PlayerControllerB player in allPlayers)
            {
                if (player.playerUsername == playerName)
                {
                    return player;
                }
            }
            return null;
        }

        private static TerminalNode CreateTerminalNode(string displayText, bool clearPreviousText)
        {
            Debug.Log($"CreateTerminalNode");
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.displayText = displayText;
            node.clearPreviousText = clearPreviousText;
            return node;
        }
    }
}
