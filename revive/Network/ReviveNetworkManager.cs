using HarmonyLib;
using lethalCompanyRevive.Managers;
using UnityEngine;
using Unity.Netcode;
using LethalLib.Modules;

namespace lethalCompanyRevive.Network
{
    [HarmonyPatch]
    public class ReviveNetworkManager
    {
        static GameObject networkPrefab;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void Init()
        {
            if (networkPrefab != null) return;
            networkPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("ReviveStore");
            networkPrefab.AddComponent<ReviveStore>();
            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var go = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                go.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
