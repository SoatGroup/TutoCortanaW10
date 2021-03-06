﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.VoiceCommands;

namespace Cortana.Interaction.CortanaAgent
{
    public sealed class CortanaVoiceCommandService : IBackgroundTask
    {
        private BackgroundTaskDeferral serviceDeferral;
        VoiceCommandServiceConnection voiceServiceConnection;

        private Dictionary<string, string> _series;
        public CortanaVoiceCommandService()
        {
            _series = new Dictionary<string, string>();
            _series.Add("homeland", "Homeland");
            _series.Add("jessica-jones", "Jessica Jones");
            _series.Add("breaking-bad", "Breaking Bad");
            _series.Add("american-dad", "American Dad");
            _series.Add("heroes", "Heroes");
            _series.Add("heroes-reborn", "Heroes Reborn");
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            this.serviceDeferral = taskInstance.GetDeferral();

            taskInstance.Canceled += (sender, reason) => serviceDeferral?.Complete();

            var triggerDetails =
              taskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (triggerDetails != null &&
              triggerDetails.Name == "CortanaVoiceIntegration")
            {
                try
                {
                    voiceServiceConnection =
                      VoiceCommandServiceConnection.FromAppServiceTriggerDetails(triggerDetails);

                    voiceServiceConnection.VoiceCommandCompleted += (sender, reason) => serviceDeferral?.Complete();

                    VoiceCommand voiceCommand = await voiceServiceConnection.GetVoiceCommandAsync();

                    switch (voiceCommand.CommandName)
                    {
                        case "launchSeries":
                            await ShowSeries();
                            break;

                        case "launchSerie":
                            await ShowSerie(voiceCommand);
                            break;
                    }
                }
                catch (Exception ex)
                { }
                finally
                {
                    this.serviceDeferral.Complete();
                }
            }
        }

        private async Task ShowSeries()
        {
            //Création d'un message de réponse temporaire permettant de ne plus couper la tâche au bout de XX secondes par défaut
            var userProgressMessage = new VoiceCommandUserMessage();
            userProgressMessage.DisplayMessage = userProgressMessage.SpokenMessage = "Nous récupérons vos séries";

            VoiceCommandResponse response_temp = VoiceCommandResponse.CreateResponse(userProgressMessage);
            await voiceServiceConnection.ReportProgressAsync(response_temp);

            //Création de la liste de résultats à afficher par Cortana
            var destinationsContentTiles = new List<VoiceCommandContentTile>();

            //Création d'une tuile pour chaque série
            foreach (var serie in _series)
            {
                var tile = new VoiceCommandContentTile();
                tile.ContentTileType = VoiceCommandContentTileType.TitleOnly;
                tile.AppLaunchArgument = "serie-" + serie.Key;
                tile.Title = serie.Value;

                destinationsContentTiles.Add(tile);
            }


            //Message de résultat
            var userReprompt = new VoiceCommandUserMessage();
            userReprompt.DisplayMessage = "Vos séries";
            userReprompt.SpokenMessage = "Voici vos séries";

            var response = VoiceCommandResponse.CreateResponse(userReprompt, destinationsContentTiles);
            await voiceServiceConnection.ReportSuccessAsync(response);
        }

