using GameNetcodeStuff;
using lethalCompanyRevive.Managers;
using Unity.Netcode;
using UnityEngine;
using lethalCompanyRevive.Helpers;
using System.Collections.Generic;

namespace lethalCompanyRevive.Misc
{
    class ReviveScript : NetworkBehaviour
    {
        private const int ReviveCost = 100;

        public void TryRevivePlayer(ulong playerId)
        {
            if (!IsServer) return;

            PlayerControllerB player = GetPlayerById(playerId);
            if (player == null || player.health != 0 || !CanAffordRevive(player)) return;

            DeductCredits(player);
            RevivePlayer(player);
        }

        private bool CanAffordRevive(PlayerControllerB player)
        {
            Terminal terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            return terminal.groupCredits >= ReviveCost;
        }

        private void DeductCredits(PlayerControllerB player)
        {
            Terminal terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            terminal.groupCredits -= ReviveCost;
            ReviveStore.instance.SyncCreditsServerRpc(terminal.groupCredits);
        }

        private void RevivePlayer(PlayerControllerB player)
        {
            player.HealClientRpc();
        }

        private PlayerControllerB GetPlayerById(ulong playerId)
        {
            PlayerControllerB[] allOtherPlayers = Helper.Players;
            List<PlayerControllerB> deadPlayers = new List<PlayerControllerB>(); // Using List for dynamic resizing

            // Fill deadPlayers list with players who are dead
            foreach (var player in allOtherPlayers)
            {
                if (player.isPlayerDead)
                {
                    deadPlayers.Add(player);
                }
            }

            // Search for the player with the specified playerId in the list of dead players
            foreach (var deadPlayer in deadPlayers)
            {
                if (deadPlayer.playerClientId == playerId)
                {
                    return deadPlayer;
                }
            }

            return null; // Return null if the player is not found
        }
    }
}
