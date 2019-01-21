using System;
using System.Threading;
using ZeroMQ;

namespace MajMordomo
{
    public static class Program
    {
        //  Finally, here is the main task. We create a new broker instance and
        //  then process messages on the broker Socket:
        public static void Main(string[] args)
        {
            CancellationTokenSource cancellor = new CancellationTokenSource();
            Console.CancelKeyPress += (s, ea) =>
            {
                ea.Cancel = true;
                cancellor.Cancel();
            };

            using (var broker = new Broker(verbose: true))
            {
                broker.Bind("tcp://127.0.0.1:5555");
                // Get and process messages forever or until interrupted
                while (true)
                {
                    if (cancellor.IsCancellationRequested
                        || (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                        broker.ShutdownContext();

                    var p = ZPollItem.CreateReceiver();
                    if (broker.Socket.PollIn(p, out var message, out var error, MdpCommon.HEARTBEAT_INTERVAL))
                    {
                        message.LogEachFrame("I: received message:");

                        using (ZFrame sender = message.Pop())
                        using (ZFrame empty = message.Pop())
                        using (ZFrame header = message.Pop())
                        {
                            switch (header.ToString())
                            {
                                case MdpCommon.MDPC_CLIENT:
                                    broker.ClientMsg(sender, message);
                                    break;
                                case MdpCommon.MDPW_WORKER:
                                    broker.WorkerMsg(sender, message);
                                    break;
                                default:
                                    message.LogEachFrame("E: invalid message:");
                                    message.Dispose();
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (Equals(error, ZError.ETERM))
                        {
                            "W: interrupt received, shutting down…".Log();
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

                        foreach (var waitingworker in broker.WaitingWorkers)
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