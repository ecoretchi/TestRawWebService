﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestHttpService.Services
{
    internal class WebsockHandle
    {
        readonly TcpClient client;

        readonly NetworkStream stream;

        readonly Task handleTask;

        readonly CancellationTokenSource cancellationSource = new();

        public string ClinetId { get; private set; }

        public WebsockHandle(string clinetId, TcpClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));

            ClinetId = clinetId;

            Console.WriteLine($"The client {clinetId} connected.");

            stream = client.GetStream();

            handleTask = Task.Run(HandleConnection);
        }

        void HandleConnection()
        {
            try
            {
                while (true)
                {
                    while (!stream.DataAvailable)
                        Task.Delay(100, cancellationSource.Token).Wait();

                    while (client.Available < 3) ; // match against "get"

                    byte[] bytes = new byte[client.Available];
                    stream.Read(bytes, 0, client.Available);
                    string s = Encoding.UTF8.GetString(bytes);

                    if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                    {
                        Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                        string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                        string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                        byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                        string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                        // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                        byte[] response = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 101 Switching Protocols\r\n" +
                            "Connection: Upgrade\r\n" +
                            "Upgrade: websocket\r\n" +
                            "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                        stream.Write(response, 0, response.Length);
                    }
                    else
                    {
                        bool fin = (bytes[0] & 0b10000000) != 0,
                            mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
                        int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                            offset = 2;
                        ulong msglen = (ulong)bytes[1] & 0b01111111;

                        if (msglen == 126)
                        {
                            // bytes are reversed because websocket will print them in Big-Endian, whereas
                            // BitConverter will want them arranged in little-endian on windows
                            msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                            offset = 4;
                        }
                        else if (msglen == 127)
                        {
                            // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                            // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                            // websocket frame available through client.Available).
                            msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                            offset = 10;
                        }

                        if (msglen == 0)
                        {
                            Console.WriteLine("msglen == 0");
                        }
                        else if (mask)
                        {
                            byte[] decoded = new byte[msglen];
                            byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                            offset += 4;

                            for (ulong i = 0; i < msglen; ++i)
                                decoded[i] = (byte)(bytes[offset + (int)i] ^ masks[i % 4]);

                            string text = Encoding.UTF8.GetString(decoded);
                            Console.WriteLine("{0}", text);
                        }
                        else
                            Console.WriteLine("mask bit not set");

                        Console.WriteLine();
                    }
                }
            }
            catch(OperationCanceledException)
            {
                Console.WriteLine("Handle Client, Operation Canceled");
            }
            catch(Exception) 
            { 
                Console.WriteLine(""); 
            }
        }    

        public void Stop()
        {
            cancellationSource.Cancel();
        }
    }
}