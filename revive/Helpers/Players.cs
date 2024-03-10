using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace lethalCompanyRevive.Helpers
{
    public static partial class Helper
    {
        public static PlayerControllerB? LocalPlayer => GameNetworkManager.Instance.localPlayerController;

        public static PlayerControllerB[]? Players => Helper.StartOfRound?.allPlayerScripts;

        public static PlayerControllerB? GetPlayer(string playerNameOrId)
        {
            PlayerControllerB[]? players = Helper.Players;

            Debug.Log($"Players: {players}");

            if (players == null || players.Length == 0)
            {
                Debug.Log("No players found.");
                return null;
            }

            Debug.Log($"Searching for player with name or ID: {playerNameOrId}");

            PlayerControllerB? playerByName = players.FirstOrDefault(player =>
            {
                if (player != null)
                {
                    Debug.Log($"Checking player: {player.playerUsername}");
                    return player.playerUsername == playerNameOrId;
                }
                return false;
            });

            if (playerByName != null)
            {
                Debug.Log($"Found player by name: {playerByName.playerUsername}");
                return playerByName;
            }

            PlayerControllerB? playerById = players.FirstOrDefault(player =>
            {
                if (player != null)
                {
                    Debug.Log($"Checking player ID: {player.playerClientId}");
                    return player.playerClientId.ToString() == playerNameOrId;
                }
                return false;
            });

            if (playerById != null)
            {
                Debug.Log($"Found player by ID: {playerById.playerUsername}");
                return playerById;
            }

            Debug.Log("Player not found.");
            return null;
        }

        public static PlayerControllerB? GetPlayer(int playerId) => Helper.GetPlayer(playerId.ToString());
    }
}