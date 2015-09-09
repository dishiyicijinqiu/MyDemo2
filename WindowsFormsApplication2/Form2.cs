using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication2
{
    public partial class Form2 : Form
    {
        // Creates one SocketPermission object for access restrictions
        SocketPermission permission = new SocketPermission(
            NetworkAccess.Accept,     // Allowed to accept connections
            TransportType.Tcp,        // Defines transport types
            "",                       // The IP addresses of local host
            SocketPermission.AllPorts // Specifies all ports
            );
        // Listening Socket object
        Socket sListener = null;
        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Ensures the code to have permission to access a Socket
                permission.Demand();

                // Resolves a host name to an IPHostEntry instance
                IPHostEntry ipHost = Dns.GetHostEntry("");

                // Gets first IP address associated with a localhost
                IPAddress ipAddr = ipHost.AddressList[0];

                // Creates a network endpoint
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 4510);

                // Create one Socket object to listen the incoming connection
                sListener = new Socket(
                    ipAddr.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp
                    );

                // Associates a Socket with a local endpoint
                sListener.Bind(ipEndPoint);

                // Places a Socket in a listening state and specifies the maximum
                // Length of the pending connections queue
                sListener.Listen(10);
                this.listBox1.Items.Add(string.Format("Waiting for a connection on port {0}",
                    ipEndPoint));

                // Begins an asynchronous operation to accept an attempt
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                sListener.BeginAccept(aCallback, sListener);
            }
            catch (Exception ex)
            {
                this.listBox1.Items.Add(string.Format("Exception: {0}", ex.ToString()));
            }
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sListener != null && sListener.Connected)
            {
                sListener.Shutdown(SocketShutdown.Receive);
                sListener.Close();
            }
        }
        /// <summary>
        /// Asynchronously accepts an incoming connection attempt and creates
        /// a new Socket to handle remote host communication.
        /// </summary>     
        /// <param name="ar">the status of an asynchronous operation
        /// </param> 
        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = null;

            // A new Socket to handle remote host communication
            Socket handler = null;
            try
            {
                // Receiving byte array
                byte[] buffer = new byte[1024];
                // Get Listening Socket object
                listener = (Socket)ar.AsyncState;
                // Create a new socket
                handler = listener.EndAccept(ar);

                // Using the Nagle algorithm
                handler.NoDelay = false;

                // Creates one object array for passing data
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;

                // Begins to asynchronously receive data
                handler.BeginReceive(
                    buffer,        // An array of type Byt for received data
                    0,             // The zero-based position in the buffer 
                    buffer.Length, // The number of bytes to receive
                    SocketFlags.None,// Specifies send and receive behaviors
                    new AsyncCallback(ReceiveCallback),//An AsyncCallback delegate
                    obj            // Specifies infomation for receive operation
                    );

                // Begins an asynchronous operation to accept an attempt
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                listener.BeginAccept(aCallback, listener);
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    this.listBox1.Items.Add(string.Format("Exception: {0}", ex.ToString()));
                }));
            }
        }

        /// <summary>
        /// Asynchronously receive data from a connected Socket.
        /// </summary>
        /// <param name="ar">
        /// the status of an asynchronous operation
        /// </param> 
        public void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Fetch a user-defined object that contains information
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                // Received byte array
                byte[] buffer = (byte[])obj[0];

                // A Socket to handle remote host communication.
                Socket handler = (Socket)obj[1];

                // Received message
                string content = string.Empty;

                // The number of bytes received.
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(buffer, 0,
                        bytesRead);

                    // Convert byte array to string
                    string strmsg = string.Format("Read {0} bytes from client.\n Data: {1}",
                        content.Length * 2, content);
                    //this.Invoke(new Action(() =>
                    //{
                    //    this.listBox1.Items.Add(strmsg);
                    //}));
                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.listBox1.Items.Add(strmsg);
                    }));

                    // Prepare the reply message
                    byte[] byteData =
                        Encoding.Unicode.GetBytes(content);

                    // Sends data asynchronously to a connected Socket
                    handler.BeginSend(byteData, 0, byteData.Length, 0,
                        new AsyncCallback(SendCallback), handler);
                    // Continues to asynchronously receive data
                    byte[] buffernew = new byte[1024];
                    obj[0] = buffernew;
                    obj[1] = handler;
                    handler.BeginReceive(buffernew, 0, buffernew.Length,
                        SocketFlags.None,
                        new AsyncCallback(ReceiveCallback), obj);
                }
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    this.listBox1.Items.Add(string.Format("Exception: {0}", ex.ToString()));
                }));
            }
        }

        /// <summary>
        /// Sends data asynchronously to a connected Socket.
        /// </summary>
        /// <param name="ar">
        /// The status of an asynchronous operation
        /// </param> 
        public void SendCallback(IAsyncResult ar)
        {
            try
            {
                // A Socket which has sent the data to remote host
                Socket handler = (Socket)ar.AsyncState;

                // The number of bytes sent to the Socket
                int bytesSend = handler.EndSend(ar);
                this.Invoke(new Action(() =>
                {
                    this.listBox1.Items.Add(string.Format("Sent {0} bytes to Client", bytesSend));
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    this.listBox1.Items.Add(string.Format("Exception: {0}", ex.ToString()));
                }));
            }
        }
    }
}
