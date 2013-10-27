using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VPIRC
{
    class IRCManager
    {
        const string tag = "IRC";

        public event UserArgs Enter;
        public event UserArgs Leave;

        public string Prefix;
        public string Channel;
        public int    PerConnectThrottle;

        IRCBotRoot   root;
        List<IRCBot> bots  = new List<IRCBot>();
        List<User>   users = new List<User>();

        public void Setup()
        {
            Prefix  = VPIRC.Settings.IRC["Prefix"] ?? "vp-";
            Channel = VPIRC.Settings.IRC["Channel"] ?? "#vp";

            PerConnectThrottle = int.Parse( VPIRC.Settings.IRC["PerConnectThrottle"] ?? "1" );

            root = new IRCBotRoot();
            root.Client.OnNames         += onNames;
            root.Client.OnJoin          += onEnter;
            root.Client.OnPart          += onLeave;
            root.Client.OnQuit          += onQuit;
            root.Client.OnDisconnected  += onDisconnect;
            root.Disposing              += onDisposing;

            root.Connect();
        }

        void onDisposing()
        {
            root.Client.OnNames         -= onNames;
            root.Client.OnJoin          -= onEnter;
            root.Client.OnPart          -= onLeave;
            root.Client.OnQuit          -= onQuit;
            root.Client.OnDisconnected  -= onDisconnect;
            root.Disposing              -= onDisposing;
        }

        public void Takedown()
        {
            foreach (var bot in bots)
                bot.Dispose();

            bots.Clear();
            root.Dispose();
            Enter = null;
            Leave = null;
            Log.Info(tag, "All bots cleared");
        }

        public void Add(User user)
        {
            if ( GetBot(user) != null )
                return;

            Log.Info(tag, "Bridging user '{0}'", user);
            bots.Add( new IRCBot(user) );
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
                    if (IRCBot.LastAttemptThrottle.SecondsToNow() < PerConnectThrottle)
                        return;

                    if (root.LastAttempt.SecondsToNow() < 5)
                        return;

                    Log.Debug(tag, "Root bridge bot is not connected; connecting...");
                    root.Connect();
                    return;

                case ConnState.Connected:
                    root.Client.ListenOnce(false);
                    break;
            }

            foreach (var bot in bots)
            {
                switch (bot.State)
                {
                    case ConnState.Connecting:
                        return;

                    case ConnState.Disconnected:
                        if (IRCBot.LastAttemptThrottle.SecondsToNow() < PerConnectThrottle)
                            return;

                        if (bot.LastAttempt.SecondsToNow() < 5)
                            return;

                        Log.Debug(tag, "User bot '{0}' is not connected; connecting...", bot);
                        bot.Connect();
                        return;

                    case ConnState.Connected:
                        bot.Client.ListenOnce(false);
                        break;
                }
            }
        }

        public User GetUser(string name)
        {
            return users.Where( u => u.Name.IEquals(name) ).FirstOrDefault();
        }

        public IRCBot GetBot(User user)
        {
            return bots.Where( b => b.User.Equals(user) ).FirstOrDefault();
        }

        void onDisconnect(object sender, EventArgs e)
        {
            Log.Warn(tag, "Bridge client disconnected; clearing all users");

            if (Leave != null)
                foreach (var user in users)
                    Leave(user);

            users.Clear();
        }

        void onEnter(object sender, JoinEventArgs e)
        {
            enter(e.Who);
        }

        void onNames(object sender, NamesEventArgs e)
        {
            foreach (var name in e.UserList)
                enter(name);
        }

        void enter(string nick)
        {
            if ( string.IsNullOrWhiteSpace(nick) )
                return;

            if ( nick.StartsWith(Prefix) || nick.IEquals(root.Name) )
                return;

            var user = new User(nick, Side.IRC);
            users.Add(user);

            if (Enter != null)
                Enter(user);
        }

        void onLeave(object sender, PartEventArgs e)
        {
            leave(e.Who);
        }

        void onQuit(object sender, QuitEventArgs e)
        {
            leave(e.Who);
        }

        void leave(string nick)
        {
            if ( nick.StartsWith(Prefix) )
                return;

            var user = GetUser(nick);

            if (user == null)
                return;

            if (Leave != null)
                Leave(user);

            users.Remove(user);
        }
    }
}
