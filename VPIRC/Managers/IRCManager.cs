using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VPIRC
{
    public delegate void IRCMessageArgs(IRCUser source, string message, bool action);
    public delegate void IRCPrivMessageArgs(IRCUser source, VPUser target, string message, bool action);

    class IRCManager
    {
        const string tag = "IRC";

        public event IRCUserArgs        Enter;
        public event IRCUserArgs        Leave;
        public event IRCMessageArgs     Message;
        public event IRCPrivMessageArgs PrivMessage;

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

            var bot = new IRCBot(user);
            Log.Info(tag, "Bridging user '{0}'", user);
            bots.Add(bot);
            bot.Query += onPrivateMessage;
        }

        public void Remove(VPUser user)
        {
            var bot = GetBot(user);

            if (bot == null)
                return;

            Log.Info(tag, "No longer bridging user '{0}'", user);
            bot.Query -= onPrivateMessage;
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
            onLeave(e.Who);
        }

        void onQuit(object sender, QuitEventArgs e)
        {
            onLeave(e.Who);
        }

        void onLeave(string nick)
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

        void onNickChange(object sender, NickChangeEventArgs e)
        {
            onLeave(e.OldNickname);
            onEnter(e.NewNickname);
        }

        void onMessage(object sender, IrcEventArgs e)
        {
            onMessage(e.Data.Nick, e.Data.Message, false);
        }

        void onAction(object sender, ActionEventArgs e)
        {
            onMessage(e.Data.Nick, e.ActionMessage, true);
        }

        void onMessage(string name, string incoming, bool action)
        {
            var user = GetUser(name);
            // Needed due to bug with SmartIrc4Net not obeying encoding
            var fixedMsg = Unicode.FixFromDefault(incoming);

            if (user == null)
                return;

            if (Message != null)
                Message(user, fixedMsg, action);
        }

        void onPrivateMessage(IRCBot recipient, IrcEventArgs e, bool action)
        {
            string fixedMsg;
            var    source = GetUser(e.Data.Nick);
            var    target = recipient.User;

            if (source == null)
                return;

            if (action)
            {
                var actionEvent = e as ActionEventArgs;
                fixedMsg = Unicode.FixFromDefault(actionEvent.ActionMessage);
            }
            else
                fixedMsg = Unicode.FixFromDefault(e.Data.Message);

            if (PrivMessage != null)
                PrivMessage(source, target, fixedMsg, action);
        }
    }
}