        private async Task ShowSerie(VoiceCommand command)
        {
            //Création d'un message de réponse temporaire permettant de ne plus couper la tâche au bout de .5 secondes par défaut mais au bout de 5 secondes
            var userProgressMessage = new VoiceCommandUserMessage();
            userProgressMessage.DisplayMessage = userProgressMessage.SpokenMessage = "Nous récupérons vos séries";

            VoiceCommandResponse response_temp = VoiceCommandResponse.CreateResponse(userProgressMessage);
            await voiceServiceConnection.ReportProgressAsync(response_temp);

            string serie = command.Properties["serie"][0];

            var selectedSeries = _series.Where(x => x.Value.Contains(serie));

            if (selectedSeries.Count() == 1)
            {
                //Si il y a seulement une série correspondante, alors nous allons demander une confirmation (juste pour l'exemple)
                var userPrompt = new VoiceCommandUserMessage();
                userPrompt.SpokenMessage = userPrompt.DisplayMessage = "Est-ce bien la série que vous voulez afficher?";

                var userReprompt = new VoiceCommandUserMessage();
                userReprompt.SpokenMessage = userReprompt.DisplayMessage = "Confirmez-vous que c'est bien la série que vous voulez afficher?";


                //Création de la liste de résultats à afficher par Cortana
                var destinationsContentTiles = new List<VoiceCommandContentTile>();

                var tile = new VoiceCommandContentTile();
                tile.ContentTileType = VoiceCommandContentTileType.TitleOnly;
                tile.AppLaunchArgument = "serie-" + selectedSeries.First().Key;
                tile.Title = selectedSeries.First().Value;

                destinationsContentTiles.Add(tile);

                var response = VoiceCommandResponse.CreateResponseForPrompt(userPrompt, userReprompt, destinationsContentTiles);

                response.AppLaunchArgument = "serie-" + selectedSeries.First().Key;

                var responseConfirmation = await voiceServiceConnection.RequestConfirmationAsync(response);

                if (responseConfirmation != null && responseConfirmation.Confirmed)
                {
                    var userMessage = new VoiceCommandUserMessage();
                    userMessage.SpokenMessage = "Lancement de l'application";

                    var tempesponse = VoiceCommandResponse.CreateResponse(userMessage);

                    response.AppLaunchArgument = "serie-" + selectedSeries.First().Key;

                    await voiceServiceConnection.RequestAppLaunchAsync(response);
                }
                else
                {
                    //Si ce n'est pas cette série, alors on affiche les autres séries
                    ShowSeries();
                }
            }
            else if (selectedSeries.Count() > 1)
            {
                //Levée d'ambigüité

                //Création de la liste de résultats à afficher par Cortana
                var destinationsContentTiles = new List<VoiceCommandContentTile>();

                //Création d'une tuile pour chaque série
                foreach (var tempSerie in selectedSeries)
                {
                    var tile = new VoiceCommandContentTile();
                    tile.ContentTileType = VoiceCommandContentTileType.TitleOnly;
                    tile.AppLaunchArgument = "serie-" + tempSerie.Key;
                    tile.Title = tempSerie.Value;

                    destinationsContentTiles.Add(tile);
                }

                var userPrompt = new VoiceCommandUserMessage();
                userPrompt.DisplayMessage = "Afficher une série";
                userPrompt.SpokenMessage = "Précisez la série que vous voulez afficher";

                var userReprompt = new VoiceCommandUserMessage();
                userReprompt.DisplayMessage = "Quelle série voulez-vous afficher?";
                userReprompt.SpokenMessage = "Quelle série voulez-vous afficher?";

                var disambiguationResponse = VoiceCommandResponse.CreateResponseForPrompt(userPrompt, userReprompt, destinationsContentTiles);

                var result = await voiceServiceConnection.RequestDisambiguationAsync(disambiguationResponse);

                if (result != null)
                {
                    var userMessage = new VoiceCommandUserMessage();
                    userMessage.SpokenMessage = "Lancement de l'application";

                    var response = VoiceCommandResponse.CreateResponse(userMessage);

                    response.AppLaunchArgument = result.SelectedItem.AppLaunchArgument;

                    await voiceServiceConnection.RequestAppLaunchAsync(response);
                }
            }
            else
            {
                //Message d'erreur
                var errorMessage = new VoiceCommandUserMessage();
                errorMessage.DisplayMessage = "Aucune série ne correspond";
                errorMessage.SpokenMessage = "Aucune série ne correspond";

                var response = VoiceCommandResponse.CreateResponse(errorMessage);

                await voiceServiceConnection.ReportFailureAsync(response);
            }

        }

        private async Task LaunchAppInForeground()
        {
            var userMessage = new VoiceCommandUserMessage();
            userMessage.SpokenMessage = "Lancement de l'application";

            var response = VoiceCommandResponse.CreateResponse(userMessage);

            response.AppLaunchArgument = "LaunchSeries";

            await voiceServiceConnection.RequestAppLaunchAsync(response);
        }

    }
}
