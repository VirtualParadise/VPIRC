using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VP;

namespace VPIRC
{
    public delegate void VPMessageArgs(VPUser source, string message);
    public delegate void ConsoleMessageArgs(ConsoleMessage message);

    class VPManager
    {
        const string tag = "Virtual Paradise";

        public event VPUserArgs         Enter;
        public event VPUserArgs         Leave;
        public event VPMessageArgs      Message;
        public event ConsoleMessageArgs Console;

        public string Prefix;
        public string World;

        VPBotRoot root;
        public VPBotRoot Root
        {
            get { return root; }
        }

        List<VPBot>  bots  = new List<VPBot>();
        List<VPUser> users = new List<VPUser>();

        public void Setup()
        {
            Prefix = VPIRC.Settings.VP["Prefix"] ?? "irc-";
            World  = VPIRC.Settings.VP["World"] ?? "test";

            root = new VPBotRoot();
            root.Bot.Avatars.Enter      += onAvatarEnter;
            root.Bot.Avatars.Leave      += onAvatarLeave;
            root.Bot.Chat               += onChat;
            root.Bot.Console            += onConsole;
            root.Bot.UniverseDisconnect += onDisconnect;
            root.Bot.WorldDisconnect    += onDisconnect;

            root.Connect();
        }

        public void Takedown()
        {
            foreach (var bot in bots)
                bot.Dispose();

            bots.Clear();
            root.Dispose();
            Enter   = null;
            Leave   = null;
            Message = null;
            Log.Info(tag, "All bots cleared");
        }

        public void Add(IRCUser user)
        {
            if ( GetBot(user) != null )
                return;

            Log.Info(tag, "Bridging user '{0}'", user);
            bots.Add( new VPBot(user) );
        }

        public void Remove(IRCUser user)
        {
            var bot = GetBot(user);

            if (bot == null)
                return;

            Log.Info(tag, "No longer bridging user '{0}'", user);
            bot.Dispose();
            bots.Remove(bot);
        }

        public void Update()
        {
            switch (root.State)
            {
                case ConnState.Connecting:
                    return;

                case ConnState.Disconnected:
                    if (root.LastAttempt.SecondsToNow() < 5)
                        return;

                    Log.Debug(tag, "Root bridge bot is not connected; connecting...");
                    root.Connect();
                    return;

                case ConnState.Connected:
                    root.Bot.Pump();
                    break;
            }

            foreach (var bot in bots)
            {
                Thread.Sleep(10);

                switch (bot.State)
                {
                    case ConnState.Connecting:
                        return;

                    case ConnState.Disconnected:
                        if (bot.LastAttempt.SecondsToNow() < 5)
                            return;

                        Log.Debug(tag, "User bot '{0}' is not connected; connecting...", bot);
                        bot.Connect();
                        return;

                    case ConnState.Connected:
                        bot.Bot.Pump();
                        break;
                }
            }
        }

        public VPUser GetUser(string name)
        {
            return users.Where( u => u.Name.IEquals(name) ).FirstOrDefault();
        }

        public VPBot GetBot(IRCUser user)
        {
            return bots.Where( b => b.User.Equals(user) ).FirstOrDefault();
        }

        void onDisconnect(Instance sender, int error)
        {
            Log.Warn(tag, "Bridge bot disconnected; clearing all users");

            if (Leave != null)
                foreach (var user in users)
                    Leave(user);

            users.Clear();
        }

        void onAvatarEnter(Instance sender, Avatar avatar)
        {
            if ( avatar.Name.StartsWith("[" + Prefix) || avatar.Name.IEquals(root.Name) )
                return;

            var user = GetUser(avatar.Name) ?? new VPUser(avatar.Name);
            user.Sessions.Add(avatar.Session);

            if ( !users.Contains(user) )
                users.Add(user);

            if (Enter != null)
                Enter(user);
        }

        void onAvatarLeave(Instance sender, string name, int session)
        {
            if ( name.StartsWith("[" + Prefix) )
                return;

            var user = GetUser(name);

            if (user == null)
                return;
            
            user.Sessions.Remove(session);
            if (user.Sessions.Count > 0)
                return;

            if (Leave != null)
                Leave(user);

            users.Remove(user);
        }

        void onChat(Instance sender, ChatMessage chat)
        {
            message(chat.Name, chat.Message);
        }

        void onConsole(Instance sender, ConsoleMessage console)
        {
            if (Console != null)
                Console(console);
        }

        void message(string name, string message)
        {
            var user = GetUser(name);

            if (user == null)
                return;

            if (Message != null)
                Message(user, message);
        }
    }
}
