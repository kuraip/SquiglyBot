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
                string[] ub_users = System.IO.File.ReadAllLines("users.ub");
                string[] ub_servers = System.IO.File.ReadAllLines("servers.ub");

                if (ub_servers.Contains(e.Server.Id.ToString())) await e.User.AddRoles(e.Server.FindRoles("waf").First());

                await Logging($"{e.User.Mention} has joined {e.Server.Name}.");

                if (ub_users.Contains(e.User.Id.ToString()) && ub_servers.Contains(e.Server.Id.ToString()))
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

                string[] ub_users = System.IO.File.ReadAllLines("users.ub");
                string[] ub_servers = System.IO.File.ReadAllLines("servers.ub");

                if (ub_users.Contains(e.User.Id.ToString()) && ub_servers.Contains(e.Server.Id.ToString()))
                {
                    Invite inviteCode = await e.Server.CreateInvite(maxAge: null, maxUses: 1, tempMembership: false, withXkcd: true);

                    await e.Server.Unban(e.User.Id);
                    Channel userdm = await e.User.CreatePMChannel();
                    await userdm.SendMessage($"Tu viens de te faire ban, kick ou alors tu as quitté le serveur '{e.Server.Name}'.");
                    await userdm.SendMessage($"Voici un lien pour revenir: {inviteCode.Url.Replace("//", "/")} ♥");

                    await Logging($"User {e.User.Mention} has been reinvited to server {e.Server.Name}.");
                }
            };
        }

        private void Updates()
        {
            discord.UserUnbanned += async (s, e) =>
            {
                string[] ub_users = System.IO.File.ReadAllLines("users.ub");
                string[] ub_servers = System.IO.File.ReadAllLines("servers.ub");

                if (!ub_users.Contains(e.User.Id.ToString()) && ub_servers.Contains(e.Server.Id.ToString()))
                {
                    await Logging($"{e.User.Mention} has been unbanned from {e.Server.Name}.");
                }
                    
            };
        }

        private void RegisterVocalCommands()
        {
            commands.CreateCommand("music")
                .Parameter("action", Discord.Commands.ParameterType.Required)
                .Alias(new string[] { "vocal", "song", "v", "radio" })
                .Do(async (e) =>
                {
                    string action = e.GetArg("action");
                    var logChannel = e.Server.FindChannels("vocal").FirstOrDefault();

                    var voiceChannel = discord.GetServer(e.Server.Id).VoiceChannels.FirstOrDefault();
                    var _vClient = await discord.GetService<AudioService>().Join(voiceChannel);

                    string folderPath = "C:\\Documents\\Musiques\\";
                    Random rnd = new Random();
                    IEnumerable<string> musicPlaylist = System.IO.Directory.GetFiles(folderPath, "*.mp3", System.IO.SearchOption.AllDirectories).OrderBy(x => rnd.Next());
                    IEnumerator<string> musicEnumerator = musicPlaylist.GetEnumerator();

                    switch (action)
                    {
                        case "kick":

                            await _vClient.Disconnect();
                            await LogAudio($"Left voice channel '{voiceChannel.Name}' in '{e.Server.Name}' (by {e.User.Name})");
                        break;

                        case "play":

                            musicEnumerator.MoveNext();
                            GetTags Tags = new GetTags();
                            string textToLog = $"Playing: {Tags.NowPlaying(musicEnumerator.Current)}.";
                            await LogAudio(textToLog + " [Server: " + e.Server.Name + "]");
                            await logChannel.SendMessage(textToLog);
                            SendAudio(musicEnumerator.Current, _vClient);
                        goto case "play";
                    }
                });
        } //plays music from a set folder until it is kicked.

        public void SendAudio(string filePath, IAudioClient _vClient)
        {
            var channelCount = discord.GetService<AudioService>().Config.Channels; 
            var OutFormat = new WaveFormat(48000, 16, channelCount); 
            using (var MP3Reader = new Mp3FileReader(filePath))
            using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) 
            {
                resampler.ResamplerQuality = 60; 
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
                    _vClient.Send(buffer, 0, blockSize);
                }  
            }
        } //sends a mp3 to Discord

        public async Task Logging(string outputString)
        {
            await discord.GetServer(226363209102262272).GetChannel(226363933185933313).SendMessage(outputString);
            Console.WriteLine(outputString);
        } //logs in #logs

        public async Task LogAudio(string outputString)
        {
            await discord.GetServer(226363209102262272).GetChannel(282870791052460033).SendMessage(outputString);
            Console.WriteLine(outputString);
        } //logs in #audio

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
                .Parameter("admin", Discord.Commands.ParameterType.Optional)
                .Do(async (e) =>
                {
                    object[] helpText =
                    {
                        "```",
                        "\n!ping: PONG!",
                        "\n!jail: Sends you to jail.",
                        "\n!music {play|kick}: Plays a song from a set directory.",
                        "```"
                    };

                    object[] adminHelpText =
                    {
                        "```",
                        "\n!purge <1-10>: Deletes from 1 to 10 messages. (admin only)",
                        "\n!kick <@username>: Kicks someone (admin only).",
                        "\n!ban <@username>: Bans someone (admin only).",
                        "```"
                    };

                    string textToOutput;
                    if (e.GetArg("admin") == "admin") textToOutput = string.Concat(adminHelpText);
                    else textToOutput = string.Concat(helpText);

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

                    await Logging($"<@!{username}> has been kicked from {e.Server.Name} by {e.User.Mention}.");
                });
        }

        private void RegisterPurgeCommand()
        {
            commands.CreateCommand("purge")
                .Alias(new string[] { "remove" })
                .AddCheck((cm, u, ch) => u.ServerPermissions.Administrator)
                .Parameter("amount", Discord.Commands.ParameterType.Optional)
                .Do(async (e) =>
                {
                    int amountToDelete = 1;
                    int.TryParse(e.GetArg("amount"), out amountToDelete);

                    if (amountToDelete > 10) amountToDelete = 10;
                    else if (amountToDelete < 1) amountToDelete = 1;

                    string message = "message";
                    string have = "has";
                   
                    if (amountToDelete > 1)
                    {
                        message = "messages";
                        have = "have";
                    }

                    Message[] messagesToDelete = await e.Channel.DownloadMessages(amountToDelete + 1); ;
                    await e.Channel.DeleteMessages(messagesToDelete);

                    var commandMessage = await e.Channel.SendMessage($"Deleted {amountToDelete} {message}!");
                    await Task.Delay(1337);
                    await commandMessage.Delete();

                    await Logging($"{amountToDelete} {message} {have} been deleted in {e.Server.Name} (Channel: #{e.Channel.Name}) by {e.User.Mention}.");
                });
        }

        private void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
