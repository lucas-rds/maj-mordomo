﻿using System;
using System.Globalization;
using System.Text;
using ZeroMQ;

namespace MajMordomo
{

    public static class MdpExtensions
    {
        public static bool StrHexEq(this ZFrame zfrm, MdpCommon.MdpwCmd cmd)
        {
            return zfrm.ToString().ToMdCmd().Equals(cmd);
        }

        /// <summary>
        /// Parse hex value to MdpwCmd, if parsing fails, return 0
        /// </summary>
        /// <param name="hexval">hex string</param>
        /// <returns>MdpwCmd, return 0 if parsing failed</returns>
        public static MdpCommon.MdpwCmd ToMdCmd(this string hexval)
        {
            try
            {
                MdpCommon.MdpwCmd cmd = (MdpCommon.MdpwCmd)byte.Parse(hexval, NumberStyles.AllowHexSpecifier);
                return cmd;
            }
            catch (FormatException)
            {
                return 0;
            }
        }

        public static string ToHexString(this MdpCommon.MdpwCmd cmd)
        {
            return cmd.ToString("X");
        }

        public static void DumpString(this string format, params object[] args)
        {
            // if you dont wanna see utc timeshift, remove zzz and use DateTime.UtcNow instead
            Console.WriteLine("[{0}] - {1}", string.Format("{0:yyyy-MM-ddTHH:mm:ss:fffffff zzz}", DateTime.Now), string.Format(format, args));
        }

        /// <summary>
        /// Based on zmsg_dump 
        /// https://github.com/imatix/zguide/blob/f94e8995a5e02d843434ace904a7afc48e266b3f/articles/src/multithreading/zmsg.c
        /// </summary>
        /// <param name="zmsg"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void DumpZmsg(this ZMessage zmsg, string format = null, params object[] args)
        {
            if (!string.IsNullOrWhiteSpace(format))
                format.DumpString(args);
            using (var dmsg = zmsg.Duplicate())
                foreach (var zfrm in dmsg)
                {
                    zfrm.DumpZfrm();
                }
        }

        public static void DumpZfrm(this ZFrame zfrm, string format = null, params object[] args)
        {
            if (!string.IsNullOrWhiteSpace(format))
                format.DumpString(args);

            byte[] data = zfrm.Read();
            long size = zfrm.Length;

            // Dump the message as text or binary
            bool isText = true;
            for (int i = 0; i < size; i++)
                if (data[i] < 32 || data[i] > 127)
                    isText = false;
            string datastr = isText ? Encoding.UTF8.GetString(data) : data.ToHexString();
            "\tD: [{0,3:D3}]:{1}".DumpString(size, datastr);
        }
    }
}
