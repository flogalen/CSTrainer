namespace OpenPrefirePrac.Managers
{
    using System.Collections.Generic;
    using CounterStrikeSharp.API.Modules.Entities;
    using CounterStrikeSharp.API.Modules.Utils;

    public class PlayerManager
    {
        private readonly Dictionary<CCSPlayerController, PlayerStatus> _playerStatuses;
        private Translator _translator;

        public PlayerManager(Dictionary<CCSPlayerController, PlayerStatus> playerStatuses, Translator translator)
        {
            _playerStatuses = playerStatuses;
            _translator = translator;
        }

        public void AddPlayer(CCSPlayerController player, PlayerStatus defaultSettings)
        {
            _playerStatuses.Add(player, defaultSettings);
            _translator.RecordPlayerCulture(player);
        }

        public void RemovePlayer(CCSPlayerController player)
        {
            _playerStatuses.Remove(player);
        }

        public bool IsValidPlayer(CCSPlayerController? player)
        {
            return player != null && player.IsValid && !player.IsHLTV;
        }

        public void SetPlayerHealth(CCSPlayerController player, int health)
        {
            if (player == null || !player.PawnIsAlive || player.Pawn.Value == null || health < 0)
                return;

            if (health > 100)
                player.Pawn.Value.MaxHealth = health;
            player.Pawn.Value.Health = health;
            Utilities.SetStateChanged(player, "CBaseEntity", "m_iHealth");
        }
    }

    public class BotManager
    {
        private readonly Dictionary<CCSPlayerController, CCSPlayerController> _ownerOfBots;

        public BotManager(Dictionary<CCSPlayerController, CCSPlayerController> ownerOfBots)
        {
            _ownerOfBots = ownerOfBots;
        }

        public void AssignBotOwner(CCSPlayerController bot, CCSPlayerController owner)
        {
            _ownerOfBots.Add(bot, owner);
        }

        public void RemoveBot(CCSPlayerController bot)
        {
            if (_ownerOfBots.ContainsKey(bot))
                _ownerOfBots.Remove(bot);
        }

        public void KickBot(CCSPlayerController bot)
        {
            if (bot == null || !bot.IsBot)
                return;

            Server.ExecuteCommand($"bot_kick {bot.PlayerName}");
        }

        public void SetBotWeapon(CCSPlayerController player, int botWeapon)
        {
            // Set bot weapon based on botWeapon parameter.
        }
    }
}