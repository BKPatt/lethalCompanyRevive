using GameNetcodeStuff;

namespace lethalCompanyRevive.Helpers
{
    public static partial class Helper
    {
        public static void SwitchRadarTarget(ulong playerClientId)
        {
            Helper.StartOfRound?.mapScreen.SwitchRadarTargetServerRpc((int)playerClientId);
        }

        public static void SwitchRadarTarget(PlayerControllerB player)
        {
            Helper.SwitchRadarTarget(player.playerClientId);
        }
    }
}