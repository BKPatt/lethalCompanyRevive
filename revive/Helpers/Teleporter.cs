using System.Linq;
using lethalCompanyRevive.Misc;

namespace lethalCompanyRevive.Helpers
{
    public static partial class Helper
    {
        static ShipTeleporter[]? ShipTeleporters => TPObject.Instance?.ShipTeleporters.Objects;

        public static ShipTeleporter? InverseTeleporter => Helper.ShipTeleporters?.FirstOrDefault(teleporter => teleporter.isInverseTeleporter);

        public static ShipTeleporter? Teleporter => Helper.ShipTeleporters?.FirstOrDefault(teleporter => !teleporter.isInverseTeleporter);
    }
}