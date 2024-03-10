using lethalCompanyRevive.Misc;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace lethalCompanyRevive.Managers
{
    public class UpgradeBus : NetworkBehaviour
    {
        public static UpgradeBus Instance { get; private set; }

        public PluginConfig cfg;
        public TerminalNode modStoreInterface;
        public List<CustomTerminalNode> terminalNodes = new List<CustomTerminalNode>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        private void Start()
        {
            InitializeReviveNode();
        }

        private void InitializeReviveNode()
        {
            CustomTerminalNode reviveNode = CustomTerminalNode.CreateReviveNode();
            terminalNodes.Add(reviveNode);
        }

        public void HandleReviveRequest(ulong playerId)
        {
            Debug.Log($"Handle Revive Request for Player ID {playerId}");
            ReviveStore.Instance.RequestReviveServerRpc(playerId);
        }

        public TerminalNode ConstructNode()
        {
            TerminalNode modStoreInterface = ScriptableObject.CreateInstance<TerminalNode>();
            modStoreInterface.clearPreviousText = true;
            foreach (CustomTerminalNode terminalNode in terminalNodes)
            {
                string saleStatus = terminalNode.salePerc == 1f ? "" : "SALE";
                if (!terminalNode.Unlocked)
                {
                    modStoreInterface.displayText += $"\n{terminalNode.Name} // {(int)(terminalNode.UnlockPrice * terminalNode.salePerc)}  // {saleStatus}  ";
                }
                else
                {
                    modStoreInterface.displayText += $"\n{terminalNode.Name} // UNLOCKED  ";
                }
            }
            if (modStoreInterface.displayText == "")
            {
                modStoreInterface.displayText = "No upgrades available";
            }
            modStoreInterface.displayText += "\n\n";
            return modStoreInterface;
        }
    }
}
