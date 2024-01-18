﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.ComponentModel.DataAnnotations;
using System.Collections.Immutable;

namespace OpenPrefirePrac;

public class OpenPrefirePrac : BasePlugin
{
    public override string ModuleName => "Open Prefire Prac";
    public override string ModuleVersion => "0.0.1";
    private string plugin_path = "../../csgo/addons/counterstrikesharp/plugins/OpenPrefirePrac/";

    private Dictionary<int, List<int>> bots_of_players = new Dictionary<int, List<int>>();
    private Dictionary<int, int> progress_of_players = new Dictionary<int, int>();
    private Dictionary<int, int> masters_of_bots = new Dictionary<int, int>();
    private Dictionary<int, int> practice_of_players = new Dictionary<int, int>();
    private Dictionary<string, int> practice_name_to_id = new Dictionary<string, int>();
    private Dictionary<int, bool> practice_enabled = new Dictionary<int, bool>();

    private string map_name = "";

    private int player_count = 0;

    private Dictionary<int, PrefirePractice> practices = new Dictionary<int, PrefirePractice>();

    private ChatMenu main_menu = new ChatMenu("Open Prefire Prac");
    private ChatMenu map_menu = new ChatMenu("Switch map");
    private ChatMenu practice_menu = new ChatMenu("Choose prefire route");

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

