﻿using InteractiveTerminalAPI.UI;
using InteractiveTerminalAPI.UI.Application;
using InteractiveTerminalAPI.UI.Cursor;
using InteractiveTerminalAPI.UI.Screen;
using lethalCompanyRevive.Helpers;
using lethalCompanyRevive.Managers;
using lethalCompanyRevive.Misc;
using GameNetcodeStuff;
using System.Linq;
using System.Text;
using UnityEngine;

namespace lethalCompanyRevive.UI.Application
{
    internal class ReviveApplication : InteractiveTerminalApplication
    {
        CursorMenu mainMenu;
        IScreen mainScreen;

        public override void Initialization()
        {
            if (!Plugin.cfg.EnableRevive.Value)
            {
                CloseUI();
                return;
            }
            var players = Helper.Players;
            if (players == null || players.Length == 0)
            {
                ShowNoPlayersUI();
                return;
            }
            var deadPlayers = players.Where(p => p != null && p.isPlayerDead).ToArray();
            if (deadPlayers.Length == 0)
            {
                ShowNoDeadPlayersUI();
                return;
            }
            CursorElement[] elements = new CursorElement[deadPlayers.Length + 2];
            int singleCost = ComputeDisplayCost();
            elements[0] = CursorElement.Create(
                $"Revive All ({deadPlayers.Length * singleCost})",
                "",
                () => ConfirmReviveAll(deadPlayers),
                (elem) => CanAfford(deadPlayers.Length * singleCost),
                true
            );
            for (int i = 0; i < deadPlayers.Length; i++)
            {
                var p = deadPlayers[i];
                string label = $"Revive {p.playerUsername} ({singleCost})";
                elements[i + 1] = CursorElement.Create(
                    label,
                    "",
                    () => ConfirmReviveSingle(p, singleCost),
                    (elem) => CanAfford(singleCost),
                    true
                );
            }
            elements[elements.Length - 1] = CursorElement.Create("Exit", "", () => CloseUI());
            mainMenu = CursorMenu.Create(0, '>', elements);
            mainScreen = BoxedScreen.Create(
                "Revive",
                new ITextElement[]
                {
                    TextElement.Create("Select a dead player or revive them all."),
                    TextElement.Create(" "),
                    mainMenu
                }
            );
            SwitchScreen(mainScreen, mainMenu, true);
        }

        int ComputeDisplayCost()
        {
            string algo = Plugin.cfg.ReviveCostAlgorithm.Value.ToLower();
            int baseCost = Plugin.cfg.BaseReviveCost.Value;
            switch (algo)
            {
                case "flat":
                    return baseCost;
                case "exponential":
                    return baseCost;
                case "quota":
                default:
                    if (TimeOfDay.Instance == null || StartOfRound.Instance == null)
                        return 100;
                    float quota = TimeOfDay.Instance.profitQuota;
                    int totalPlayers = StartOfRound.Instance.connectedPlayersAmount + 1;
                    int cost = (int)(quota / totalPlayers);
                    if (cost < 1) cost = 1;
                    return cost;
            }
        }

        bool CanAfford(int cost)
        {
            var t = Helper.Terminal;
            return (t != null && t.groupCredits >= cost);
        }

        void ConfirmReviveSingle(PlayerControllerB p, int cost)
        {
            if (!CanAfford(cost))
            {
                ErrorMessage("Revive", () => SwitchScreen(mainScreen, mainMenu, true), "Not enough credits.");
                return;
            }
            Confirm(
                "Revive",
                $"Revive {p.playerUsername} for {cost} credits?",
                () => DoReviveSingle(p),
                () => SwitchScreen(mainScreen, mainMenu, true)
            );
        }

        void DoReviveSingle(PlayerControllerB p)
        {
            if (ReviveStore.Instance == null)
            {
                ErrorMessage("Revive", () => SwitchScreen(mainScreen, mainMenu, true), "ReviveStore missing.");
                return;
            }
            ReviveStore.Instance.RequestReviveServerRpc(p.playerClientId);
            ErrorMessage("Revive", () => CloseUI(), $"Reviving {p.playerUsername}...");
        }

        void ConfirmReviveAll(PlayerControllerB[] players)
        {
            int singleCost = ComputeDisplayCost();
            int totalCost = players.Length * singleCost;
            if (!CanAfford(totalCost))
            {
                ErrorMessage("Revive", () => SwitchScreen(mainScreen, mainMenu, true), "Not enough credits.");
                return;
            }
            StringBuilder sb = new StringBuilder("Revive all:\n\n");
            foreach (var pl in players) sb.AppendLine(pl.playerUsername);
            sb.AppendLine($"\nTotal Cost: {totalCost}");
            Confirm(
                "Revive All",
                sb.ToString(),
                () => DoReviveAll(players),
                () => SwitchScreen(mainScreen, mainMenu, true)
            );
        }

        void DoReviveAll(PlayerControllerB[] players)
        {
            if (ReviveStore.Instance == null)
            {
                ErrorMessage("Revive", () => SwitchScreen(mainScreen, mainMenu, true), "ReviveStore missing.");
                return;
            }
            foreach (var p in players)
            {
                if (p != null && p.isPlayerDead)
                    ReviveStore.Instance.RequestReviveServerRpc(p.playerClientId);
            }
            ErrorMessage("Revive", () => CloseUI(), "Reviving all dead players...");
        }

        void ShowNoPlayersUI()
        {
            var menu = CursorMenu.Create(0, '>', new[]
            {
                CursorElement.Create("Exit", "", () => CloseUI())
            });
            mainScreen = BoxedScreen.Create("Revive", new ITextElement[]
            {
                TextElement.Create("No players found."),
                TextElement.Create(" "),
                menu
            });
            SwitchScreen(mainScreen, menu, true);
        }

        void ShowNoDeadPlayersUI()
        {
            var menu = CursorMenu.Create(0, '>', new[]
            {
                CursorElement.Create("Exit", "", () => CloseUI())
            });
            mainScreen = BoxedScreen.Create("Revive", new ITextElement[]
            {
                TextElement.Create("No dead players to revive."),
                TextElement.Create(" "),
                menu
            });
            SwitchScreen(mainScreen, menu, true);
        }

        void CloseUI()
        {
            if (InteractiveTerminalManager.Instance != null)
                Object.Destroy(InteractiveTerminalManager.Instance.gameObject);
        }
    }
}
