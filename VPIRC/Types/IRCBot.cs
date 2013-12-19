using Meebey.SmartIrc4net;
using System;
using System.Text;
using System.Threading.Tasks;

namespace VPIRC
{
    public delegate void IRCQueryArgs(IRCBot recipient, IrcEventArgs e, bool action);

    public class IRCBot : IDisposable
    {
        const string tag = "IRCBot";

        public static DateTime LastAttemptThrottle = TDateTime.UnixEpoch;

        public event Action       Connected;
        public event Action       Disposing;
        public event IRCQueryArgs Query;

        public readonly VPUser    User;
        public readonly IrcClient Client = new IrcClient();

        protected string name;
        /// <summary>
        /// Gets the name of this bot
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        ConnState state = ConnState.Disconnected;
        /// <summary>
        /// Gets the connection state of this bot
        /// </summary>
        public ConnState State
        {
            get { return state; }
        }

        DateTime lastAttempt = TDateTime.UnixEpoch;
        /// <summary>
        /// Gets the timestamp of the last connection attempt of this bot
        /// </summary>
        public DateTime LastAttempt
        {
            get { return lastAttempt; }
        }

        DateTime lastConnect = TDateTime.UnixEpoch;
        /// <summary>
        /// Gets the timestamp of the last successful connection to this bot
        /// </summary>
        public DateTime LastConnect
        {
            get { return lastConnect; }
        }

        public IRCBot(VPUser user)
        {
            this.User = user;
            this.name = VPIRC.IRC.Prefix + user.Name.Replace(" ","");
            Client.ActiveChannelSyncing = true;
            Client.Encoding             = Encoding.UTF8;

            registerEvents();
            Log.Fine(tag, "Created IRC client for VP user '{0}'", user);
        }

        protected void registerEvents()
        {
            Client.OnKick         += onKick;
            Client.OnError        += onError;
            Client.OnErrorMessage += onError;
            Client.OnDisconnected += onDisconnect;
            Client.OnQueryMessage += onQuery;
            Client.OnQueryAction  += onQueryAction;
        }

        public void Dispose()
        {
            if (Disposing != null)
                Disposing();

            Client.OnDisconnected -= onDisconnect;
            Client.OnError        -= onError;
            Client.OnKick         -= onKick;
            Client.OnQueryMessage -= onQuery;
            Client.OnQueryAction  -= onQueryAction;
            Client.RfcQuit("User has left world");
            Client.Disconnect();

            Connected = null;
            Disposing = null;
            Query     = null;
        }

        protected IRCBot() { }

        public async void Connect()
        {
            state = ConnState.Connecting;

            await Task.Run( () => {
                var hostname = VPIRC.Settings.IRC["Hostname"];
                var port     = int.Parse(VPIRC.Settings.IRC["Port"]);

                var username = VPIRC.Settings.IRC["Username"];
                var realname = VPIRC.Settings.IRC["Realname"];
                var password = VPIRC.Settings.IRC["Password"];
                var nickname = Name;

                LastAttemptThrottle = DateTime.Now;
                lastAttempt         = DateTime.Now;

                try
                {
                    Client.Connect(hostname, port);
                    Client.Login(Name, realname, 0, username, password);
                    Client.RfcJoin(VPIRC.IRC.Channel);
                    Client.ListenOnce(true);

                    if (!Client.IsConnected)
                        throw new Exception("IRC did not connect");
                }
                catch (Exception e)
                {
                    Log.Warn(tag, "Client '{0}' cannot connect: {1}", Name, e.Message);

                    if (Client.IsConnected)
                        Client.Disconnect();

                    state = ConnState.Disconnected;
                    return;
                }

                Log.Debug(tag, "Connected client '{0}'", Name);
                lastConnect = DateTime.Now;
                state       = ConnState.Connected;

                if (Connected != null)
                    Connected();
            });
        }

        void onDisconnect(object sender, EventArgs e)
        {
            if (state == ConnState.Connecting)
                return;

            Log.Warn(tag, "{0} lost connection", Name);
            state = ConnState.Disconnected;
        }

        void onError(object sender, IrcEventArgs e)
        {
            Log.Warn(tag, "IRC error: {0}", e.Data.Message);
        }
        
        void onKick(object sender, KickEventArgs e)
        {
            if ( !e.Whom.IEquals(name) )
                return;

            Log.Warn(tag, "{0} got kicked; disconnecting", Name);
            Client.Disconnect();
        }

        void onQuery(object sender, IrcEventArgs e)
        {
            if (Query != null)
                Query(this, e, false);
        }

        void onQueryAction(object sender, ActionEventArgs e)
        {
            if (Query != null)
                Query(this, e, true);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
