using Discord;
using Discord.Commands;
using Discord.Audio;
using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using TagLib;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquiglyBot
{
    class MyBot
    {
        DiscordClient discord;
        CommandService commands;

        public MyBot()
        {

            /*COMMANDS*/

            discord = new DiscordClient(x =>
            {
                x.LogLevel = LogSeverity.Info;
                x.LogHandler = Log;
            });

            discord.UsingCommands(x =>
            {
                x.PrefixChar = '!';
                x.AllowMentionPrefix = true;
            });

            commands = discord.GetService<CommandService>();

            /*AUDIO*/

            discord.UsingAudio(x => // Opens an AudioConfigBuilder so we can configure our AudioService
            {
                x.Mode = AudioMode.Outgoing; // Tells the AudioService that we will only be sending audio
                x.EnableEncryption = false;
            });

            RegisterVocalCommands();

            /*FUNCTIONS*/

            WelcomeMessage();
            LeaveMessage();
            Updates();
            RegisterHelpCommand();
            RegisterPingCommand();
            RegisterPurgeCommand();
            RegisterBanCommand();
            RegisterKickCommand();
            RegisterJailCommand();
            AutoUnbanOvoui();

            discord.ExecuteAndWait(async () =>
            {
                string token = System.IO.File.ReadAllText(@"token.txt"); //Fetches the token to connect
                await discord.Connect(token, TokenType.Bot);
            });

        }

        private void WelcomeMessage()
        {
            discord.UserJoined += async (s, e) =>
            {
                if (e.Server.Id == 210518320888152065) await e.User.AddRoles(e.Server.FindRoles("waf").First());

                await Logging($"{e.User.Mention} has joined {e.Server.Name}.");

                if ((e.User.Id == 129323526267207680 || e.User.Id == 161905660735389696) && e.Server.Id == 210518320888152065) //Automatically adds the role saucisse to Ovoui and Renko
                {
                    await e.User.AddRoles(e.Server.FindRoles("Saucisse").First());
                }
            };
        }

        private void LeaveMessage()
        {
            discord.UserLeft += async (s, e) =>
            {
                await Logging($"{e.User.Mention} has left {e.Server.Name}.");
            };
        }

        private void Updates()
        {
            discord.UserUnbanned += async (s, e) =>
            {
                if (e.User.Id != 129323526267207680 && e.Server.Id == 210518320888152065)
                {
                    await Logging($"{e.User.Mention} has been unbanned from {e.Server.Name}.");
                }
                    
            };
        }

        private void RegisterVocalCommands()
        {
            commands.CreateCommand("v")
                .Parameter("action", Discord.Commands.ParameterType.Required)
                .Do(async (e) =>
                {
                    string action = e.GetArg("action");                    
                    var voiceChannel = discord.GetServer(e.Server.Id).VoiceChannels.FirstOrDefault();
                    var _vClient = await discord.GetService<AudioService>().Join(voiceChannel);

                    if (action == "join")
                    {
                        await LogAudio($"Joined voice channel '{voiceChannel.Name}' in '{e.Server.Name}' (by {e.User.Name})");
                    }
                    else if (action == "leave")
                    {
                        await _vClient.Disconnect();
                        await LogAudio($"Left voice channel '{voiceChannel.Name}' in '{e.Server.Name}' (by {e.User.Name})");
                    }
                    else if (action == "play")
                    {
                        string folderPath = "C:\\Documents\\Musiques\\";
                        Random rnd = new Random();
                        IEnumerable<string> musicPlaylist = System.IO.Directory.GetFiles(folderPath, "*.mp3", System.IO.SearchOption.AllDirectories).OrderBy(x => rnd.Next());
                        IEnumerator<string> musicEnumerator = musicPlaylist.GetEnumerator();
                        musicEnumerator.MoveNext();

                        TagLib.File music = TagLib.File.Create(musicEnumerator.Current);
                        String title = music.Tag.Title;
                        String artist = music.Tag.FirstPerformer;
                        String length = music.Properties.Duration.ToString(@"mm\:ss");

                        await LogAudio($"Playing: {artist} - {title} ({length}).");
                        await e.Channel.SendMessage($"Playing: {artist} - {title} ({length}).");
                        SendAudio(musicEnumerator.Current, _vClient);
                    }
                });
        }

        private void RegisterPingCommand()
        {
            commands.CreateCommand("ping")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage("PONG!");
                });
        }

        private void RegisterJailCommand()
        {
            commands.CreateCommand("jail")
                .Do(async (e) =>
                {
                    await e.Channel.SendMessage("http://kuraip.net/jail.gif");
                });
        }

        private void RegisterHelpCommand()
        {
            commands.CreateCommand("help")
                .Do(async (e) =>
                {
                    object[] helpText =
                    {
                        "```",
                        "\n!ping: Answers with 'PONG!'.",
                        "\n!purge <1-10>: Deletes from 1 to 10 messages. (admin only)",
                        "\n!kick <@username>: Kicks someone (admin only).",
                        "\n!ban <@username>: Bans someone (admin only).",
                        "\n!v {join|leave|play}: Joins or leaves the default vocal channel. Plays the Skullgirls intro theme ^^.",
                        "```"
                    };

                    string textToOutput = string.Concat(helpText);
                    await e.Channel.SendMessage(textToOutput);
                });
        }

        private void RegisterBanCommand()
        {
            commands.CreateCommand("ban")
                .AddCheck((cm, u, ch) => u.ServerPermissions.Administrator)
                .Parameter("username", Discord.Commands.ParameterType.Required)
                .Do(async (e) =>
                {
                    string username = e.GetArg("username");
                    var charsToRemove = new string[] { "<", "@", "!", ">" };
                    foreach (var c in charsToRemove)
                    {
                        username = username.Replace(c, string.Empty);
                    }
                    ulong userToBanID = 0;
                    ulong.TryParse(username, out userToBanID);
                    User userToBan = e.Server.GetUser(userToBanID);
                    await e.Server.Ban(userToBan);

                    await Logging($"<@!{username}> has been banned from {e.Server.Name} by {e.User.Mention}.");
                });
        }

        private void RegisterKickCommand()
        {
            commands.CreateCommand("kick")
                .AddCheck((cm, u, ch) => u.ServerPermissions.Administrator)
                .Parameter("username", Discord.Commands.ParameterType.Required)
                .Do(async (e) =>
                {
                    string username = e.GetArg("username");
                    var charsToRemove = new string[] { "<", "@", "!", ">" };
                    foreach (var c in charsToRemove)
                    {
                        username = username.Replace(c, string.Empty);
                    }
                    ulong userToKickID = 0;
                    ulong.TryParse(username, out userToKickID);
                    User userToKick = e.Server.GetUser(userToKickID);
                    await userToKick.Kick();

                    await Logging($"<@!{username}> has been kick from {e.Server.Name} by {e.User.Mention}.");
                });
        }

        private void RegisterPurgeCommand()
        {
            commands.CreateCommand("purge")
                .Alias(new string[] { "clear", "remove" })
                .AddCheck((cm, u, ch) => u.ServerPermissions.Administrator)
                .Parameter("amount", Discord.Commands.ParameterType.Optional)
                .Do(async (e) =>
                {
                    int amountToDelete = 1;
                    int.TryParse(e.GetArg("amount"), out amountToDelete);

                    if (amountToDelete > 10) amountToDelete = 10;
                    else if (amountToDelete < 1) amountToDelete = 1;

                    string plural = "";
                    string singular = "ve";
                    if (amountToDelete > 1)
                    {
                        plural = "s";
                        singular = "";
                    }

                    Message[] messagesToDelete = await e.Channel.DownloadMessages(amountToDelete + 1); ;
                    await e.Channel.DeleteMessages(messagesToDelete);

                    var commandMessage = await e.Channel.SendMessage($"Deleted {amountToDelete} message{plural}!");
                    await Task.Delay(1337);
                    await commandMessage.Delete();

                    await Logging($"{amountToDelete} message{plural} ha{singular}{plural} been deleted in {e.Server.Name} (Channel: #{e.Channel.Name}) by {e.User.Mention}.");
                });
        }
        
        private void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        private void AutoUnbanOvoui() 
        {
            discord.UserBanned += async (s, e) =>
            {
                if ((e.User.Id == 129323526267207680 || e.User.Id == 161905660735389696) && e.Server.Id == 210518320888152065)
                {
                    var logChannel = e.Server.FindChannels("waf").FirstOrDefault();
                    await e.Server.Unban(e.User.Id);
                    Channel userdm = await e.User.CreatePMChannel();
                    await userdm.SendMessage("Tu t'es encore fais ban, tiens, un lien pour revenir : https://discord.gg/HccdzR8 :')");
                    await Logging($"{e.User.Mention} has been automatically unbanned from {e.Server.Name}.");
                }
            };
        }

        public async Task Logging(string outputString)
        {
            await discord.GetServer(226363209102262272).GetChannel(226363933185933313).SendMessage(outputString);
            Console.WriteLine(outputString);
        }

        public async Task LogAudio(string outputString)
        {
            await discord.GetServer(226363209102262272).GetChannel(282870791052460033).SendMessage(outputString);
            Console.WriteLine(outputString);
        }

        public void SendAudio(string filePath, IAudioClient _vClient)
        {
            var channelCount = discord.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
            var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
            using (var MP3Reader = new Mp3FileReader(filePath)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
            using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
            {
                resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of the AudioBuffer
                byte[] buffer = new byte[blockSize];
                int byteCount;

                while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) // Read audio into the buffer, and keep a loop open while data is present
                {   
                    if (byteCount < blockSize)
                    {
                        // Incomplete Frame
                        for (int i = byteCount; i < blockSize; i++)
                            buffer[i] = 0;
                    }
                    
                    _vClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                    
                }
                
            }

        }
    }
}
