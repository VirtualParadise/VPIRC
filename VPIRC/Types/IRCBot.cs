using Meebey.SmartIrc4net;
using System;
using System.Threading.Tasks;

namespace VPIRC
{
    class IRCBot : IDisposable
    {
        const string tag = "IRCBot";
        public static DateTime LastAttemptThrottle = TDateTime.UnixEpoch;

        public event Action Connected;
        public event Action Disposing;

        public readonly User      User;
        public readonly IrcClient Client = new IrcClient();

        string lastError;

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

        public IRCBot(User user)
        {
            if (user.Side != Side.VirtualParadise)
                throw new InvalidOperationException("Tried to create IRC bot for an IRC user (wrong side)");

            this.User = user;
            this.name = VPIRC.IRC.Prefix + user.Name;

            registerEvents();
            Log.Fine(tag, "Created IRC client for VP user '{0}'", user);
        }

        protected void registerEvents()
        {
            Client.OnError        += onError;
            Client.OnDisconnected += onDisconnect;
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

        void onError(object sender, ErrorEventArgs e)
        {
            lastError = e.ErrorMessage;
            Log.Warn(tag, "IRC error: {0}", e.ErrorMessage);
        }

        public void Dispose()
        {
            if (Disposing != null)
                Disposing();

            Client.OnDisconnected -= onDisconnect;
            Client.OnError        -= onError;
            Client.RfcQuit();
            Client.Disconnect();

            Connected = null;
            Disposing = null;
        }
    }
}
