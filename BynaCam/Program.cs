﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tibia;
using Tibia.Objects;
using Tibia.Util;
using System.Diagnostics;
using Tibia.Packets;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace BynaCam
{
    class MainClass
    {
        static Client client;
        static int speed = 1;
        static StreamReader stream;

        private static Stream getCamFileStream()
        {
            //Open File Dialog
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            dialog.Filter = "BynaCam Files|*.byn";
            dialog.Title = "Open BynaCam file.";
            if (dialog.ShowDialog() == DialogResult.Cancel)
            {
                MessageBox.Show("Cannot open BynaCam file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
            return dialog.OpenFile();
        }

        private static void updateClientTitle()
        {
            try
            {
                client.Title = "BynaCam -> speed: x" + speed;
            }
            catch { }
        }

        private static void setUpKeyboardHook(Client client)
        {
            KeyboardHook.Enable();
            KeyboardHook.KeyDown = null;
            KeyboardHook.KeyDown += new KeyboardHook.KeyboardHookHandler(delegate(Keys key)
            {
                if (client.IsActive)
                {
                    if (key == Keys.Right)
                    {
                        if (speed == 50)
                            return false;
                        speed++;
                        updateClientTitle();
                    }
                    if (key == Keys.Left)
                    {
                        if (speed == 1)
                            return false;
                        speed--;
                        updateClientTitle();
                    }
                    if (key == Keys.Up)
                    {
                        speed = 50;
                        updateClientTitle();
                    }
                    if (key == Keys.Down)
                    {
                        speed = 1;
                        updateClientTitle();
                    }

                    if (key == Keys.Left || key == Keys.Right
                        || key == Keys.Down || key == Keys.Up)
                        return false;
                }
                return true;
            });
            KeyboardHook.KeyDown += null;
        }

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            
            stream = new StreamReader(getCamFileStream());

            ClientChooserOptions options = new ClientChooserOptions();
            options.ShowOTOption = false;
            client = ClientChooser.ShowBox(options);

            if (client != null)
            {
                TibiaNetwork Network = new TibiaNetwork(client);
                updateClientTitle();
                client.Exited += new EventHandler(client_Exited);
                client.AutoLogin("111", "111", "Byna", "BynaCam");

                Thread.Sleep(2000);
                if (!Network.uxGameServer.Accepted)
                {
                    client.Process.Kill();
                    Process.GetCurrentProcess().Kill();
                }
                else
                {
                    TimeSpan time;
                    byte[] packet;

                    new Thread(new ThreadStart(delegate()
                    {
                        while (!stream.EndOfStream)
                        {
                            time = TimeSpan.Parse(stream.ReadLine());
                            packet = stream.ReadLine().ToBytesAsHex();

                            if (packet == null)
                                continue;

                            if (packet[0] == 0x14) //disconnectclient 0x14
                                continue;

                            if (packet[0] == 0x65 || packet[0] == 0x66 || packet[0] == 0x67 || packet[0] == 0x68
                                 || packet[0] == (byte)IncomingPacketType.MapDescription
                                 || packet[0] == (byte)IncomingPacketType.SelfAppear
                                 || packet[0] == (byte)IncomingPacketType.WorldLight)
                            {
                                Network.uxGameServer.Send(packet);
                                continue;
                            }
                            else
                            {
                                Thread.Sleep(time.Milliseconds / speed);
                            }

                            if (packet[0] == 0xc8)//setoufit block
                                return;
                            if (packet[0] == (byte)IncomingPacketType.ChannelList)
                                return; //channellist block

                            Network.uxGameServer.Send(packet);
                        }
                        Thread.Sleep(3000);
                        Process.GetCurrentProcess().Kill();
                    })).Start();

                    while (true)
                    {
                        setUpKeyboardHook(client);
                        updateClientTitle();
                    }
                }
            }
            else
            {
                Process.GetCurrentProcess().Kill();
            }
        }

        private static void client_Exited(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
