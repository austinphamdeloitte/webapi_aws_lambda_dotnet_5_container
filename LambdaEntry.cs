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
