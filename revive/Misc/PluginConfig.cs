using BepInEx.Configuration;

namespace lethalCompanyRevive.Misc
{
    public class PluginConfig
    {
        readonly ConfigFile configFile;

        public ConfigEntry<bool> EnableRevive;
        public ConfigEntry<int> BaseReviveCost;
        public ConfigEntry<string> ReviveCostAlgorithm;
        public ConfigEntry<bool> EnableMaxRevivesPerDay;
        public ConfigEntry<int> MaxRevivesPerDay;

        public PluginConfig(ConfigFile file)
        {
            configFile = file;
        }

        public void InitBindings()
        {
            EnableRevive = configFile.Bind(
                "General",
                "EnableRevive",
                true,
                "Enable or disable the entire revive feature."
            );

            BaseReviveCost = configFile.Bind(
                "Costs",
                "BaseReviveCost",
                100,
                "Base cost used in the revive formula. Only used for Flat and Exponential"
            );

            ReviveCostAlgorithm = configFile.Bind(
                "Costs",
                "ReviveCostAlgorithm",
                "Quota",
                "How the revive cost is calculated: Flat, Exponential, or Quota. Exponential is untested"
            );

            EnableMaxRevivesPerDay = configFile.Bind(
                "Limits",
                "EnableMaxRevivesPerDay",
                false,
                "If true, limits how many revives can happen each real-life day."
            );

            MaxRevivesPerDay = configFile.Bind(
                "Limits",
                "MaxRevivesPerDay",
                3,
                "Max revives allowed per day (if EnableMaxRevivesPerDay is true)."
            );
        }
    }
}
