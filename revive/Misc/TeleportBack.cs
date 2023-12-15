using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using lethalCompanyRevive.Helpers;

namespace lethalCompanyRevive.Misc
{
    class TeleportBack : ICommand
    {
        Result TeleportPlayerToBase(string[] args)
        {
            if (!Helper.GetPlayer(args[0]).IsNotNull(out PlayerControllerB sourcePlayer))
            {
                return new Result(message: "Player not found!");
            }

            Helper.BuyUnlockable(Unlockable.TELEPORTER);
            TPObject.Instance?.ShipTeleporters.Renew();

            if (!Helper.Teleporter.IsNotNull(out ShipTeleporter teleporter))
            {
                return new Result(message: "ShipTeleporter not found!");
            }

            Helper.SwitchRadarTarget(sourcePlayer);
            teleporter.PressTeleportButtonServerRpc();
            return new Result(true);
        }

        public void Execute(string[] args)
        {
            if (args.Length is 0)
            {
                Helper.StartOfRound?.ForcePlayerIntoShip();
                return;
            }

            Result result = this.TeleportPlayerToBase(args);
        }
    }
}
