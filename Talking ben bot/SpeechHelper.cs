using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;

using Discord;
using Discord.Audio.Streams;
using Discord.WebSocket;

namespace Talking_ben_bot
{
    public class SpeechUserListener
    {
        private readonly IGuildUser _user;
        private readonly InputStream _stream;
        private readonly Timer _timer;
        private readonly SpeechGuildListener _guildListener;

        public SpeechUserListener(IGuildUser user, Timer timer, SpeechGuildListener guildListener)
        {
            _user = user;
            _timer = timer;
            _guildListener = guildListener;

            var socketUser = user as SocketGuildUser;
            _stream = (InputStream)socketUser.AudioStream;
        }

        public async void Start()
        {
            try
            {
                var buffer = new byte[3840];
                while (_stream.CanRead)
                {
                    var count = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                    if (count > 0)
                    {
                        _guildListener.TalkingStart();

                        _timer.Stop();
                        _timer.Start();
                    }
                }
            }
            finally
            { }
        }

        //Mb gonna be used later
        public Task Stop()
        {
            return Task.CompletedTask;
        }
    }

    public class SpeechGuildListener
    {
        public const int MIN_TALK_LENGTH = 1500;

        private readonly ConcurrentDictionary<ulong, SpeechUserListener> _listeners = new();

        private readonly IGuild _guild;
        private readonly Timer _timer;

        private DateTime? _talkStart;

        public SpeechGuildListener(IGuild guild)
        {
            _guild = guild;
            _timer = new Timer(1000)
            {
                AutoReset = false
            };

            _timer.Elapsed += OnTimerProc;
        }

        public void TalkingStart()
        {
            if (_talkStart.HasValue)
                return;

            _talkStart = DateTime.Now;
        }

        public async void OnTimerProc(object sender, EventArgs e)
        {
            var talkStartTime = _talkStart.Value.AddSeconds(1);
            _talkStart = null;

            if ((DateTime.Now - talkStartTime).TotalMilliseconds < MIN_TALK_LENGTH)
                return;

            await Chance.Proc(
                new()
                {
                    new(1, async () =>
                    {
                        await AudioHelper.Play(_guild.Id, "HangUp.mp3");
                        await AudioHelper.Leave(_guild.Id);
                    }),
                    new(1, async () =>
                    {
                        await AudioHelper.Play(_guild.Id, "Pernul.mp3");
                    }),
                    new(2, async () =>
                    {
                        await AudioHelper.Play(_guild.Id, "HEHEHEHA.mp3");
                    }),
                    new(4, async () =>
                    {
                        await AudioHelper.Play(_guild.Id, "Hohoho.mp3");
                        await AudioHelper.Play(_guild.Id, "Yes.mp3");
                    }),
                    new(4, async () =>
                    {
                        await AudioHelper.Play(_guild.Id, "Hohoho.mp3");
                        await AudioHelper.Play(_guild.Id, "No.mp3");
                    }),
                    new(15, async () =>
                    {
                        await AudioHelper.Play(_guild.Id, "Hohoho.mp3");
                    }),
                    new(30, async () =>
                    {
                        await AudioHelper.Play(_guild.Id, "Yes.mp3");
                    }),
                    new(30, async () =>
                    {
                        await AudioHelper.Play(_guild.Id, "No.mp3");
                    }),
                }
            );
        }

        public async Task Stop()
        {
            foreach (var (_, listener) in _listeners)
                await listener.Stop();

            _timer.Stop();
        }

        public void AddUser(IGuildUser user)
        {
            if (_listeners.ContainsKey(user.Id))
                return;

            var listener = new SpeechUserListener(user, _timer, this);
            _listeners[user.Id] = listener;

            listener.Start();
        }

        public async Task RemoveUser(IGuildUser user)
        {
            if (_listeners.TryRemove(user.Id, out var listener))
                await listener.Stop();
        }
    }

    public static class SpeechHelper
    {
        private static readonly ConcurrentDictionary<ulong, SpeechGuildListener> _listeners = new();

        public static void StartListen(IGuildUser user)
        {
            if (user.IsBot)
                return;

            if (!_listeners.TryGetValue(user.GuildId, out var listener))
            {
                listener = new SpeechGuildListener(user.Guild);
                _listeners[user.GuildId] = listener;
            }

            Console.WriteLine($"Start listening user: {user.DisplayName}");

            listener.AddUser(user);
        }

        public static async Task StopListen(IGuildUser user)
        {
            if (user.IsBot)
                return;

            if (_listeners.TryGetValue(user.GuildId, out var listener))
            {
                Console.WriteLine($"Stop listening user: {user.DisplayName}");

                await listener.RemoveUser(user);
            }
        }

        public static async Task StopListen(IGuild guild)
        {
            if (_listeners.TryRemove(guild.Id, out var listener))
            {
                Console.WriteLine($"Stop listening guild: {guild.Name}");

                await listener.Stop();
            }
        }
    }
}
