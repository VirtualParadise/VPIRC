using System;
using System.Collections.Generic;
using System.Linq;
using VP;

namespace VPIRC
{
    public delegate void MessageArgs(User source, string message);

    class VPManager
    {
        const string tag = "Virtual Paradise";

        public event UserArgs    Enter;
        public event UserArgs    Leave;
        public event MessageArgs Message;

        public string Prefix;
        public string World;

        VPBotRoot root;
        public VPBotRoot Root
        {
            get { return root; }
        }

        List<VPBot> bots  = new List<VPBot>();
        List<User>  users = new List<User>();

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

        public void Add(User user)
        {
            if ( GetBot(user) != null )
                return;

            Log.Info(tag, "Bridging user '{0}'", user);
            bots.Add( new VPBot(user) );
        }

        public void Remove(User user)
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

        public User GetUser(string name)
        {
            return users.Where( u => u.Name.IEquals(name) ).FirstOrDefault();
        }

        public VPBot GetBot(User user)
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

            var user = GetUser(avatar.Name);

            if (user != null)
            {
                user.Instances++;
                return;
            }

            user = new User(avatar.Name, Side.VirtualParadise);
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
            
            user.Instances--;
            if (user.Instances > 0)
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
            message(console.Name, console.Message);
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
