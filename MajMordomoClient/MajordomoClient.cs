using MajMordomo;
using System;
using System.Threading;
using ZeroMQ;

namespace MajMordomoClient
{
    public class MajordomoClient : IDisposable
    {
        //  Structure of our class
        //  We access these properties only via class methods

        // Our context
        private readonly ZContext _context;

        // Majordomo broker
        private readonly string _brokerEndpoint;

        //  Socket to broker
        private ZSocket _client;

        //  Print activity to console
        private readonly bool _verbose;

        //  Request timeout
        private readonly TimeSpan _timeout;

        //  Request retries
        private readonly int _retries;

        public void ConnectToBroker()
        {
            //  Connect or reconnect to broker
            _client = new ZSocket(_context, ZSocketType.REQ);
            _client.Connect(_brokerEndpoint);
            if (_verbose)
                "I: connecting to broker at '{0}'…".Log(_brokerEndpoint);
        }

        public MajordomoClient(string brokerEndpoint, bool verbose = true)
        {
            _brokerEndpoint = brokerEndpoint ?? throw new InvalidOperationException();
            _context = new ZContext();
            _verbose = verbose;
            _timeout = TimeSpan.MaxValue;
            _retries = 3;

            ConnectToBroker();
        }

        ~MajordomoClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Destructor

                if (_client != null)
                {
                    _client.Dispose();
                    _client = null;
                }
                ////Do not Dispose Context: cuz of weird shutdown behavior, stucks in using calls //
            }
        }

        //  Here is the send method. It sends a request to the broker and gets
        //  a reply even if it has to retry several times. It takes ownership of //
        //  the request message, and destroys it when sent. It returns the reply
        //  message, or NULL if there was no reply after multiple attempts://
        public ZMessage Send(string service, ZMessage request, CancellationTokenSource cancellor)
        {
            if (request == null)
                throw new InvalidOperationException();

            //  Prefix request with protocol frames
            //  Frame 1: "MDPCxy" (six bytes, MDP/Client x.y)
            //  Frame 2: Service name (printable string)
            request.Prepend(new ZFrame(service));
            request.Prepend(new ZFrame(MdpCommon.MDPC_CLIENT));
            if (_verbose)
                request.LogEachFrame("I: send request to '{0}' service:", service);

            int retriesLeft = _retries;
            while (retriesLeft > 0 && !cancellor.IsCancellationRequested)
            {
                if (cancellor.IsCancellationRequested
                    || (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                    _context.Shutdown();

                // Copy the Request and send on Client
                ZMessage msgreq = request.Duplicate();

                ZError error;
                if (!_client.Send(msgreq, out error))
                {
                    if (Equals(error, ZError.ETERM))
                    {
                        cancellor.Cancel();
                        break; // Interrupted
                    }
                }

                var p = ZPollItem.CreateReceiver();
                ZMessage msg;
                //  On any blocking call, libzmq will return -1 if there was
                //  an error; we could in theory check for different error codes,
                //  but in practice it's OK to assume it was EINTR (Ctrl-C):

                // Poll the client Message
                if (_client.PollIn(p, out msg, out error, _timeout))
                {
                    //  If we got a reply, process it
                    if (_verbose)
                        msg.LogEachFrame("I: received reply");

                    if (msg.Count < 3)
                        throw new InvalidOperationException();

                    using (ZFrame header = msg.Pop())
                        if (!header.ToString().Equals(MdpCommon.MDPC_CLIENT))
                            throw new InvalidOperationException();

                    using (ZFrame replyService = msg.Pop())
                        if (!replyService.ToString().Equals(service))
                            throw new InvalidOperationException();

                    request.Dispose();
                    return msg;
                }
                else if (Equals(error, ZError.ETERM))
                {
                    cancellor.Cancel();
                    break; // Interrupted
                }
                else if (Equals(error, ZError.EAGAIN))
                {
                    if (--retriesLeft > 0)
                    {
                        if (_verbose)
                            "W: no reply, reconnecting…".Log();

                        ConnectToBroker();
                    }
                    else
                    {
                        if (_verbose)
                            "W: permanent error, abandoning".Log();
                        break; // Give up
                    }
                }
            }
            if (cancellor.IsCancellationRequested)
            {
                "W: interrupt received, killing client…\n".Log();
            }
            request.Dispose();
            return null;
        }
    }
}
