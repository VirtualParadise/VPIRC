using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;
using VP;

namespace VPIRC
{
    class BridgeManager
    {
        const string tag = "Bridge";

        public void Setup()
        {
            VPIRC.VP.Enter  += u => { onEnterLeave(u, Direction.Entering); };
            VPIRC.VP.Leave  += u => { onEnterLeave(u, Direction.Leaving); };
            VPIRC.IRC.Enter += u => { onEnterLeave(u, Direction.Entering); };
            VPIRC.IRC.Leave += u => { onEnterLeave(u, Direction.Leaving); };
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

        public void Takedown()
        {

        }
    }
}
