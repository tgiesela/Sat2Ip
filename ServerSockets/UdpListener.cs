using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerSockets
{
    public class UdpListener
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Boolean _listening = false;
        private Boolean _threadactive = false;
        private Thread _listeningThread;
        private int _port;
        private UdpClient listener;
        private IPEndPoint _groupEP;
        public event EventHandler<DatareceivedArgs> dataReceived;
        private ManualResetEvent receiveDone =
            new ManualResetEvent(false);
        public int Port
        {
            get
            {
                return _port;
            }

            set
            {
                _port = value;
            }
        }
        public bool Listening
        {
            get
            {
                return _listening;
            }

            set
            {
                _listening = value;
            }
        }
        public class StateObject
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 4096;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            public int totalbytesread;
        }
        public class DatareceivedArgs : EventArgs
        {
            public byte[] buffer { get; set; }
            public int length { get; set; }
            public DatareceivedArgs(byte[] buffer, int bytecount)
            {
                this.buffer = buffer;
                this.length = bytecount;
            }
        }
        public event EventHandler<MessageReceivedArgs> MessageReceived;

        public UdpListener(int port)
        {
            _listening = false;
            _threadactive = false;
            _port = port;
        }
        public void StartListener(int exceptedMessageLength)
        /**
         * Starts separate thread which raises events when data is received
         */
        {
            if (!_listening)
            {
                _listeningThread = new Thread(ListenForUDPPackages);
                _listening = true;
                _listeningThread.Start();
                _threadactive = true;
            }
        }
        public void StopListener()
        {
            _listening = false;
            listener.Close();
            if (_threadactive && _listeningThread.IsAlive)
            {
                //_listeningThread.Abort();
                _listeningThread = null;
                _threadactive = false;
            }
            Console.WriteLine("Done listening for UDP broadcast");
        }
        public void Connect()
        /**
         * Connects to specified port. To be used in waited mode.
         * Receive() must be used to receive Data.
         */
        {
            listener = null;
            try
            {
                listener = new UdpClient(_port);
                log.Debug("UdpListener connected to port " + _port.ToString());
            }
            catch (SocketException se)
            {
                throw se;
            }

            if (listener != null)
            {
                _groupEP = new IPEndPoint(IPAddress.Any, _port);
                _listening = true;
            }
        }
        public void setReceiveTimeout(int receivetimeout)
        {
            listener.Client.ReceiveTimeout = receivetimeout;
        }
        public byte[] Receive()
        {
            try
            {
                return listener.Receive(ref _groupEP);
            }
            catch (SocketException se)
            {
                this.StopListener();
                return null;
            }
        }
        public void ReceiveNowait()
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();

                // Begin receiving the data from the remote device.
                listener.BeginReceive(new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                //Socket client = state.workSocket;

                // Read data from the remote device.
                state.buffer = listener.EndReceive(ar, ref _groupEP );
                state.totalbytesread = state.buffer.Length ;
                dataReceived(this, new DatareceivedArgs(state.buffer, state.totalbytesread));
                receiveDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private async Task<byte[]> readAsync()
        {
            UdpReceiveResult taskresult = await listener.ReceiveAsync();
            return taskresult.Buffer;
        }
        private async void ListenForUDPPackages()
        {
            listener = null;
            try
            {
                listener = new UdpClient(_port);
            }
            catch (SocketException)
            {
                //do nothing
            }

            if (listener != null)
            {
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, _port);

                try
                {
                    Task<byte[]> bytes = readAsync();
                    while (_threadactive )
                    {
                        try
                        {
                            await bytes;
                            bytes = readAsync();
                            OnMessageReceived(new MessageReceivedArgs(bytes.Result));
                        }
                        catch (SocketException se)
                        {
                            _listening = false;
                            _threadactive = false;
                        }

                    }
                }
                catch (Exception e)
                {
                    log.Debug(e.ToString());
                }
                finally
                {
                    listener.Close();
                    log.Debug("Done listening for UDP broadcast");
                }
            }
        }
        protected virtual void OnMessageReceived(EventArgs e)
        {
            EventHandler<MessageReceivedArgs> handler = MessageReceived;
            if (handler != null)
            {
                handler(this, (MessageReceivedArgs)e);
            }
        }

    }
    public class MessageReceivedArgs : EventArgs
    {
        public byte[] data { get; set; }

        public MessageReceivedArgs(byte[] newData)
        {
            data = newData;
        }
    }

}
