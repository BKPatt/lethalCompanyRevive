using lethalCompanyRevive.Misc;

namespace lethalCompanyRevive.Helpers
{
    public static partial class Helper
    {
        public static Terminal? Terminal =>
            Helper.HUDManager is null
                ? null
                : Reflector.Target(Helper.HUDManager).GetInternalField<Terminal>("terminalScript");
    }
}