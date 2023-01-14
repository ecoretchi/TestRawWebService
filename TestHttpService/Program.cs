using System;
using System.Net;
using TestHttpService;

var httpService = new HttpServiceSample();

var t = httpService.StartAsync();

int counter = 0;
int refreshInterval = 1;
int ETag = 1;

var onCheckCounter = (Action<bool> doRefresh) =>
{
    counter++;
    // every time after 4-times attempts (if refresh 1 seconds, it mean every 4 seconds return OK result)
    var isRefresh = counter % 7 == 0;
    doRefresh(isRefresh);
    return isRefresh;
};

httpService.OnRequest += (HttpListenerRequest request, HttpListenerResponse response) =>
{
    Console.WriteLine($"Process request #{counter} ...");
    Console.WriteLine("---------------------- Request.Header ---------------------- ");
    Console.WriteLine(request.Headers);
    Console.WriteLine("------------------------------------------------------------- ");

    var validate = request.QueryString.Get("validate");

    if (string.IsNullOrEmpty(validate) == false)
    {
        return onCheckCounter(isRefreshed =>
        {
            if (isRefreshed)
            {
                Console.WriteLine(".... Simulate Image Data was changed!!!");
            }
            else
            {
                Console.WriteLine(".... Simulate Image Data not changed yet....");
            }
        });
    }
    else
    {
        var filename = request.Url?.Segments.Last();

        if (filename == "test")
        {            
            response.AddHeader("Content-Type", "image/jpg");
            response.AddHeader("Refresh", $"{refreshInterval};url={filename}");

            onCheckCounter(isRefreshed =>
            {
                if (isRefreshed)
                {
                    ++ETag;
                    Console.WriteLine(".... Simulate Image was changed!!!");

                    filename = "TheStars2.jpg";
                }
                else
                {
                    filename = "TheStars.jpg";
                }
            });

            response.AddHeader("ETag", ETag.ToString());
            response.AddHeader("Cache-Control", "max-age=3600, public");

        }
        else if (!File.Exists(filename))
        {
            filename = "index.html";

            response.AddHeader("Content-Type", "text/html; charset=utf-8");
            response.AddHeader("Cache-Control", "max-age=1500, public");
        }
        else
        {
            response.AddHeader("Content-Type", "image/jpg");
            response.AddHeader("Date", DateTime.Now.ToString("r"));
            response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));
            response.AddHeader("Cache-Control", "no-cache");
        }

        Console.WriteLine("---------------------- Response.Header ---------------------- ");
        Console.WriteLine(response.Headers);
        Console.WriteLine("------------------------------------------------------------- ");

        WriteBody(response, filename);
    }

    return true;
};



Console.ReadLine();

httpService.Stop();

void WriteBody(HttpListenerResponse response, string filename)
{
    using FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
    var buffer = new byte[file.Length];
    file.Read(buffer, 0, buffer.Length);

    using Stream output = response.OutputStream;
    output.Write(buffer, 0, buffer.Length);
}