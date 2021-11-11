# Lambda dotnet 5 setup

## Full code

Check full code in the master branch

## Requirement

- [AWS Command Line Interface (amazon.com)](https://aws.amazon.com/cli/)
- [aws-lambda-dotnet/Tools/LambdaTestTool at master · aws/aws-lambda-dotnet (github.com)](https://github.com/aws/aws-lambda-dotnet/tree/master/Tools/LambdaTestTool) (Install for local testing)

## Quick start

- Run `dotnet new webapi`

- Add this to `launchSettings.json`

```json
"Mock Lambda Test Tool": {
    "commandName": "Executable",
    "commandLineArgs": "--port 5050",
    "workingDirectory": ".\\bin\\$(Configuration)\\net5.0",
    "executablePath": "%USERPROFILE%\\.dotnet\\tools\\dotnet-lambda-test-tool-5.0.exe"
}
```

  - Add a `Dockerfile`

```Dockerfile
FROM public.ecr.aws/lambda/dotnet:5.0

WORKDIR /var/task

# This COPY command copies the .NET Lambda project's build artifacts from the host machine into the image. 
# The source of the COPY should match where the .NET Lambda project publishes its build artifacts. If the Lambda function is being built 
# with the AWS .NET Lambda Tooling, the `--docker-host-build-output-dir` switch controls where the .NET Lambda project
# will be built. The .NET Lambda project templates default to having `--docker-host-build-output-dir`
# set in the aws-lambda-tools-defaults.json file to "bin/Release/lambda-publish".
#
# Alternatively Docker multi-stage build could be used to build the .NET Lambda project inside the image.
# For more information on this approach checkout the project's README.md file.
COPY "bin/Release/lambda-publish" .

```

- Add these packages in `csproj`

```
<PackageReference Include="Amazon.Lambda.AspNetCoreServer" Version="6.1.0" />
<PackageReference Include="Amazon.Lambda.Core" Version="2.1.0" />
<PackageReference Include="Amazon.Lambda.Serialization.SystemTextJson" Version="2.2.0" />
```

- Run `dotnet restore`
- Create a `LocalEntry.cs` (to replace `Program.cs`) for `dotnet run`

```c#
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

namespace dotnet_api
{
    /// <summary>
    /// The Main function can be used to run the ASP.NET Core application locally using the Kestrel webserver.
    /// </summary>
    public class LocalEntryPoint
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}

```

This basically use `Kestrel` to run the server instead of lambda for local debugging. Now you can run using `dotnet run`

You can run `dotnet run` now to test your function locally. Should work.

- Modify `Startup.cs` . Feel free to adjust as needed, this is the minimal for me to work

```C#
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dotnet_api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {

            loggerFactory.AddLambdaLogger(Configuration.GetLambdaLoggerOptions());
            app.UseHsts().UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

```

- Create `LambdaEntry.cs`

```c#
using System.IO;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer.Internal;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace dotnet_api
{
    public class LambdaEntry: Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
    {
        protected override void Init(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .UseLambdaServer();
        }

        protected override void MarshallRequest(InvokeFeatures features,
                                                APIGatewayProxyRequest apiGatewayRequest,
                                                ILambdaContext lambdaContext)
        {
            LambdaLogger.Log($"Request path: {apiGatewayRequest.Path}");
            LambdaLogger.Log($"Request path parameters: {apiGatewayRequest.PathParameters}");
            LambdaLogger.Log($"Request body: {apiGatewayRequest.Body}");
            LambdaLogger.Log($"Request request context: {apiGatewayRequest.RequestContext}");

            /* NOTE: ONLY NEEDED IF MULTIPLE LAMBDA USING THE SAME API GATEWAY */
            var pathBase = "/aws_lambda_cs_net_5/";
            apiGatewayRequest.Path = 
                apiGatewayRequest.Path.Substring(pathBase.Length - 1);
            /* */
            
            base.MarshallRequest(features, apiGatewayRequest, lambdaContext);
        }
    }
}

```

**Note**: The commented code you add only if needed, I'll explain why that's the case below.



- Create `aws-lambda-tools-defaults.json`

```json
{
    "Information" : [
        "This file provides default values for the deployment wizard inside Visual Studio and the AWS Lambda commands added to the .NET Core CLI.",
        "To learn more about the Lambda commands with the .NET Core CLI execute the following command at the command line in the project root directory.",
        "dotnet lambda help",
        "All the command line options for the Lambda command can be specified in this file."
    ],
    "profile"     : "default",
    "region"      : "ap-southeast-2",
    "configuration" : "Release",
    "package-type"  : "Image",
    "function-memory-size" : 256,
    "function-timeout"     : 30,
    "image-command"        : "dotnet_api::dotnet_api.LambdaEntry::FunctionHandlerAsync",
    "docker-host-build-output-dir" : "./bin/Release/lambda-publish",
    "function-name"                : "aws_lambda_cs_net_5",
    "function-role"                : "arn:aws:iam::453559296919:role/lambda_exec_aws_dotnet_cs_5",
    "function-architecture"        : "x86_64",
    "tracing-mode"                 : "PassThrough",
    "environment-variables"        :  "\"LAMBDA_NET_SERIALIZER_DEBUG\"=\"true\";",
    "dockerfile"                   : "Dockerfile",
    "image-tag"                    : "aws_lambda_cs_net_5:latest"
}
```

Change the region, profile, function name and stuff. Please notice that the image-command is in the format of `name_space::name_space.Class::FunctionHandlerAsync` We have to use `FunctionHandlerAsync` from the `APIGatewayProxyFunction` base class as the entry point.

We enable **LAMBDA_NET_SERIALIZER_DEBUG** to **true** for debugging purpose in cloudwatch



That's pretty much done with the code. Now we setup our API gateway

## API gateway

![image-20211111115621357](C:\Projects\dotnet_api\docs\README.assets\image-20211111115621357.png)

This is my API gateway setup. We create `Resources` for each route path, and `Action` for each  HTTP method

![image-20211111115748672](C:\Projects\dotnet_api\docs\README.assets\image-20211111115748672.png)

**Note:** remember to click `Deploy API` once you're done.



We always check **Use Lambda Proxy integration**. This gives us a lot of information from the request side including IP, browser and so on.

![image-20211111115832525](C:\Projects\dotnet_api\docs\README.assets\image-20211111115832525.png)

## Explanation for the subpath replacement in `LambdaEntry.cs`

**Some gotcha**: When using the "Add Trigger" in lambda function, API gateway will create a path for your function:

![image-20211111120150763](C:\Projects\dotnet_api\docs\README.assets\image-20211111120150763.png)



When doing this, the API gateway will create a sub-route with the `function name`. For example:

![image-20211111120354851](C:\Projects\dotnet_api\docs\README.assets\image-20211111120354851.png)

Therefore, if your api is programmed for `localhost:5000/weatherforecast`, the path from api gateway will actually be `/aws_lambda_cs_net_5/weatherforecast`. Which will return back a **404 Not Found**.



### Solution

1. Create API Gateway by hand, make sure it going towards this format (no function name in the hierarchy):

   ![image-20211111120645902](C:\Projects\dotnet_api\docs\README.assets\image-20211111120645902.png)

2. If you're too deep in the mud and not willing to recreate everything by hand, override `MarshallRequest` and try to intercept the `APIGatewayProxyRequest.path` like I did:

   ```C#
           protected override void MarshallRequest(InvokeFeatures features,
                                                   APIGatewayProxyRequest apiGatewayRequest,
                                                   ILambdaContext lambdaContext)
           {
               LambdaLogger.Log($"Request path: {apiGatewayRequest.Path}");
               LambdaLogger.Log($"Request path parameters: {apiGatewayRequest.PathParameters}");
               LambdaLogger.Log($"Request body: {apiGatewayRequest.Body}");
               LambdaLogger.Log($"Request request context: {apiGatewayRequest.RequestContext}");
   
               /* NOTE: ONLY NEEDED IF MULTIPLE LAMBDA USING THE SAME API GATEWAY */
               var pathBase = "/aws_lambda_cs_net_5/";
               apiGatewayRequest.Path = 
                   apiGatewayRequest.Path.Substring(pathBase.Length - 1);
               /* */
               
               base.MarshallRequest(features, apiGatewayRequest, lambdaContext);
           }
   ```

   This simply just cut the first path of the string, so `/aws_lambda_cs_net_5/weatherforecast` becomes `/weatherforecast`. **I'd recommend this solution when we're having multiple lambda within 1 API gateway**.

   The code above is adapted from this: [aws-lambda-dotnet/Libraries/src/Amazon.Lambda.AspNetCoreServer at master · aws/aws-lambda-dotnet (github.com)](https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.AspNetCoreServer#web-app-path-base) - but they're using `ApplicationLoadBalancerFunction`, but we're using `APIGatewayProxyFunction` as the class base so in our case it would be `MarshellRequest` instead of `PostMarshallRequestFeature`



## Deploy

`dotnet lambda deploy-function`



## Testing and stuff

1. I haven't try unit test, but it should work

2. To run locally: `dotnet run`

3. To run locally and send events / post request and so on: `dotnet lambda-test-tools-5.0`

   **Note:** if you're planning to run with environment variables, please use Visual Studio and click on the `Mock Lambda Test Tool` instead.

   Example request payload:

   ```
   {
   "httpMethod":"get",
   "path":"/weatherforecast"
   }
   ```

   

