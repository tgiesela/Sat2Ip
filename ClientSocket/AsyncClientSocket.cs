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

        public event EventHandler<DatareceivedArgs> ReceiveComplete;
        public event EventHandler<ConnectArgs> ConnectComplete;
        // The response from the remote device.
        private String response = String.Empty;
        private Socket m_client = null;
        private String m_hostaddress;
        private IPHostEntry m_ipHostInfo;
        private IPAddress m_ipAddress;
        private IPEndPoint m_remoteEP;
        private int m_port;
        public AsyncClientSocket(String hostaddress, int port)
        {
            m_hostaddress = hostaddress;
            m_port = port;
            if (!IPAddress.TryParse(m_hostaddress, out m_ipAddress))
            {
                m_ipHostInfo = Dns.GetHostEntry(m_hostaddress);
                m_ipAddress = m_ipHostInfo.AddressList[0];
            }
        }
        public void StartNowait()
        {
            // Connect to a remote device.
            try
            {
                m_client = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
                m_remoteEP = new IPEndPoint(m_ipAddress, m_port);
                m_client.BeginConnect(m_remoteEP,
                    new AsyncCallback(ConnectCallback), m_client);
                connectDone.WaitOne();
            }
            catch (Exception e)
            {
                m_client.Shutdown(SocketShutdown.Both);
                log.Debug("Socket connectNW failed: " + e.Message);
            }

        }

        public void Start()
        {
            // Connect to a remote device.
            try
            {
                m_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_remoteEP = new IPEndPoint(m_ipAddress, m_port);
                m_client.Connect(m_remoteEP);
            }
            catch (Exception e)
            {
                //m_client.Shutdown(SocketShutdown.Both);
                log.Debug("Socket connect failed: " + e.Message);
            }
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                // Signal to the creator that the connection has been made.
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
        private void checkconnected()
        {
            if (!m_client.Connected)
            {
                log.Debug("Receive requested, but socket not connected");
                throw new Exception("Socket not connected");

            }
        }
        public void Receive()
        {
            checkconnected();
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = m_client;
                state.totalbytesread = 0;

                // Begin receiving the data from the remote device.
                m_client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                log.Debug("Receive failed: " + e.Message);
            }
        }
        public void Disconnect()
        {
            try
            {
                m_client.Disconnect(true);
                m_client.Close();
            }
            catch (Exception e)
            {
                log.Debug("Disconnected failed: " + e.Message);
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);
                state.totalbytesread = bytesRead;
                if (bytesRead > 0)
                {
                    log.Debug("Received data: " + bytesRead);
                    Utils.Utils.DumpBytes(state.buffer, bytesRead);
                    // Signal to the creator that all bytes have been received.
                    ReceiveComplete(this, new DatareceivedArgs(state.buffer, state.totalbytesread ));
                    receiveDone.Set();
                }
                else
                {
                    log.Debug("Socket is being closed");
                    client.Close();
                }
            }
            catch (Exception e)
            {
                log.Debug("EndReceive failed: " + e.Message);
            }
        }
        public void Send(String data)
        {
            checkconnected();
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            Send(byteData);
        }
        public void Send(byte[] byteData)
        {
            checkconnected();
            // Begin sending the data to the remote device.
            try
            {
                m_client.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), m_client);
            }
            catch (Exception e)
            {
                log.Debug("BeginSend failed: " + e.Message);
            }
        }
        public void Send(byte[] byteData, int length)
        {
            checkconnected();
            // Begin sending the data to the remote device.
            try
            {
                m_client.BeginSend(byteData, 0, length, 0,
                    new AsyncCallback(SendCallback), m_client);
            }
            catch (Exception e)
            {
                log.Debug("BeginSend failed: " + e.Message);
            }
        }
        public void SendWait(byte[] stopdata, int v)
        {
            checkconnected();
            try
            {
                SocketFlags socketFlags = default;
                m_client.Send(stopdata, 0, v, socketFlags);
            }
            catch (Exception e)
            {
                log.Debug("Send failed: " + e.Message);
            }
        }


        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                log.Debug("EndSend failed: " + e.Message);
            }
        }
    }
}
