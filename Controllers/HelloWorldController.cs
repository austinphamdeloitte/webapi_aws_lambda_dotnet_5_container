using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace dotnet_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloWorldController: ControllerBase
    {
        public HelloWorldController(ILogger<WeatherForecastController> logger) 
        {
        }
        
        [HttpGet]
        public APIGatewayProxyResponse Get() {
            return new APIGatewayProxyResponse
            {
                Body = "Hello world",
                StatusCode = 200
            };
        }

        [HttpGet("count/{id}")]
        public int Count(int id) {
            return id;
        }

        [HttpGet("count")]
        public int Count() {
            return 0;
        } 
    }
}