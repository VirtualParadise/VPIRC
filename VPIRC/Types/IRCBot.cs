using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetIrc2;
using NetIrc2.Events;

namespace VPIRC
{
    class IRCBot : IDisposable
    {
        const string tag = "IRCBot";

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

        public IRCBot(string name)
        {
            this.name = VPIRC.IRC.Prefix + name;

            Client.GotIrcError += onError;

            Log.Fine(tag, "Created IRC client for user '{0}'", Name);
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
                lastAttempt  = DateTime.Now;

                try
                {
                    Client.Connect(hostname, port);
                    Client.LogIn(username, realname, nickname, null, null, password);
                    Client.Join(VPIRC.IRC.Channel);
                }
                catch (Exception e)
                {
                    Log.Warn(tag, "Client '{0}' cannot connect: {1}", Name, e.Message);

                    state = ConnState.Disconnected;
                    return;
                }

                Log.Debug(tag, "Connected client '{0}'", Name);
                lastConnect = DateTime.Now;
                state       = ConnState.Connected;
            });
        }

        void onError(object s, IrcErrorEventArgs e)
        {
            Log.Warn(tag, "Error: {0}", e.Error);
        }

        public void Dispose()
        {
            Client.GotIrcError -= onError;
            Client.Close();
        }
    }
}
