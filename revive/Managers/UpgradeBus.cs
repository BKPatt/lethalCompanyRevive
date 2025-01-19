using lethalCompanyRevive.Misc;
using System.Collections.Generic;
using UnityEngine;

namespace lethalCompanyRevive.Managers
{
    public class UpgradeBus : MonoBehaviour
    {
        public static UpgradeBus Instance { get; private set; }
        public List<CustomTerminalNode> terminalNodes = new List<CustomTerminalNode>();

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            InitializeReviveNode();
        }

        void InitializeReviveNode()
        {
            CustomTerminalNode node = CustomTerminalNode.CreateReviveNode();
            terminalNodes.Add(node);
        }

        public void HandleReviveRequest(ulong playerId)
        {
            GetComponent<ReviveStore>().RequestReviveServerRpc(playerId);
        }

        public TerminalNode ConstructNode()
        {
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            foreach (CustomTerminalNode t in terminalNodes)
            {
                string saleStatus = t.salePerc == 1f ? "" : "SALE";
                if (!t.Unlocked) node.displayText += $"\\n{t.Name} // {(int)(t.UnlockPrice * t.salePerc)} // {saleStatus} ";
                else node.displayText += $"\n{t.Name} // UNLOCKED ";
            }
            if (node.displayText == "") node.displayText = "No upgrades available";
            node.displayText += "\n\n";
            return node;
        }
    }
}
