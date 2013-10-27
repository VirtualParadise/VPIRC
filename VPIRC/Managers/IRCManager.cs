using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;

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
            root.Client.OnJoin          += onEnter;
            root.Client.OnPart          += onLeave;
            root.Client.OnQuit          += onQuit;
            root.Client.OnDisconnected  += onDisconnect;
            root.Disposing              += onDisposing;

            root.Connect();
        }

        void onDisposing(IRCBot obj)
        {
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
            Log.Info(tag, "All bots cleared");
        }

        public IRCBot Add(string name)
        {
            var bot = new IRCBot(name);

            Log.Info(tag, "Bridging user '{0}'", name);
            bots.Add(bot);
            return bot;
        }

        public void Remove(string name)
        {
            var bot = bots.FirstOrDefault( b => b.Name.IEquals(name) );

            if (bot == null)
                return;

            Log.Info(tag, "No longer bridging user '{0}'", name);
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
            var nick = e.Who;
            if ( nick.StartsWith(Prefix) )
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
