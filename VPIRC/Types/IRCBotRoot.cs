using System;
using Meebey.SmartIrc4net;

namespace VPIRC
{
    class IRCBotRoot : IRCBot
    {
        const string tag = "IRCBot Root";

        public IRCBotRoot() : base()
        {
            name = VPIRC.Settings.IRC["BridgeNick"] ?? "VP-IRC";

            this.Disposing += onDispose;

            registerRootEvents();
            registerEvents();
            Log.Fine(tag, "Created root bridge client instance");
        }

        void registerRootEvents()
        {
            this.Connected += onConnected;
        }

        void onDispose()
        {
            this.Connected -= onConnected;
            this.Disposing -= onDispose;
        }
        
        void onConnected()
        {
            Client.SendMessage(SendType.Action, VPIRC.IRC.Channel, "is bridging this channel with {0} world...".LFormat(VPIRC.VP.World) );
        }
    }
}
