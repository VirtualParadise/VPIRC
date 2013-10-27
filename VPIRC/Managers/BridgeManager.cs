using System;
using VP;
using Meebey.SmartIrc4net;

namespace VPIRC
{
    class BridgeManager
    {
        const string tag = "Bridge";

        public void Setup()
        {
            VPIRC.VP.Enter   += u => { onEnterLeave(u, Direction.Entering); };
            VPIRC.VP.Leave   += u => { onEnterLeave(u, Direction.Leaving); };
            VPIRC.VP.Message += onVPMessage;

            VPIRC.IRC.Enter   += u => { onEnterLeave(u, Direction.Entering); };
            VPIRC.IRC.Leave   += u => { onEnterLeave(u, Direction.Leaving); };
            VPIRC.IRC.Message += onIRCMessage;
        }

        public void Takedown() { }

        void onVPMessage(User source, string message)
        {
            SendType sendType;
            var bot = VPIRC.IRC.GetBot(source);

            if (bot == null || bot.State != ConnState.Connected)
                return;

            if ( message.StartsWith("/me ") )
            {
                sendType = SendType.Action;
                message  = message.Substring(4);
            }
            else
                sendType = SendType.Message;

            bot.Client.SendMessage(sendType, VPIRC.IRC.Channel, message);
        }
        
        void onIRCMessage(User source, string message, bool action)
        {
            var bot    = VPIRC.VP.GetBot(source);
            var prefix = action ? "/me " : "";

            if (bot == null || bot.State != ConnState.Connected)
                return;

            if (message.Length <= 250)
                bot.Bot.Say("{0}{1}", prefix, message);
            else while (message.Length > 0)
            {
                var len   = Math.Min(message.Length, 250);
                var chunk = message.Substring(0, len);
                message   = message.Substring(len);

                bot.Bot.Say("{0}{1}", prefix, chunk);
            }
        }

        void onEnterLeave(User user, Direction dir)
        {
            if (user.Side == Side.IRC)
            {
                if (dir == Direction.Entering)
                    VPIRC.VP.Add(user);
                else
                    VPIRC.VP.Remove(user);
            }
            else
            {
                if (dir == Direction.Entering)
                    VPIRC.IRC.Add(user);
                else
                    VPIRC.IRC.Remove(user);
            }
        }
    }
}
