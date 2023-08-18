using System.Net;
using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;

namespace BattleBitLifeSteal;

class LifeSteal
{
    static void Main(string[] args)
    {
        var listener = new ServerListener<LifeStealPlayer, LifeStealServer>();
        listener.Start(30001);
        listener.OnGameServerConnecting += OnGameServerConnecting;
        listener.OnValidateGameServerToken += OnValidateGameServerToken;

        Console.WriteLine("API started!");

        Thread.Sleep(-1);
    }

    private static async Task<bool> OnValidateGameServerToken(IPAddress ip, ushort gameport, string sentToken)
    {
        await Console.Out.WriteLineAsync(ip + ":" + gameport + " sent token: " + sentToken);
        return sentToken == "123";
    }
    
    private static async Task<bool> OnGameServerConnecting(IPAddress arg)
    {
        await Console.Out.WriteLineAsync(arg.ToString() + " connecting");
        return true;
    }
}

public class LifeStealPlayer : Player<LifeStealPlayer>
{
    public bool IsAdmin;
    public int Kills;
    public int Deaths;
}

class LifeStealServer : GameServer<LifeStealPlayer>
{
    public static List<ApiCommand> ApiCommands = new()
    {
        new HelpCommand(),
        new StatsCommand(),
        new KillCommand(),
        new StartCommand(),
        new StopCommand(),
    };
    
    private CommandHandler handler = new();

    public readonly List<Weapon> WeaponList = new()
    {
        Weapons.Groza,
        Weapons.ACR,
        Weapons.AK15,
        Weapons.AK74,
        Weapons.G36C,
        Weapons.HoneyBadger,
        Weapons.KrissVector,
        Weapons.L86A1,
        Weapons.M4A1,
        Weapons.M249,
        Weapons.MK14EBR,
        Weapons.MK20,
        Weapons.MP7,
        Weapons.PP2000,
        Weapons.SCARH,
        Weapons.FAL,
        Weapons.MP5,
        Weapons.P90
    };

    public async Task SetupServer()
    {
        ServerSettings.PlayerCollision = true;
        MapRotation.ClearRotation();
        MapRotation.AddToRotation("Azagor");
        GamemodeRotation.ClearRotation();
        GamemodeRotation.AddToRotation("TDM");
    }

    public override async Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args)
    {
        var stats = args.Stats;
        
        stats.Progress.Rank = 200;
        stats.Progress.Prestige = 10;

        if (steamID == 76561198395073327 || steamID == 76561198196108998)
            stats.Roles = Roles.Admin;
    }

    public override async Task OnTick()
    {
        foreach (var player in AllPlayers)
        {
            /* player.Message("hp: " + player.HP + "<br>"
                           + "pos: " + player.Position + "<br>"
                           + "isdead: " + player.IsDead + "<br>"
                           + "crouching: " + player.StandingState + "<br>"
                           + "lean: " + player.LeaningState + "<br>"
                           + "bleeding: " + player.IsBleeding + "<br>"
                           );
             */
            
            player.Modifications.JumpHeightMultiplier = 1.25f;
            player.Modifications.RunningSpeedMultiplier = 1.5f;
            player.Modifications.FallDamageMultiplier = 0f;
            player.Modifications.CanSpectate = false;
            player.Modifications.ReloadSpeedMultiplier = 1.5f;
            player.Modifications.GiveDamageMultiplier = 1f;
            player.Modifications.RespawnTime = 0;
            player.Modifications.DownTimeGiveUpTime = 0;
            player.Modifications.MinimumDamageToStartBleeding = 100f;
            player.Modifications.MinimumHpToStartBleeding = 0f;
            player.Modifications.HitMarkersEnabled = false;
            player.Modifications.KillFeed = true;
        }
        
        Task.Run(() =>
        {
            switch (RoundSettings.State)
            {
                case GameState.Playing:
                {
                    RoundSettings.SecondsLeft = 666666;
                    break;
                }
                case GameState.WaitingForPlayers:
                {
                    ForceStartGame();
                    break;
                }
            }

            Task.Delay(1000);
        });
    }

    public override async Task OnConnected()
    {
        Console.WriteLine($"Gameserver connected! {this.GameIP}:{this.GamePort}");

        await SetupServer();
    }
    
    public override async Task OnDisconnected()
    {
        Console.WriteLine($"Gameserver disconnected! {this.GameIP}:{this.GamePort}");
    }

    public override async Task OnReconnected()
    {
        Console.WriteLine($"Gameserver reconnected! {this.GameIP}:{this.GamePort}");

        await SetupServer();
    }

    public override async Task OnRoundEnded()
    {
        Console.WriteLine("Round ended!");
        ForceStartGame();
    }

    public override async Task<bool> OnPlayerRequestingToChangeRole(LifeStealPlayer player, GameRole requestedRole)
    {
        if (requestedRole != GameRole.Assault)
        {
            player.Message("You can only play as Assault!", fadeoutTime: 2f);
            return false;
        }
        
        return true;
    }

    public override async Task OnPlayerConnected(LifeStealPlayer player)
    {
        SayToChat("<color=green>" + player.Name + " joined the game!</color>");
        await Console.Out.WriteLineAsync("Connected: " + player);
        
        player.JoinSquad(Squads.Alpha);
    }
    
    public override async Task OnPlayerDisconnected(LifeStealPlayer player)
    {
        SayToChat("<color=orange>" + player.Name + " left the game!</color>");
        await Console.Out.WriteLineAsync("Disconnected: " + player);
    }
    
    public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<LifeStealPlayer> args)
    {
        if (args.Killer == args.Victim)
        {
            args.Victim.Kill();
            args.Victim.Deaths++;
        }
        else
        {
            args.Victim.Kill();
            args.Killer.SetHP(100);
            args.Killer.Kills++;
            args.Victim.Deaths++;
            await Task.Run(() => { UpdateWeapon(args.Killer); });
        }
    }

    public override async Task<OnPlayerSpawnArguments> OnPlayerSpawning(LifeStealPlayer player, OnPlayerSpawnArguments request)
    {
        await Task.Run(() => { UpdateWeapon(player); });
        
        request.Loadout.SecondaryWeapon = default;
        request.Loadout.Throwable = default;
        request.Loadout.FirstAid = default;
        request.Loadout.HeavyGadget = default;
        request.Loadout.LightGadget = default;
        
        return request;
    }

    public void UpdateWeapon(LifeStealPlayer player)
    {
        var weapon = WeaponList[new Random().Next(WeaponList.Count)];

        var weaponItem = new WeaponItem()
        {
            Tool = weapon,
            MainSight = Attachments.RedDot,
        };
        
        player.SetPrimaryWeapon(weaponItem, 20, true);
    }

    public override async Task<bool> OnPlayerTypedMessage(LifeStealPlayer player, ChatChannel channel, string msg)
    {
        if (player.SteamID == 76561198395073327)
            player.IsAdmin = true;
        
        var splits = msg.Split(" ");
        var cmd = splits[0].ToLower();
        if (!cmd.StartsWith("/")) return true;
        
        foreach(var apiCommand in ApiCommands)
        {
            if (apiCommand.CommandString == cmd || apiCommand.Aliases.Contains(cmd))
            {
                var command = apiCommand.ChatCommand(player, channel, msg);
                if (apiCommand.AdminOnly && !player.IsAdmin)
                    return true;
                    
                await handler.handleCommand(player, command);
                return false;
            }
        }

        return true;
    }
}