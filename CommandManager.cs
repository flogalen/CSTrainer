namespace OpenPrefirePrac
{
    public static class CommandManager
    {
        public static void RegisterCommand(OpenPrefirePrac plugin)
        {
            // Define the command
            var commandDefinition = new CommandDefinition("css_prefire", "Open Prefire Prac main menu", (player, commandInfo) =>
            {
                // Validate if the command is player-based
                if (player == null)
                {
                    return;
                }

                // Command shortcuts based on input arguments
                if (commandInfo.ArgCount > 1)
                {
                    switch (commandInfo.ArgByIndex(1))
                    {
                        case "prac":
                            int choice = 0;
                            if (int.TryParse(commandInfo.ArgByIndex(2), out choice) && choice > 0 && choice <= plugin.Practices.Count)
                            {
                                plugin.StartPractice(player, choice - 1);
                                return;
                            }
                            player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {plugin.Translator!.Translate(player, "practice.help", plugin.Practices.Count)}");
                            return;

                        case "map":
                            string mapName = commandInfo.ArgByIndex(2);
                            plugin.ChangeMap(player, mapName);
                            return;

                        case "df":
                            int difficulty = 0;
                            if (int.TryParse(commandInfo.ArgByIndex(2), out difficulty) && difficulty > 0 && difficulty <= 5)
                            {
                                plugin.ChangeDifficulty(player, 5 - difficulty);
                                return;
                            }
                            player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {plugin.Translator!.Translate(player, "difficulty.help")}");
                            return;

                        case "mode":
                            string trainingMode = commandInfo.ArgByIndex(2);
                            switch (trainingMode)
                            {
                                case "full":
                                    plugin.ChangeTrainingMode(player, 1);
                                    return;
                                case "rand":
                                    plugin.ChangeTrainingMode(player, 0);
                                    return;
                                default:
                                    player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {plugin.Translator!.Translate(player, "modemenu.help")}");
                                    return;
                            }

                        case "bw":
                            string botWeapon = commandInfo.ArgByIndex(2);
                            switch (botWeapon)
                            {
                                case "rand":
                                    plugin.SetBotWeapon(player, 0);
                                    return;
                                case "ump":
                                    plugin.SetBotWeapon(player, 1);
                                    return;
                                case "ak":
                                    plugin.SetBotWeapon(player, 2);
                                    return;
                                case "sct":
                                    plugin.SetBotWeapon(player, 3);
                                    return;
                                case "awp":
                                    plugin.SetBotWeapon(player, 4);
                                    return;
                                default:
                                    player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {plugin.Translator!.Translate(player, "weaponmenu.help")}");
                                    return;
                            }

                        case "lang":
                            string language = commandInfo.ArgByIndex(2);
                            switch (language)
                            {
                                case "en":
                                    plugin.Translator!.UpdatePlayerCulture(player.SteamID, "EN");
                                    break;
                                case "pt":
                                    plugin.Translator!.UpdatePlayerCulture(player.SteamID, "pt-BR");
                                    break;
                                case "zh":
                                    plugin.Translator!.UpdatePlayerCulture(player.SteamID, "ZH");
                                    break;
                                default:
                                    player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {plugin.Translator!.Translate(player, "languagemenu.help")}");
                                    return;
                            }
                            player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {plugin.Translator!.Translate(player, "languagemenu.set")}");
                            return;

                        case "exit":
                            plugin.ForceStopPractice(player);
                            return;

                        case "help":
                            player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {plugin.Translator!.Translate(player, "mainmenu.help", plugin.Practices.Count)}");
                            return;

                        default:
                            player.PrintToChat($" {ChatColors.Green}[OpenPrefirePrac] {ChatColors.White} {plugin.Translator!.Translate(player, "mainmenu.help", plugin.Practices.Count)}");
                            break;
                    }
                }

                // Draw the main menu usind methods from the plugin
                plugin.OpenMainMenu(player);
            });

            // Register the command
            CommandManager.RegisterCommand(commandDefinition);
        }

        public static void UnregisterCommand(OpenPrefirePrac plugin)
        {
            CommandManager.UnregisterCommand("css_prefire");
        }
    }
}