using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace Talking_ben_bot
{
    public class Ben
    {
        private readonly string _token;
        private readonly DiscordSocketClient _client;

        public Ben(string token)
        {
            _token = token;
            _client = new();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
            _client.UserVoiceStateUpdated += VoiceStateUpdated;
        }

        private Task VoiceStateUpdated(SocketUser user, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState)
        {
            Task.Run(async () =>
            {
                if (user.IsBot)
                    return;

                await AudioHelper.VoiceStateChanged(user as IGuildUser, oldVoiceState, newVoiceState);
            });

            return Task.CompletedTask;
        }

        private Task MessageReceived(SocketMessage msg)
        {
            Task.Run(async () =>
            {
                var guildUser = msg.Author as IGuildUser;

                var content = msg.Content;
                switch (content.ToLower())
                {
                    case "!ben call":
                        var voiceState = msg.Author as IVoiceState;
                        var voice = voiceState.VoiceChannel;

                        if (voice is not null)
                        {
                            await AudioHelper.Join(voice);
                            await AudioHelper.Play(voice.GuildId, "Ben.mp3");
                        }
                        else await msg.Channel.SendMessageAsync("Ты откуда звонишь?"); 

                        break;
                    case "!ben leave":
                        await AudioHelper.Play(guildUser.GuildId, "Dyadya1.mp3");
                        await AudioHelper.Play(guildUser.GuildId, "Dyadya2.mp3");
                        await AudioHelper.Leave(guildUser.GuildId);

                        break;
                    case "!ben command add":
                        var commandConfig = content.Substring("!ben command add".Length);
                        var config = GuildConfiguration.GetConfig(guildUser.GuildId);

                        config.Commands.Add(new GuildCommand(commandConfig));

                        break;
                }
            });

            return Task.CompletedTask;
        }

        public async Task Start()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        }

        public async Task Stop()
        {
            await _client.StopAsync();
            await _client.LogoutAsync();
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString());

            return Task.CompletedTask;
        }
    }
}