	    Console.WriteLine("[OpenPrefirePrac] Registering listeners.");
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServerHandler);
        RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnectHandler);
    }

    public void OnClientPutInServerHandler(int slot)
    {
        var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));

        if (!player.IsValid || player.IsBot) return;

        bots_of_players.Add(slot, new List<int>());
        progress_of_players.Add(slot, 0);
        practice_of_players.Add(slot, -1);

        Console.WriteLine("[OpenPrefirePrac] Player just connected: " + player.Handle);
    }

    public void OnClientDisconnectHandler(int slot)
    {
        var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));

        if (!player.IsValid || player.IsBot) return;

        if (bots_of_players[slot].Count != 0)
        {
            RemoveBots(player);
            player_count--;
        }

        // TODO: Release resources(practices, targets, bots...)
        bots_of_players.Remove(slot);
        progress_of_players.Remove(slot);
        practice_of_players.Remove(slot);
    }

    public void OnMapStartHandler(string map)
    {
        map_name = map;
        Console.WriteLine("[OpenPrefirePrac] Map loaded: " + map_name);

        // load practices available in current map, from corresponding map directory.
        List<string> map_dirs = new List<string>(Directory.EnumerateDirectories(plugin_path + "maps"));
        bool found = false;
        for (int i = 0; i < map_dirs.Count; i++)
        {
            string map_path = map_dirs[i].Substring(map_dirs[i].LastIndexOf(Path.DirectorySeparatorChar) + 1);
            Console.WriteLine($"[OpenPrefirePrac] Map folder for map {map_path} founded.");
            map_menu.AddMenuOption(map_path, ChangeMap);

            if (map_path.Equals(map_name))
            {
                found = true;
                Console.WriteLine("[OpenPrefirePrac] Map folder for current map founded.");
                break;
            }
        }

        if (found)
        {
            LoadPractice();
        }
        else
        {
            Console.WriteLine("[OpenPrefirePrac] Failed to load practices on map " + map_name);
        }

        // Create menu.
        main_menu.MenuOptions.Clear();
        main_menu.AddMenuOption("Choose practice.", OpenPracticeMenu);
        main_menu.AddMenuOption("Switch map.", OpenMapMenu);
        main_menu.AddMenuOption("Exit prefire mode", ForceExitPrefireMode);

    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (@event.Userid.IsValid && @event.Userid.IsBot && !@event.Userid.IsHLTV) 
        {
            // if there are more targets to place, move bot to next place
            if (masters_of_bots.ContainsKey(@event.Userid.Slot))
            {
                int master_slot = masters_of_bots[@event.Userid.Slot];
                int target_no = progress_of_players[master_slot];
                int practice_no = practice_of_players[master_slot];
                if (target_no < practices[practice_no].targets.Count)
                {
                    progress_of_players[master_slot]++;
                    MovePlayer(@event.Userid, practices[practice_no].targets[target_no].is_crouching, practices[practice_no].targets[target_no].position, practices[practice_no].targets[target_no].rotation);
                }
                else
                {
                    masters_of_bots.Remove(@event.Userid.Slot);
                    bots_of_players[master_slot].Remove(@event.Userid.Slot);
                    Server.ExecuteCommand($"bot_kick {@event.Userid.PlayerName}");

                    if (bots_of_players[master_slot].Count == 0)
                    {
                        // Practice finished.
                        var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(master_slot + 1));
                        player.PrintToChat("[OpenPrefirePrac] Congratulations! You have finished your prefire practice!");
                        ExitPrefireMode(player);
                    }
                }
            }
        }

        return HookResult.Continue;
    }

    [ConsoleCommand("css_prefire", "Print available prefire routes and receive user's choice")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPrefireCommand(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player_count == 0)
        {
            Server.ExecuteCommand("tv_enable 0");
            Server.ExecuteCommand("sv_cheats 1");
            Server.ExecuteCommand("mp_maxmoney 60000");
            Server.ExecuteCommand("mp_startmoney 60000");
            Server.ExecuteCommand("mp_buytime 9999");
            Server.ExecuteCommand("mp_buy_anywhere 1");
            Server.ExecuteCommand("sv_infinite_ammo 1");
            Server.ExecuteCommand("mp_autoteambalance 0");
            Server.ExecuteCommand("mp_limitteams 50");
            Server.ExecuteCommand("mp_warmup_pausetimer 1");
            Server.ExecuteCommand("mp_warmup_start");
            // Server.ExecuteCommand("bot_stop 1");
            // Server.ExecuteCommand("bot_freeze 1");
            Server.ExecuteCommand("bot_zombie 1");
        }

        player.PrintToChat("============ [OpenPrefirePrac] ============");
        ChatMenus.OpenMenu(player, main_menu);
        player.PrintToChat("===========================================");
    }

    public void OnRouteSelect(CCSPlayerController player, ChatMenuOption option)
    {
        string choosen_practice = option.Text;
        int practice_no = practice_name_to_id[choosen_practice];

        // Check if selected practice route is compatible with other on-playing routes.
        if (!practice_enabled[practice_no])
        {
            player.PrintToChat("Others are playing incompatible routes that might share some bots with your selected route. Please choose another practice.");
            return;
        }

        // Entering practice mode
        if (bots_of_players[player.Slot].Count == 0)
        {
            // If the player isn't practicing now
            player_count++;
        }
        else
        {
            // Enable disabled practice routes
            int previous_practice_no = practice_of_players[player.Slot];
            for (int i = 0; i < practices[previous_practice_no].incompatible_practices.Count; i++)
            {
                if (practice_name_to_id.ContainsKey(practices[previous_practice_no].incompatible_practices[i]))
                {
                    int disabled_practice_no = practice_name_to_id[practices[previous_practice_no].incompatible_practices[i]];
                    practice_enabled[disabled_practice_no] = true;
                }
            }

            // Reset player's practice
            RemoveBots(player);
            progress_of_players[player.Slot] = 0;
            
        }
        
        player.PrintToChat($"[OpenPrefirePrac] Starting prefire route ({choosen_practice}).");
        practice_of_players[player.Slot] = practice_no;

        // Disable incompatible practices.
        for (int i = 0; i < practices[practice_no].incompatible_practices.Count; i++)
        {
            if (practice_name_to_id.ContainsKey(practices[practice_no].incompatible_practices[i]))
            {
                int disabled_practice_no = practice_name_to_id[practices[practice_no].incompatible_practices[i]];
                practice_enabled[disabled_practice_no] = false;
            }
        }
        
        // Spawn bots
        int num_spawn = Math.Min(practices[practice_no].targets.Count, 4);
        Console.WriteLine($"[OpenPrefirePrac] Spawn {num_spawn} targets (total {practices[practice_no].targets.Count})");
        AddBot(player, num_spawn);
        AddTimer(0.5f, () => 
        {
            for (int i = 0; i < bots_of_players[player.Slot].Count; i++)
            {
                var bot = new CCSPlayerController(NativeAPI.GetEntityFromIndex(bots_of_players[player.Slot][i] + 1));
                Console.WriteLine($"[OpenPrefirePrac] Moving bot {bot.PlayerName}, slot: {bot.Slot}.");
                MovePlayer(bot, practices[practice_no].targets[i].is_crouching, practices[practice_no].targets[i].position, practices[practice_no].targets[i].rotation);
            }
            progress_of_players[player.Slot] = num_spawn;
            
        });
        
        AddTimer(0.5f * num_spawn, () => MovePlayer(player, false, practices[practice_no].player.position, practices[practice_no].player.rotation));
    }

    public void ForceExitPrefireMode(CCSPlayerController player, ChatMenuOption option)
    {
        ExitPrefireMode(player);
        
        player.PrintToChat("[OpenPrefirePrac] Prefire mode exited.");
    }

    public void OpenMapMenu(CCSPlayerController player, ChatMenuOption option)
    {
        player.PrintToChat("============ [OpenPrefirePrac] ============");
        ChatMenus.OpenMenu(player, map_menu);
        player.PrintToChat("===========================================");
    }

    public void OpenPracticeMenu(CCSPlayerController player, ChatMenuOption option)
    {
        // Dynamically draw menu
        practice_menu.MenuOptions.Clear();
        for (int i = 0; i < practices.Count; i++)
        {
            if (practice_enabled[i])
                practice_menu.AddMenuOption(practices[i].practice_name, OnRouteSelect);     // practice name here is splited by space instead of underline
        }

        player.PrintToChat("============ [OpenPrefirePrac] ============");
        ChatMenus.OpenMenu(player, practice_menu);
        player.PrintToChat("===========================================");
    }

    public void ChangeMap(CCSPlayerController player, ChatMenuOption option)
    {
        // Only allow change map when noone is practicing.
        if (player_count == 0)
        {
            // map_name = option.Text;
            // LoadPractice();
            Server.ExecuteCommand($"changelevel {option.Text}");
        }
        else
        {
            player.PrintToChat("There are other players practicing. Try again later.");
        }
    }

    public void LoadPractice()
    {
        Console.WriteLine($"[OpenPrefirePrac] Loading practices for map {map_name}.");
        List<string> practice_files = new List<string>(Directory.EnumerateFiles(plugin_path + "maps/" + map_name));
        practices.Clear();
        practice_name_to_id.Clear();
        practice_menu.MenuOptions.Clear();
        for (int i = 0; i < practice_files.Count; i++)
        {
            string practice_name = practice_files[i].Substring(practice_files[i].LastIndexOf(Path.DirectorySeparatorChar) + 1).Split(".")[0];
            practices.Add(i, new PrefirePractice(map_name, practice_name));
            practice_name_to_id.Add(practice_name.Replace("_", " "), i);
            practice_enabled.Add(i, true);
            Console.WriteLine($"[OpenPrefirePrac] {map_name} {practice_name} Loaded.");
        }
    }
    
    public void ExitPrefireMode(CCSPlayerController player)
    {
        player_count--;
        RemoveBots(player);
        practice_of_players[player.Slot] = -1;
        
        if (player_count == 0)
        {
            Server.ExecuteCommand("sv_cheats 0");
            Server.ExecuteCommand("mp_warmup_pausetimer 0");
            Server.ExecuteCommand("mp_warmup_start");
            Server.ExecuteCommand("tv_enable 0");
        }
    }

    public void RemoveBots(CCSPlayerController player)
    {
        // TODO: support multiplayer

        for (int i = 0; i < bots_of_players[player.Slot].Count; i++)
        {
            int bot_slot = bots_of_players[player.Slot][i];
            var bot = new CCSPlayerController(NativeAPI.GetEntityFromIndex(bot_slot + 1));
            if (bot.IsValid)
            {
                Server.ExecuteCommand($"bot_kick {bot.PlayerName}");
            }
            else
            {
                Console.WriteLine($"[OpenPrefirePrac] Trying to kick an invalid bot.");
            }
            masters_of_bots.Remove(bot_slot);
        }
        bots_of_players[player.Slot].Clear();
    }

    private void AddBot(CCSPlayerController player, int number_of_bots)
    {
        // Not working
        // CCSPlayerController? bot = Utilities.CreateEntityByName<CCSPlayerController>($"bot {bots_of_players[player.Slot].Count}");
        
        // if (bot != null)
        // {
        //     Console.WriteLine($"[OpenPrefirePrac] Bot created: {bot.Slot}.");
        //     bot.Teleport(pos, ang, vel);
        //     bot.DispatchSpawn();
        //     bots_of_players[player.Slot].Add(bot.Slot);
        // }

        Console.WriteLine($"[OpenPrefirePrac] Creating {number_of_bots} bots.");
        for (int i = 0; i < number_of_bots; i++)
        {
            if (player.TeamNum == (byte)CsTeam.CounterTerrorist)
            {
                Server.ExecuteCommand("bot_join_team T");
                Server.ExecuteCommand("bot_add_t");
            }
            else if (player.TeamNum == (byte)CsTeam.Terrorist)
            {
                Server.ExecuteCommand("bot_join_team CT");
                Server.ExecuteCommand("bot_add_ct");
            }
        }

        AddTimer(0.2f, () =>
        {
            int number_bot_to_find = number_of_bots;
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

            foreach (var tempPlayer in playerEntities)
            {
                if (!tempPlayer.IsValid || !tempPlayer.IsBot || tempPlayer.IsHLTV) continue;
                if (tempPlayer.UserId.HasValue)
                {
                    // Chech if it belongs to someone, if so, do nothing
                    if (masters_of_bots.ContainsKey(tempPlayer.Slot))
                        continue;

                    // If it's a newly added bot
                    if (number_bot_to_find == 0)
                    {
                        // a redundent bot, kick it
                        Server.ExecuteCommand($"bot_kick {tempPlayer.PlayerName}");
                        Console.WriteLine($"[OpenPrefirePrac] Exec command: bot_kick {tempPlayer.PlayerName}");
                        continue;
                    }

                    bots_of_players[player.Slot].Add(tempPlayer.Slot);
                    masters_of_bots.Add(tempPlayer.Slot, player.Slot);

                    number_bot_to_find--;
                    
                    Console.WriteLine($"[OpenPrefirePrac] Bot {tempPlayer.PlayerName}, slot: {tempPlayer.Slot} has been spawned.");
                }
            }
        });


        // System.Threading.Thread.Sleep(200);

        // var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
        // foreach (var tempPlayer in playerEntities)
        // {
        //     if (!tempPlayer.IsBot || tempPlayer.IsHLTV) continue;
        //     if (tempPlayer.UserId.HasValue)
        //     {
        //         // Chech if it belongs to someone, if so, do nothing
        //         if (masters_of_bots.ContainsKey(tempPlayer.Slot))
        //             continue;
                
        //         // If it's a newly added bot
        //         if (have_added_one_bot)
        //         {
        //             // a redundent bot, kick it
        //             Server.ExecuteCommand($"bot_kick {tempPlayer.PlayerName}");
        //             Console.WriteLine($"[OpenPrefirePrac] Exec command: bot_kick {tempPlayer.PlayerName}");
        //             continue;
        //         }

        //         bots_of_players[player.Slot].Add(tempPlayer.Slot);
        //         masters_of_bots.Add(tempPlayer.Slot, player.Slot);
        //         Console.WriteLine($"[OpenPrefirePrac] Moving bot {tempPlayer.PlayerName}, slot: {tempPlayer.Slot}.");

        //         MovePlayer(tempPlayer, crouch, pos, ang);
        //         have_added_one_bot = true;
        //         Console.WriteLine($"[OpenPrefirePrac] Bot {tempPlayer.UserId}, slot: {tempPlayer.Slot} has been spawned.");
        //     }
        // }

    }

    public void MovePlayer(CCSPlayerController player, bool crouch, Vector pos, QAngle ang)
    {
        // Only bot can crouch
        if (crouch)
        {
            CCSPlayer_MovementServices movement_service = new CCSPlayer_MovementServices(player.PlayerPawn.Value.MovementServices.Handle);
            // System.Threading.Thread.Sleep(100);
            // movement_service.DuckAmount = 1;
            // System.Threading.Thread.Sleep(100);
            // player.PlayerPawn.Value.Bot.IsCrouching = true;
            AddTimer(0.3f, () => movement_service.DuckAmount = 1);
            AddTimer(0.4f, () => player.PlayerPawn.Value.Bot.IsCrouching = true);
        }
        
        // System.Threading.Thread.Sleep(100);
        player.PlayerPawn.Value.Teleport(pos, ang, new Vector(0, 0, 0));
        // AddTimer(0.1f, () => player.PlayerPawn.Value.Teleport(pos, ang, new Vector(0, 0, 0)));
    }
}