using HarmonyLib;
using lethalCompanyRevive.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace lethalCompanyRevive.Network
{
    [HarmonyPatch]
    public class ReviveNetworkManager
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            Debug.Log("Init");
            if (networkPrefab != null)
                return;

            networkPrefab = new GameObject("ReviveStore");
            ReviveStore reviveStore = networkPrefab.AddComponent<ReviveStore>();
            UpgradeBus upgradeBus = networkPrefab.AddComponent<UpgradeBus>();
            NetworkObject networkObject = networkPrefab.AddComponent<NetworkObject>();
            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            Debug.Log("SpawnNetworkHandler");
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Debug.Log("IsHost or IsServer");
                var networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }

        static GameObject networkPrefab;
    }
}
