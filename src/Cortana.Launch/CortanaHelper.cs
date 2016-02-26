using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Cortana.Launch
{
    public static class CortanaHelper
    {
        public static async Task RegisterVCD()
        {
            var vcdFile = await Package.Current.InstalledLocation.GetFileAsync("VCD.xml");
            await Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(vcdFile);
        }

        public static async Task EditSeries()
        {
            Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinition commandSet;

            if (Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstalledCommandDefinitions.TryGetValue(
                    "CortanaLaunchCommandSet_fr-fr", out commandSet))
            {
                await commandSet.SetPhraseListAsync(
                  "serie", new string[] {"Homeland", "Jessica Jones", "Breaking Bad"});
            }
        }
    }
}
