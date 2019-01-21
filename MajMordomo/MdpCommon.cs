using System;

namespace MajMordomo
{
    public static class MdpCommon
    {
        public const int HEARTBEAT_LIVENESS = 3;

        public static readonly TimeSpan HEARTBEAT_DELAY = TimeSpan.FromMilliseconds(2500);
        public static readonly TimeSpan RECONNECT_DELAY = TimeSpan.FromMilliseconds(2500);

        public static readonly TimeSpan HEARTBEAT_INTERVAL = TimeSpan.FromMilliseconds(2500);
        public static readonly TimeSpan HEARTBEAT_EXPIRY =
            TimeSpan.FromMilliseconds(HEARTBEAT_INTERVAL.TotalMilliseconds * HEARTBEAT_LIVENESS);

        public const string MDPW_WORKER = "MDPW01";
        public const string MDPC_CLIENT = "MDPC01";

        //public static readonly string READY = "001";
        //public static readonly string REQUEST = "002";
        //public static readonly string REPLY = "003";
        //public static readonly string HEARTBEAT = "004";
        //public static readonly string DISCONNECT = "005";

        public enum MdpwCmd : byte { READY = 1, REQUEST = 2, REPLY = 3, HEARTBEAT = 4, DISCONNECT = 5 }
    }
}