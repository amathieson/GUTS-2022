using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialCommClient.Models
{
    enum NetworkPacketId
    {
        CONNECT         = 0x00,
        CONNECT_OK      = 0x01,
        CONNECT_FAILED  = 0x02,
        PING            = 0x03,
        PONG            = 0x04,
    }
}
