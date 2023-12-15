using lethalCompanyRevive.Misc;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace lethalCompanyRevive.Managers
{
    public class UpgradeBus : NetworkBehaviour
    {
        public static UpgradeBus instance;
        public PluginConfig cfg;
        public TerminalNode modStoreInterface;
        public List<CustomTerminalNode> terminalNodes = new List<CustomTerminalNode>();

        void Awake()
        {
            instance = this;
            InitializeReviveNode();
        }

        private void InitializeReviveNode()
        {
            CustomTerminalNode reviveNode = CustomTerminalNode.CreateReviveNode();
            terminalNodes.Add(reviveNode);
        }

        public void HandleReviveRequest(ulong playerId)
        {
            ReviveStore.instance.RequestReviveServerRpc(playerId);
        }

        public TerminalNode ConstructNode()
        {
            modStoreInterface = new TerminalNode();
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
