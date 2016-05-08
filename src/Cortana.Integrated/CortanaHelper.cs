using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Cortana.Integrated
{
    public static class CortanaHelper
    {
        public static async Task RegisterVCD()
        {
            var vcdFile = await Package.Current.InstalledLocation.GetFileAsync("VCD.xml");
            await Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(vcdFile);
        }
    }
}
