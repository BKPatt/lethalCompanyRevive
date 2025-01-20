using HarmonyLib;
using lethalCompanyRevive.Managers;
using Unity.Netcode;
using UnityEngine;

namespace lethalCompanyRevive.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal static class DisconnectPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
        static void OnGameDisconnect()
        {
            if (ReviveStore.Instance != null)
            {
                ReviveStore.Instance.ResetAllValues();
                var netObj = ReviveStore.Instance.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);
                else
                    Object.Destroy(ReviveStore.Instance.gameObject);
            }
        }
    }
}
