using HarmonyLib;
using lethalCompanyRevive.Managers;
using UnityEngine;
using lethalCompanyRevive.Helpers;
using GameNetcodeStuff;

namespace lethalCompanyRevive.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch("ParsePlayerSentence")]
        private static void CustomParser(ref Terminal __instance, ref TerminalNode __result)
        {
            string text = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
            string[] commandParts = text.ToLower().Split(' ');

            if (commandParts[0] == "revive" && commandParts.Length == 2)
            {
                string playerName = commandParts[1];
                PlayerControllerB player = Helper.GetPlayer(playerName);

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
                __result = UpgradeBus.instance.ConstructNode();
                UpgradeBus.instance.HandleReviveRequest(playerId);
            }
        }

        private static TerminalNode CreateTerminalNode(string displayText, bool clearPreviousText)
        {
            TerminalNode node = new TerminalNode();
            node.displayText = displayText;
            node.clearPreviousText = clearPreviousText;
            return node;
        }
    }
}
