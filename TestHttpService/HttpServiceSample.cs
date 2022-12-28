namespace TestHttpService
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    internal class HttpServiceSample
    {
        readonly int port = 80;

        readonly CancellationTokenSource cancellationSource = new();

        readonly HttpListener listener = new();

        public Func<HttpListenerRequest, HttpListenerResponse, bool>? OnRequest { get; set; }

        public HttpServiceSample()
        {
            listener.Prefixes.Add($"http://+:{port}/TestService/");
        }

        Task? runTask;

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
            cancellationSource.Cancel();
            runTask?.Wait();

        }

        void Start()
        {
            listener.Start();

            Console.WriteLine($"Listening on port {port}...");

            try
            {
                while (true)
                {
                    var task = listener.GetContextAsync();

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
                        HttpListenerContext ctx = task.Result;


                        using (HttpListenerResponse response = ctx.Response)
                        {
                            if ((OnRequest?.Invoke(ctx.Request, response) ?? false) == false)
                            {
                                response.StatusCode = (int)HttpStatusCode.NotFound;
                                response.StatusDescription = "Not found";
                            }
                            else
                            {
                                response.StatusCode = (int)HttpStatusCode.OK;
                                response.StatusDescription = "Status OK";
                            }
                        }
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
