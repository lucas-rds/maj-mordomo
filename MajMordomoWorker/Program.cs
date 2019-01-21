using System;
using System.Threading;
using ZeroMQ;

namespace MajMordomoWorker
{
    class Program
    {
        static void Main(string[] args)
        {
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            Console.CancelKeyPress += (s, ea) =>
            {
                ea.Cancel = true;
                cancellationToken.Cancel();
            };

            using (MajordomoWorker session = new MajordomoWorker("tcp://127.0.0.1:5555", "echo", verbose: true))
            {
                ZMessage reply = null;
                while (true)
                {
                    ZMessage request = session.Recv(reply, cancellationToken);
                    if (request == null)
                        break; // worker was interrupted
                    reply = request; // Echo is complex
                }
            }
        }
    }
}
