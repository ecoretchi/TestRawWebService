namespace TestHttpService.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    internal class WebsockService
    {
        readonly int port = 80;
        readonly string ip = "127.0.0.1";

        readonly CancellationTokenSource cancellationSource = new();

        readonly TcpListener listener;

        private string ServiceName { get; set; } = string.Empty;

        private List<WebsockHandle> clientHandles = new();

        private Task? runTask;

        public Func<HttpListenerRequest, HttpListenerResponse, bool>? OnRequest { get; set; }


        public WebsockService(int port = 80, string name = "TestWebHookService")
        {
            this.port = port;
            ServiceName = name;
            listener = new TcpListener(IPAddress.Parse(ip), port);
        }

        public bool StartAsync()
        {
            if (runTask?.IsCompleted == false)
            {
                return false;
            }
            runTask = Task.Run(Start);
            return true;
        }

        public void Stop()
        {
            foreach(var handle in clientHandles)
            {
                handle.Stop();
            }
            cancellationSource.Cancel();
            runTask?.Wait();
        }

        void Start()
        {
            listener.Start();

            Console.WriteLine($"WebsockService '{ServiceName}' started on port {port}...");

            try
            {
                while (true)
                {
                    var task = listener.AcceptTcpClientAsync();

                    try
                    {
                        task.Wait(cancellationSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Listening Operation Canceled");
                        break;
                    }

                    if (task.IsCompleted)
                    {
                        var clinetId = $"Client#{clientHandles.Count}";
                        var handle = new WebsockHandle(clinetId, task.Result);

                        clientHandles.Add(handle);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
