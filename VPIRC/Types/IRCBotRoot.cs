using System;

namespace VPIRC
{
    class IRCBotRoot : IRCBot
    {
        const string tag = "IRCBot Root";

        public IRCBotRoot() : base()
        {
            name = VPIRC.Settings.IRC["BridgeNick"] ?? "VP-IRC";
            
            registerEvents();
            Log.Fine(tag, "Created root bridge bot instance");
        }

    }
}
