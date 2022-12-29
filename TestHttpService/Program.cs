using System;
using System.Diagnostics;
using System.Net;
using TestHttpService.Services;

Guid myuuid = Guid.NewGuid();
string myuuidAsString = myuuid.ToString();

Console.WriteLine("Your UUID is: " + myuuidAsString);

var webService = new WebsockService(8021);
webService.StartAsync();

var httpService = new HttpService();
httpService.StartAsync();


int counter = 0;

httpService.OnRequest += (HttpListenerRequest request, HttpListenerResponse response) =>
{
    Console.WriteLine($"Process request #{counter} ...");

    var validate = request.QueryString.Get("validate");

    if (string.IsNullOrEmpty(validate) == false)
    {
        ++counter;

        if (counter % 4 == 0) // every time after 4-times attempts (if refresh 1 seconds, it mean every 4 seconds return OK result)
        {
            Console.WriteLine(".... Simulate Image Data was changed!!!");
            return true;
        }
        else
        {
            Console.WriteLine(".... Simulate Image Data not changed yet....");
            return false;
        }
    }
    else
    {
        var filename = request.Url?.Segments.Last();

        if (!File.Exists(filename))
        {
            filename = "index.html";

            response.AddHeader("Cache-Control", "max-age=1500, must-revalidate");
        }
        else
        {
            Console.WriteLine(".... Simulate Reloading Image Data ....");
            Console.WriteLine($"Resource: {filename}");
            Console.WriteLine(request.Headers);

            response.AddHeader("Date", DateTime.Now.ToString("r"));
            response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));
            response.AddHeader("Cache-Control", "no-cache");
        }

        var ext = Path.GetExtension(filename);
        switch (ext)
        {
            case ".html":
                response.AddHeader("Content-Type", "text/html; charset=utf-8");
                break;
            case ".jpg":
                response.AddHeader("Content-Type", "image/jpg");
                break;
            case ".png":
                response.AddHeader("Content-Type", "image/png");
                break;

        }


        using FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
        var buffer = new byte[file.Length];
        file.Read(buffer, 0, buffer.Length);

        using Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
    }

    return true;
};


Console.ReadLine();

httpService.Stop();

