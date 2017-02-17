using Discord;
using Discord.Commands;

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

            WelcomeMessage();
            LeaveMessage();
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

                if (e.User.Id == 129323526267207680) //Automatically adds the role saucisse to Ovoui
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
                    if (amountToDelete > 1) plural = "s";

                    Message[] messagesToDelete = await e.Channel.DownloadMessages(amountToDelete); ;
                    await e.Channel.DeleteMessages(messagesToDelete);

                    var commandMessage = await e.Channel.SendMessage($"Deleted {amountToDelete} message{plural}!");
                    await Task.Delay(1337);
                    await commandMessage.Delete();

                    await Logging($"{amountToDelete} message{plural} have been deleted in {e.Server.Name} (Channel: #{e.Channel.Name}) by {e.User.Mention}.");
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
                if (e.User.Id == 129323526267207680 && e.Server.Id == 210518320888152065)
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
        }
    }
}
