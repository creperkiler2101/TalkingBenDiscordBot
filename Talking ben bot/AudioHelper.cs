using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Audio;

namespace Talking_ben_bot
{
    public class AudioHelperClient
    {
        private bool _locked = false;

        private readonly IVoiceChannel _voice;
        private readonly IAudioClient _client;
        private readonly AudioOutStream _outStream;

        public ulong VoiceChannelId { get => _voice.Id; }

        public AudioHelperClient(IVoiceChannel voice, IAudioClient client)
        {
            _voice = voice;
            _client = client;
            _outStream = _client.CreatePCMStream(AudioApplication.Music);
        }

        public async Task Stop()
        {
            if (_client.ConnectionState == ConnectionState.Connected)
            {
                await _outStream.DisposeAsync();
                await _client.StopAsync();
                await SpeechHelper.StopListen(_voice.Guild);
            }
        }

        public async Task Play(string path)
        {
            if (_locked)
                return;

            _locked = true;

            using var ffmpeg = CreateSendFFMPEG(path);

            try 
            { 
                await ffmpeg.StandardOutput.BaseStream.CopyToAsync(_outStream); 
            }
            finally
            {
                try
                {
                    await _outStream.FlushAsync();
                } catch { }
            }

            ffmpeg.Close();

            _locked = false;
        }

        private static Process CreateSendFFMPEG(string path)
        {
            return Process.Start(
                new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            );
        }
    }

    public static class AudioHelper
    {
        private static readonly ConcurrentDictionary<ulong, AudioHelperClient> _clients = new();

        public static async Task Join(IVoiceChannel voice)
        {
            var guildId = voice.GuildId;
            if (_clients.TryGetValue(guildId, out var existsClient))
                await existsClient.Stop();

            var client = await voice.ConnectAsync();
            var clientHelper = new AudioHelperClient(voice, client);
            _clients[guildId] = clientHelper;

            client.Disconnected += async (ex) =>
            {
                _clients.TryRemove(guildId, out var _);
                await clientHelper.Stop();

                Console.WriteLine($"Leave voice channel: {voice.Name}");

                await SpeechHelper.StopListen(voice.Guild);
            };

            Console.WriteLine($"Join voice channel: {voice.Name}");

            var users = await voice.GetUsersAsync().ToListAsync();
            foreach (var user in users[0])
                SpeechHelper.StartListen(user);
        }

        public static async Task Leave(ulong guildId)
        {
            if (_clients.TryRemove(guildId, out var client))
                await client.Stop();
        }

        public static async Task Play(ulong guildId, string fileName) 
        {
            if (_clients.TryGetValue(guildId, out var client))
            {
                var path = GetAudioFullPath(fileName);
                if (!File.Exists(path))
                    throw new FileNotFoundException("Audio not found", fileName);

                await client.Play(path);
            }
        }

        public static async Task VoiceStateChanged(IGuildUser user, IVoiceState oldState, IVoiceState newState)
        {
            var guildId = user.GuildId;
            if (_clients.TryGetValue(guildId, out var client))
            {
                if (oldState.VoiceChannel is null && newState.VoiceChannel is not null) 
                {
                    if (client.VoiceChannelId == newState.VoiceChannel.Id)
                    {
                        Console.WriteLine($"{user.DisplayName} joined voice");
                        SpeechHelper.StartListen(user);
                    }
                }

                if (oldState.VoiceChannel is not null && newState.VoiceChannel is not null)
                {
                    if (oldState.VoiceChannel.Id == newState.VoiceChannel.Id)
                        return;

                    if (client.VoiceChannelId == oldState.VoiceChannel.Id)
                    {
                        Console.WriteLine($"{user.DisplayName} leaved voice");
                        await SpeechHelper.StopListen(user);
                    }
                    else if (client.VoiceChannelId == newState.VoiceChannel.Id)
                    {
                        Console.WriteLine($"{user.DisplayName} joined voice");
                        SpeechHelper.StartListen(user);
                    }
                }

                if (oldState.VoiceChannel is not null && newState.VoiceChannel is null)
                {
                    if (client.VoiceChannelId == oldState.VoiceChannel.Id)
                    {
                        Console.WriteLine($"{user.DisplayName} leaved voice");
                        await SpeechHelper.StopListen(user);
                    }
                }
            }
        }

        public static string GetAudioFullPath(string fileName)
        {
            var directory = Directory.GetCurrentDirectory();
            return directory + Path.DirectorySeparatorChar + fileName;
        }
    }
}
