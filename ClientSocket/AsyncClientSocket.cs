using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSockets
{
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

// State object for receiving data from remote device.
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
        public int length {get; set; }
        public DatareceivedArgs(byte[] buffer, int bytecount)
        {
            this.buffer = buffer;
            this.length = bytecount;
        }
    }
    public class ConnectArgs : EventArgs
    {
        public bool connected { get; set; }
        public ConnectArgs(bool connected)
        {
            this.connected = connected;
        }
    }

    public class AsyncClientSocket
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        public event EventHandler<DatareceivedArgs> dataReceived;
        public event EventHandler<ConnectArgs> ConnectComplete;
        // The response from the remote device.
        private String response = String.Empty;
        private Socket client = null;
        private String _hostaddress;
        private IPHostEntry _ipHostInfo;
        private IPAddress _ipAddress;
        private IPEndPoint _remoteEP;
        private int _port;
        protected void OnConnectComplete(ConnectArgs e)
        {
            EventHandler<ConnectArgs> handler = ConnectComplete;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected void OnDataReceived(DatareceivedArgs e)
        {
            EventHandler<DatareceivedArgs> handler = dataReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public AsyncClientSocket()
        {
        }
        public void StartClient(String hostaddress, int port)
        {
            // Connect to a remote device.
            try
            {
                _hostaddress = hostaddress;
                _port = port;
                if (!IPAddress.TryParse(hostaddress, out _ipAddress))
                {
                    _ipHostInfo = Dns.GetHostEntry(_hostaddress);
                    _ipAddress = _ipHostInfo.AddressList[0];
                }
                _remoteEP = new IPEndPoint(_ipAddress, port);

                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                client.BeginConnect(_remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                //Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                // Signal that the connection has been made.
                ConnectComplete(this, new ConnectArgs (client.Connected));
                connectDone.Set();
            }
            catch (Exception e)
            {
                log.Debug("Exception: " + e.Message);
                if (ConnectComplete != null)
                {
                    ConnectComplete(this, new ConnectArgs(false));
                }
            }
        }
        public void Receive()
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public void Disconnect()
        {
            try
            {
                client.Disconnect(false);
                client.Shutdown(SocketShutdown.Both);
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
                int bytesRead = client.EndReceive(ar);
                state.totalbytesread = bytesRead;
                if (bytesRead > 0)
                {
                    // Signal that all bytes have been received.
                    dataReceived(this, new DatareceivedArgs(state.buffer, state.totalbytesread ));
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public void Send(String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            Send(byteData);
        }
        public void Send(byte[] byteData)
        {
            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }
        public void Send(byte[] byteData, int length)
        {
            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, length, 0,
                new AsyncCallback(SendCallback), client);
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                //Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
