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
    public partial class Form3 : Form
    {
        // Receiving byte array 
        byte[] bytes = new byte[1024];
        Socket clientsender;
        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (clientsender == null || !clientsender.Connected)
                return;
            // Disables sends and receives on a Socket.
            clientsender.Shutdown(SocketShutdown.Both);
            //Closes the Socket connection and releases all resources
            clientsender.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Create one SocketPermission for socket access restrictions
            SocketPermission permission = new SocketPermission(
                NetworkAccess.Connect,    // Connection permission
                TransportType.Tcp,        // Defines transport types
                "",                       // Gets the IP addresses
                SocketPermission.AllPorts // All ports
                );

            // Ensures the code to have permission to access a Socket
            permission.Demand();
            // Resolves a host name to an IPHostEntry instance           
            IPHostEntry ipHost = Dns.GetHostEntry("");

            // Gets first IP address associated with a localhost
            IPAddress ipAddr = ipHost.AddressList[0];

            // Creates a network endpoint
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 4510);

            // Create one Socket object to setup Tcp connection
            clientsender = new Socket(
               ipAddr.AddressFamily,// Specifies the addressing scheme
               SocketType.Stream,   // The type of socket 
               ProtocolType.Tcp     // Specifies the protocols 
               );

            clientsender.NoDelay = false;   // Using the Nagle algorithm

            // Establishes a connection to a remote host
            clientsender.Connect(ipEndPoint);
            this.listBox1.Items.Add(string.Format("Socket connected to {0}",
                clientsender.RemoteEndPoint.ToString()));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Sending message
            //<Client Quit> is the sign for end of data
            string theMessage = this.textBox1.Text;
            byte[] msg = Encoding.Unicode.GetBytes(theMessage);

            // Sends data to a connected Socket.
            int bytesSend = clientsender.Send(msg);

            // Receives data from a bound Socket.
            int bytesRec = clientsender.Receive(bytes);

            // Converts byte array to string
            theMessage = Encoding.Unicode.GetString(bytes, 0, bytesRec);

            // Continues to read the data till data isn't available
            while (clientsender.Available > 0)
            {
                bytesRec = clientsender.Receive(bytes);
                theMessage += Encoding.Unicode.GetString(bytes, 0, bytesRec);
            }
            this.listBox1.Items.Add(string.Format("The server reply: {0}", theMessage));
        }
    }
}
