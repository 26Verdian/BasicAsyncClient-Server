using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace MovingObject/
{
    public partial class Form1 : Form
    {
        Pen red = new Pen(Color.Red);
        Rectangle rect = new Rectangle(20, 20, 30, 30);
        SolidBrush fillBlue = new SolidBrush(Color.Blue);
        int slide = 10;

        private Socket listenerSocket;
        private List<Socket> clientSockets = new List<Socket>(); // Koleksi untuk menyimpan semua soket klien

        public Form1()
        {
            InitializeComponent();
            timer1.Interval = 50;
            timer1.Enabled = true;

            // Setup server socket
            SetupServerSocket();
        }

        private void SetupServerSocket()
        {
            try
            {
                // Setup the server socket to listen for client connections
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);

                listenerSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenerSocket.Bind(localEndPoint);
                listenerSocket.Listen(10); // Max number of pending connections

                // Accept the client connection asynchronously
                listenerSocket.BeginAccept(new AsyncCallback(AcceptCallback), listenerSocket);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting up server: " + ex.Message);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Accept the incoming client connection
                Socket listener = (Socket)ar.AsyncState;
                Socket clientSocket = listener.EndAccept(ar);

                // Add the client socket to the list
                lock (clientSockets)
                {
                    clientSockets.Add(clientSocket);
                }

                MessageBox.Show("Client connected!");

                // Start listening for more clients
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error accepting client: " + ex.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            back();

            rect.X += slide;
            Invalidate(); // Redraw the object locally

            // Kirim data posisi objek ke semua klien
            lock (clientSockets)
            {
                foreach (Socket clientSocket in clientSockets)/
                {
                    if (clientSocket.Connected)
                    {
                        try
                        {
                            string dataToSend = rect.X + ":" + rect.Y;
                            byte[] message = Encoding.ASCII.GetBytes(dataToSend);
                            clientSocket.Send(message); // Send position to connected client
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error sending data to client: " + ex.Message);
                        }
                    }
                }
            }
        }

        private void back()
        {
            if (rect.X >= this.Width - rect.Width * 2)
                slide = -10;
            else if (rect.X <= rect.Width / 2)
                slide = 10;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawRectangle(red, rect);
            g.FillRectangle(fillBlue, rect);
        }
    }
}
