using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;

namespace Talking_ben_bot
{
    public class GuildConfiguration
    {
        private static readonly List<GuildConfiguration> _configrations = new();

        public ulong GuildId { get; set; }

        public int MIN_TALK_LENGTH { get; set; } = 1500;
        public List<GuildCommand> Commands { get; set; } = new();
    
        public GuildConfiguration() { }
        public GuildConfiguration(ulong guildId)
        {
            GuildId = guildId;
        }

        public static GuildConfiguration GetConfig(ulong guildId)
        {
            var config = _configrations.FirstOrDefault(x => x.GuildId == guildId);
            if (config is null)
            {
                config = new GuildConfiguration(guildId);
                _configrations.Add(config);
            }

            return config;
        }
    }

    public class GuildCommand
    {
        public string Name { get; set; }
        public On On { get; set; }
        public int Chance { get; set; }
        public bool Repeat { get; set; }

        public List<IGuildActon> Actions { get; set; } = new();
        public List<IGuildActon> FailActions { get; set; } = new();

        public GuildCommand() { }
        public GuildCommand(string config)
        {
            var args = config.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            Name = args[0];
            On = Enum.Parse<On>(args[1]);
            Chance = int.Parse(args[2]);
            Repeat = bool.Parse(args[3]);

            var actionConfig = args[4];
            Actions = ParseActions(actionConfig);

            if (args.Length == 6)
            {
                var failActionConfig = args[5];
                FailActions = ParseActions(failActionConfig);
            }

            Console.WriteLine($"Command: {Name} {On} {Chance} {Repeat}");
        }

        public async Task Execute(IGuildUser user)
        {
            foreach (var action in Actions)
                await action.Execute(user);
        }

        private static List<IGuildActon> ParseActions(string actionConfig)
        {
            var actions = actionConfig.Substring(1, actionConfig.Length - 2).Split(';', StringSplitOptions.RemoveEmptyEntries);

            return actions.Select(x => ParseAction(actionConfig)).ToList();
        }

        private static IGuildActon ParseAction(string actionConfig)
        {
            var args = actionConfig.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return args[0] switch
            {
                "play" => new PlayAction(args[1]),
                _ => throw new Exception($"Unknown action {args[0]}"),
            };
        }
    }

    public interface IGuildActon
    {
        Task Execute(IGuildUser user);
    }

    public class PlayAction : IGuildActon
    {
        public string File { get; set; }

        public PlayAction() { }
        public PlayAction(string file)
        {
            File = file;
        }

        public async Task Execute(IGuildUser user)
        {
            await AudioHelper.Play(user.GuildId, File);
        }
    }

    public enum On
    {
        Join,
        UserJoin,
        Leave,
        UserLeave,
        UserStartTalk,
        UserStopTalk
    }
}
