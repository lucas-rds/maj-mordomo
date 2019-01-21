using System;
using System.Collections.Generic;
using System.Threading;
using ZeroMQ;

namespace MajMordomoClient
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

            var Verbose = true;
            using (MajordomoClient client = new MajordomoClient("tcp://127.0.0.1:5555", Verbose))
            {
                //int count;
                //for (count = 0; count < 100000; count++)
                //{
                ZMessage request = new ZMessage(new List<ZFrame> { new ZFrame("Hello world") });
                using (ZMessage reply = client.Send("echo", request, cancellationToken))
                {
                    Console.WriteLine(reply);
                }
                //}
                //Console.WriteLine("{0} requests/replies processed\n", count);
            }
        }
    }
}
