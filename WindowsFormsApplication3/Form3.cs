using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication3
{
    public partial class Form3 : Form
    {
        Socket socket;
        byte[] sendBuffer = new byte[1024];
        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        private void button1_Click(object sender, EventArgs e)
        {

            try
            {
                var msg = this.textBox1.Text;
                // 发送数据
                EndPoint sendPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8405);
                var sendLength = socket.SendTo(sendBuffer, sendPoint);
            }
            catch (SocketException exception)
            {
            }
            catch (Exception exception)
            {
            }
        }
    }
}
