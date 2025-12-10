using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnlimitedEventExpansion.Data
{
    public interface ISmartPhoneApi
    {
        List<string> GetPhoneNpcList();
        void SendSmartphoneMessageFromNPC(string npcName, string message);
        void SendSmartphoneMessageFromPlayer(string npcName, string message);
    }
}
