using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameHelpers.Helpers;
using Newtonsoft.Json;
using SocketHelpers;

namespace TestSocketBall
{
    public partial class FrmMain : Form
    {
        //Name owner
        public String ownerName = "Xavi";

        //Mi ip
        public String ipLocal = "";

        //Vecinos
        public String ipLeft =  "192.168.3.36";
        public String ipRight = "192.168.3.30";

        //Guarda el JSon de la pelota
        public String datosPelota = "";

        //ClSckets
        private ClSockets loSocket;

        //ClBall
        private ClBall loBall;

        //ClPala
        private ClPaddle loPaddle;

        //Hay paredes?
        public Boolean wallsExist = false;

        //Random
        public Random random = new Random();

        

        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            loPaddle = new ClPaddle(this,Color.DarkBlue,100,15);

            ipLocal = GetLocalIPAddress().ToString();

            //Abrir clSocket y conectar listener
            loSocket = new ClSockets();
            loSocket.connectSocketListener(ipLocal);

            //Conectamos con los clientes
            loSocket.connectSocketLeft(ipLeft);
            loSocket.connectSocketRight(ipRight);            

            //Configuramos las funciones que escucharan los listeners
            loSocket.msgReceived += LoSocket_msgReceived;
        }

        public static IPAddress GetLocalIPAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var ipAddress = nic.GetIPProperties().GatewayAddresses.FirstOrDefault();

                if (ipAddress != null && !ipAddress.Address.ToString().Equals("0.0.0.0") && !ipAddress.Address.ToString().Equals("127.0.0.1"))
                {
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork && ip.IsDnsEligible)
                            {
                                return ip.Address;
                            }
                        }
                    }
                }
            }
            throw new Exception("No active network adapters found."); // Replace with messagebox
        }

        private void LoSocket_msgReceived(object sender, EventArgs e)
        {
            //Obtenems la pelota del vecino
            ClSockets temp = (ClSockets) sender;
            datosPelota = temp.data;

            Ball pelota = JsonConvert.DeserializeObject<Ball>(datosPelota);
            BeginInvoke((Action)delegate
            {
                pelota.positionY = 0 + (pelota.positionY - 0) * (Screen.PrimaryScreen.Bounds.Height - 0) / (pelota.resolutionY - 0);

                ClBall pelotaui = new ClBall(Color.FromArgb(pelota.color), pelota.creator, pelota.movementX,
                pelota.movementY, pelota.diameter, this, 30, loPaddle, pelota.positionX, pelota.positionY,
                pelota.resolutionX, pelota.resolutionY, pelota.life);
                pelotaui.wallhit += LoBall_wallhit;
            });
            
        }

        private void FrmMain_KeyUp(object sender, KeyEventArgs e)
        {
            //Al clicar en la tecla "Ctrl+n" generamos una pelota

            if (e.Control && e.KeyCode == Keys.N)
            {
                //Poner la pelota de la Marta
                loBall = new ClBall(Color.FromArgb(255, random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)), ownerName, 10, 10, 30, this, 30, loPaddle, 70, 70, 50, 50, 1);
                loBall.wallhit += LoBall_wallhit;
            }
        }

        private void LoBall_wallhit(object sender, EventArgs e)
        {
            ClBall loBall = (ClBall) sender;
            Ball ball;

            //Miramos si hay paredes en la partida
            if (!wallsExist)
            {
                ball = new Ball
                {
                    color = loBall.Color.ToArgb(),
                    diameter = loBall.Diametre,
                    creator = loBall.Owner,
                    life = loBall.Life,
                    movementX = loBall.MovX,
                    movementY = loBall.MovY,
                    positionX = loBall.PosX,
                    positionY = loBall.PosY,
                    resolutionY = loBall.ResY,
                    resolutionX = loBall.ResX
                };

                //Segun la posicion X de la pelota sabemos si es left o right
                if (loBall.PosX < this.Width / 2)
                {
                    ball.positionX = this.Width - (ball.diameter / 2);
                    datosPelota = JsonConvert.SerializeObject(ball);
                    loSocket.sendDataLeft(datosPelota);
                }
                else
                {
                    ball.positionX = 4;
                    datosPelota = JsonConvert.SerializeObject(ball);
                    loSocket.sendDataRight(datosPelota);
                }

            }


        }

        private void FrmMain_MouseMove(object sender, MouseEventArgs e)
        {
            loPaddle.PosPala(MousePosition);
        }
    }
}
