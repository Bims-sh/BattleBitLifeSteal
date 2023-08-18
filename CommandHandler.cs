using System.Text;
using BattleBitAPI.Common;
using CommunityServerAPI.Enums;

namespace BattleBitLifeSteal;

public class CommandHandler
{
    public async Task handleCommand(LifeStealPlayer player, Command cmd)
    {
        switch (cmd.Action)
        {
            case ActionType.Help:
            {
                player.Message("Available commands:", fadeoutTime: 2f);
                var commands = LifeStealServer.ApiCommands.Where(c => !c.AdminOnly || player.IsAdmin).ToList();
                
                StringBuilder messageBuilder = new StringBuilder();
                foreach (var command in commands)
                {
                    messageBuilder.Append($"{command.CommandString} - {command.HelpString}\n");
                }
                string message = messageBuilder.ToString();
                
                player.Message(message, fadeoutTime: 5f);
                break;
            }
            case ActionType.Stats:
            {
                var playerKills = player.Kills;
                var playerDeaths = player.Deaths;
                var playerKd = playerDeaths == 0 ? playerKills : (double)playerKills / playerDeaths;
                var formattedPlayerKd = playerKd.ToString("0.00");
                
                player.Message($"Kills: {playerKills}<br>Deaths: {playerDeaths}<br>K/D: {formattedPlayerKd}", fadeoutTime: 5f);
                break;
            }
            case ActionType.Kill:
            {
                var target = cmd.Message.Split(" ")[1..].Aggregate((a, b) => a + " " + b);
                var targetPlayer = player.GameServer.AllPlayers.ToList().FirstOrDefault(p => p.Name.ToLower().Contains(target.ToLower()) || p.SteamID.ToString().Contains(target));
                
                if (target == null)
                {
                    player.Message("Player not found!", fadeoutTime: 2f);
                    break;
                }
                
                targetPlayer?.Kill();
                player.Message($"Killed {targetPlayer?.Name}", fadeoutTime: 2f);
                break;
            }
            case ActionType.Start:
            {
                if (player.GameServer.RoundSettings.State != GameState.WaitingForPlayers && player.GameServer.RoundSettings.State != GameState.CountingDown)
                {
                    player.Message("Round already started!", fadeoutTime: 2f);
                    break;
                }
                
                player.Message("Starting game!", fadeoutTime: 2f);
                player.GameServer.ForceStartGame();
                player.GameServer.RoundSettings.SecondsLeft = 3;
                break;
            }
            case ActionType.Stop:
            {
                if (player.GameServer.RoundSettings.State != GameState.EndingGame)
                {
                    player.Message("Round already ended!", fadeoutTime: 2f);
                    break;
                }
                
                player.Message("Ending game!", fadeoutTime: 2f);
                player.GameServer.ForceEndGame();
                player.GameServer.RoundSettings.SecondsLeft = 3;
                break;
            }
            default:
            {
                player.Message("Unknown command!");
                break;
            }
        }
    }
}