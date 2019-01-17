using System;
using System.Threading;
using ZeroMQ;

namespace MajMordomo
{
    public static class Program
    {
        //  Finally, here is the main task. We create a new broker instance and
        //  then process messages on the broker Socket:
        public static void MDBroker(string[] args)
        {
            CancellationTokenSource cancellor = new CancellationTokenSource();
            Console.CancelKeyPress += (s, ea) =>
            {
                ea.Cancel = true;
                cancellor.Cancel();
            };

            var verbose = true;

            using (Broker broker = new Broker(verbose))
            {
                broker.Bind("tcp://*:5555");
                // Get and process messages forever or until interrupted
                while (true)
                {
                    if (cancellor.IsCancellationRequested
                        || (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                        broker.ShutdownContext();

                    var p = ZPollItem.CreateReceiver();
                    ZMessage msg;
                    ZError error;
                    if (broker.Socket.PollIn(p, out msg, out error, MdpCommon.HEARTBEAT_INTERVAL))
                    {
                        if (verbose)
                            msg.DumpZmsg("I: received message:");

                        using (ZFrame sender = msg.Pop())
                        using (ZFrame empty = msg.Pop())
                        using (ZFrame header = msg.Pop())
                        {
                            if (header.ToString().Equals(MdpCommon.MDPC_CLIENT))
                                broker.ClientMsg(sender, msg);
                            else if (header.ToString().Equals(MdpCommon.MDPW_WORKER))
                                broker.WorkerMsg(sender, msg);
                            else
                            {
                                msg.DumpZmsg("E: invalid message:");
                                msg.Dispose();
                            }
                        }
                    }
                    else
                    {
                        if (Equals(error, ZError.ETERM))
                        {
                            "W: interrupt received, shutting down…".DumpString();
                            break; // Interrupted
                        }
                        if (!Equals(error, ZError.EAGAIN))
                            throw new ZException(error);
                    }
                    // Disconnect and delete any expired workers
                    // Send heartbeats to idle workes if needed
                    if (DateTime.UtcNow > broker.HeartbeatAt)
                    {
                        broker.Purge();

                        foreach (var waitingworker in broker.Waiting)
                        {
                            waitingworker.Send(MdpCommon.MdpwCmd.HEARTBEAT.ToHexString(), null, null);
                        }
                        broker.HeartbeatAt = DateTime.UtcNow + MdpCommon.HEARTBEAT_INTERVAL;
                    }
                }
            }
        }
    }
}