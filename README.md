## Sample demonstrates how we could tell the client about some source that could be reloaded.

ToDo and Start

1. Modify index.html: change urlSrv to your ip
2. Open cmd and run command 'netsh', see it bellow
3. Build and run service
4. Open the browser and run url: http://192.168.1.3/TestService/
5. Ensure that the 192.168.1.3 is changed to your ip 

----
Execute this command at system console to allow the start of service without administrator rights:
netsh http add urlacl url=http://+:80/TestService/ user=@SYSTEM_USERNAME

## How it works

1. Client requests by default index.html it is the default page.
2. Default page has the javascript that polls service by url with GET request: /?validate=true
3. If the service response OK (response.status==200), that mean the image could be reloaded
4. If the service response NotFound, client polling waits 1 second and create GET request again


