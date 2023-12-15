using lethalCompanyRevive.Misc;
using System;
using Unity.Netcode;
using UnityEngine;

namespace lethalCompanyRevive.Managers
{
    public class ReviveStore : NetworkBehaviour
    {
        public static ReviveStore instance;

        private void Start()
        {
            instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncCreditsServerRpc(int newCredits)
        {
            SyncCreditsClientRpc(newCredits);
        }

        [ClientRpc]
        private void SyncCreditsClientRpc(int newCredits)
        {
            Terminal terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            terminal.groupCredits = newCredits;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestReviveServerRpc(ulong playerId)
        {
            ReviveScript reviveScript = GetComponent<ReviveScript>();
            if (reviveScript != null)
            {
                reviveScript.TryRevivePlayer(playerId);
            }
        }
    }
}
