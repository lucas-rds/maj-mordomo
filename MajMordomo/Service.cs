using System;
using System.Collections.Generic;
using ZeroMQ;

namespace MajMordomo
{
    public class Service : IDisposable
    {
        // Broker Instance
        private readonly Broker _broker;
        // List of client requests
        private readonly List<ZMessage> _requests;
        // List of waiting workers
        public List<Worker> Workers { get; set; }
        // Service Name
        public readonly string Name;

        internal Service(Broker broker, string name)
        {
            _broker = broker;
            _requests = new List<ZMessage>();
            Workers = new List<Worker>();
            Name = name;
        }

        ~Service()
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
                foreach (var r in _requests)
                {
                    // probably obsolete?
                    using (r)
                    {
                    }
                }
            }
        }

        //  This method sends requests to waiting workers:
        public void Dispatch(ZMessage msg)
        {
            if (msg != null) // queue msg if any
                _requests.Add(msg);

            _broker.Purge();
            while (Workers.Count > 0 && _requests.Count > 0)
            {
                Worker worker = Workers[0];
                Workers.RemoveAt(0);
                _broker.WaitingWorkers.Remove(worker);
                ZMessage reqMsg = _requests[0];
                _requests.RemoveAt(0);
                using (reqMsg)
                    worker.Send(MdpCommon.MdpwCmd.REQUEST.ToHexString(), null, reqMsg);
            }
        }
    }
}