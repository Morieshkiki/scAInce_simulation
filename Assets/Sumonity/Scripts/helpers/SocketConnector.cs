using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace tumvt.sumounity
{
    public class SocketConnector
    {

        public string connectionIP = "127.0.0.1";
        public int connectionPort = 25001;

        public string messageReceived { get; set; }
        public string messageToSend { get; set; }


        Thread _socketThread;
        IPAddress localAdd;
        TcpListener listener;
        TcpClient client;
        NetworkStream networkStream;
        bool isRunning;

        public SocketConnector()
        {
            connectionIP = "127.0.0.1";
            connectionPort = 25001;
        }

        public SocketConnector(string ipAdress, int port)
        {
            connectionIP = ipAdress;
            connectionPort = port;
        }


        public void Start()
        {
            // Initialize Thread

            ThreadStart threadStart = new ThreadStart(GetInfo);
            _socketThread = new Thread(threadStart);
            _socketThread.Start();

        }

        public void Close()
        {
            _socketThread.Abort();
        }

        void GetInfo()
        {
            Thread.Sleep(1000);

            // The SUMO host (the LLM app, script.py) serves on this port. Unity's
            // client used to connect exactly once with no retry, so if Play was
            // pressed before the LLM app was listening the thread died silently and
            // no cars ever appeared. Retry for ~30 s so connection ordering is
            // forgiving (start the LLM app, then press Play — either order works).
            int attempts = 0;
            while (true)
            {
                try
                {
                    client = new TcpClient();
                    client.Connect(connectionIP, connectionPort);
                    break;
                }
                catch (Exception ex)
                {
                    attempts++;
                    if (attempts >= 30)
                    {
                        Debug.LogWarning("Could not connect to SUMO bridge on " + connectionIP + ":" + connectionPort +
                                         " after " + attempts + " attempts: " + ex.Message);
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }

            isRunning = true;
            while (isRunning)
            {
                SendAndReceiveData();
            }
            listener.Stop();
        }

        void SendAndReceiveData()
        {
            try
            {
                networkStream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];

                // ======================
                //      Receive Data
                // ====================== 
                        
                try
                {
                    int bytesRead = networkStream.Read(buffer, 0, client.ReceiveBufferSize); //Getting data in Bytes from Python
                    string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead); //Converting byte data to string
                    
                    if (dataReceived != null)
                    {
                        ProcessReceivedData();
                        messageReceived = dataReceived;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("An error occurred while reading data from the network stream: " + ex.Message);
                }

            }
            catch (Exception ex)
            {
                Debug.LogWarning("An error occurred while receiving data: " + ex.Message);
            }

            // ======================
            //      Send Data
            // ======================
            messageToSend = string.IsNullOrEmpty(messageToSend) ? "Empty Message" : messageToSend;
            byte[] myWriteBuffer = Encoding.ASCII.GetBytes(messageToSend); //Converting string to byte data
            networkStream.Write(myWriteBuffer, 0, myWriteBuffer.Length); //Sending the data in Bytes to Python
        }


        void ProcessReceivedData()
        {

        }
    }
}
