using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VPIRC
{
    public delegate void IrcMessageArgs(IRCUser source, string message, bool action);

    class IRCManager
    {
        const string tag = "IRC";

        public event IRCUserArgs    Enter;
        public event IRCUserArgs    Leave;
        public event IrcMessageArgs Message;

        public string Prefix;
        public string Channel;
        public int    PerConnectThrottle;

        IRCBotRoot root;
        public IRCBotRoot Root
        {
            get { return root; }
        }

        List<IRCBot>  bots  = new List<IRCBot>();
        List<IRCUser> users = new List<IRCUser>();

        public void Setup()
        {
            Prefix  = VPIRC.Settings.IRC["Prefix"] ?? "vp-";
            Channel = VPIRC.Settings.IRC["Channel"] ?? "#vp";

            PerConnectThrottle = int.Parse( VPIRC.Settings.IRC["PerConnectThrottle"] ?? "1" );

            root = new IRCBotRoot();
            root.Client.OnNames          += onNames;
            root.Client.OnJoin           += onEnter;
            root.Client.OnPart           += onLeave;
            root.Client.OnQuit           += onQuit;
            root.Client.OnDisconnected   += onDisconnect;
            root.Client.OnChannelMessage += onMessage;
            root.Client.OnChannelAction  += onAction;
            root.Client.OnNickChange     += onNickChange;
            root.Disposing               += onDisposing;

            root.Connect();
        }

        void onDisposing()
        {
            root.Client.OnNames          -= onNames;
            root.Client.OnJoin           -= onEnter;
            root.Client.OnPart           -= onLeave;
            root.Client.OnQuit           -= onQuit;
            root.Client.OnDisconnected   -= onDisconnect;
            root.Client.OnChannelMessage -= onMessage;
            root.Client.OnChannelAction  -= onAction;
            root.Client.OnNickChange     -= onNickChange;
            root.Disposing               -= onDisposing;
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

        public void Add(VPUser user)
        {
            if ( GetBot(user) != null )
                return;

            Log.Info(tag, "Bridging user '{0}'", user);
            bots.Add( new IRCBot(user) );
        }

        public void Remove(VPUser user)
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
                Thread.Sleep(10);

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

        public IRCUser GetUser(string name)
        {
            return users.Where( u => u.Name.IEquals(name) ).FirstOrDefault();
        }

        public IRCBot GetBot(VPUser user)
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
            onEnter(e.Data.Nick);
        }

        void onNames(object sender, NamesEventArgs e)
        {
            foreach (var name in e.UserList)
               onEnter( name.TrimStart('~', '+', '@', '&', '%') );
        }

        void onEnter(string nick)
        {
            if ( string.IsNullOrWhiteSpace(nick) )
                return;

            if ( nick.StartsWith(Prefix) || nick.IEquals(root.Name) )
                return;

            var user = new IRCUser(nick);
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

        void onMessage(object sender, IrcEventArgs e)
        {
            message(e.Data.Nick, e.Data.Message, false);
        }

        void onAction(object sender, ActionEventArgs e)
        {
            message(e.Data.Nick, e.ActionMessage, true);
        }

        void message(string name, string incoming, bool action)
        {
            var user = GetUser(name);
            // Needed due to bug with SmartIrc4Net not obeying encoding
            var bytes    = Encoding.Default.GetBytes(incoming);
            var fixedMsg = Encoding.UTF8.GetString(bytes);

            if (user == null)
                return;

            if (Message != null)
                Message(user, fixedMsg, action);
        }
        
        void onNickChange(object sender, NickChangeEventArgs e)
        {
            leave(e.OldNickname);
            onEnter(e.NewNickname);
        }
    }
}
