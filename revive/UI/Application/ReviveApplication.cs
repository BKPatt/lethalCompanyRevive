﻿using InteractiveTerminalAPI.UI;
using InteractiveTerminalAPI.UI.Application;
using InteractiveTerminalAPI.UI.Cursor;
using InteractiveTerminalAPI.UI.Screen;
using lethalCompanyRevive.Helpers;
using lethalCompanyRevive.Managers;
using GameNetcodeStuff;
using System.Linq;
using System.Text;
using UnityEngine;

// This file now computes revive cost dynamically, matching the same formula.

namespace lethalCompanyRevive.UI.Application
{
    internal class ReviveApplication : InteractiveTerminalApplication
    {
        // Instead of a fixed const, compute cost at runtime.
        int ReviveCost
        {
            get
            {
                if (TimeOfDay.Instance == null || StartOfRound.Instance == null)
                    return 100; // fallback
                int totalPlayers = StartOfRound.Instance.connectedPlayersAmount + 1;
                float quota = TimeOfDay.Instance.profitQuota;
                int cost = (int)(quota / totalPlayers);
                return cost < 1 ? 1 : cost;
            }
        }

        CursorMenu mainMenu;
        IScreen mainScreen;

        public override void Initialization()
        {
            var players = Helper.Players;
            if (players == null || players.Length == 0)
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
                return;
            }

            var deadPlayers = players.Where(p => p != null && p.isPlayerDead).ToArray();
            if (deadPlayers.Length == 0)
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
                return;
            }

            // Build menu items dynamically
            CursorElement[] elements = new CursorElement[deadPlayers.Length + 2];
            int cost = ReviveCost;

            // "Revive All"
            elements[0] = CursorElement.Create(
                $"Revive All ({deadPlayers.Length * cost})",
                "",
                () => ConfirmReviveAll(deadPlayers),
                (elem) => CanAfford(deadPlayers.Length * cost),
                true
            );

            // Individual dead players
            for (int i = 0; i < deadPlayers.Length; i++)
            {
                var p = deadPlayers[i];
                string label = $"Revive {p.playerUsername} ({cost})";
                elements[i + 1] = CursorElement.Create(
                    label,
                    "",
                    () => ConfirmReviveSingle(p),
                    (elem) => CanAfford(cost),
                    true
                );
            }

            // "Exit"
            elements[elements.Length - 1] = CursorElement.Create("Exit", "", () => CloseUI());

            mainMenu = CursorMenu.Create(0, '>', elements);
            mainScreen = BoxedScreen.Create("Revive", new ITextElement[]
            {
                TextElement.Create("Select a dead player to revive or revive them all."),
                TextElement.Create(" "),
                mainMenu
            });
            SwitchScreen(mainScreen, mainMenu, true);
        }

        bool CanAfford(int cost) => (Helper.Terminal != null && Helper.Terminal.groupCredits >= cost);

        void ConfirmReviveSingle(PlayerControllerB p)
        {
            int cost = ReviveCost;
            if (!CanAfford(cost))
            {
                ErrorMessage("Revive", () => SwitchScreen(mainScreen, mainMenu, true), $"Not enough credits.");
                return;
            }
            Confirm("Revive", $"Revive {p.playerUsername} for {cost}?",
                () => DoReviveSingle(p),
                () => SwitchScreen(mainScreen, mainMenu, true));
        }

        void DoReviveSingle(PlayerControllerB p)
        {
            if (p != null && p.isPlayerDead && ReviveStore.Instance != null)
            {
                ReviveStore.Instance.RequestReviveServerRpc(p.playerClientId);
                ErrorMessage("Revive", () => CloseUI(), $"Reviving {p.playerUsername}...");
            }
            else
            {
                ErrorMessage("Revive", () => SwitchScreen(mainScreen, mainMenu, true), "Could not revive.");
            }
        }

        void ConfirmReviveAll(PlayerControllerB[] players)
        {
            int cost = ReviveCost * players.Length;
            if (!CanAfford(cost))
            {
                ErrorMessage("Revive", () => SwitchScreen(mainScreen, mainMenu, true), "Not enough credits.");
                return;
            }
            StringBuilder sb = new StringBuilder("Revive all of:\n\n");
            foreach (var pl in players) sb.AppendLine(pl.playerUsername);
            sb.AppendLine($"\nTotal Cost: {cost}");
            Confirm("Revive All", sb.ToString(),
                () => DoReviveAll(players),
                () => SwitchScreen(mainScreen, mainMenu, true));
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

        void CloseUI()
        {
            if (InteractiveTerminalManager.Instance != null)
                UnityEngine.Object.Destroy(InteractiveTerminalManager.Instance.gameObject);
        }
    }
}
