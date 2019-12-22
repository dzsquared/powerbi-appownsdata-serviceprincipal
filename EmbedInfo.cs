using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;


namespace Company.Function
{
    public static class EmbedInfo
    {
        [FunctionName("EmbedInfo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestData = ""; 
            string reportInfo = req.Query["id"];

            string ResourceUrl = "https://analysis.windows.net/powerbi/api"; 
            string ApiUrl = "https://api.powerbi.com/"; 
            string ADdomain = Environment.GetEnvironmentVariable("ADdomain");
            string ClientId =  Environment.GetEnvironmentVariable("AppClientId"); 
            string ClientSecret = Environment.GetEnvironmentVariable("AppClientSecret");
            string GroupId = Environment.GetEnvironmentVariable("WorkspaceId");

            var successful = true;
            try
            {
                // create object for response
                var authInfo = new xAuthen();

                // create service principal credential
                var credential = new ClientCredential(ClientId, ClientSecret);

                // Authenticate using created credentials
                string AuthorityUrl = "https://login.microsoftonline.com/"+ADdomain+"/oauth2/v2.0/authorize";
                var authenticationContext = new AuthenticationContext(AuthorityUrl);
                var authenticationResult = await authenticationContext.AcquireTokenAsync(ResourceUrl, credential);

                if (authenticationResult == null)
                {
                    log.LogError("Authentication Failed.");
                }

                var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");
            
                // Create a Power BI Client object. It will be used to call Power BI APIs.
                using (var client = new PowerBIClient(new Uri(ApiUrl), tokenCredentials))
                {
                    var report = await client.Reports.GetReportInGroupAsync(GroupId, reportInfo);

                    // Generate Embed Token.
                    var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                    var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(GroupId, report.Id, generateTokenRequestParameters);

                    if (tokenResponse == null)
                    {
                        log.LogInformation("Failed to generate embed token.");
                    }

                    authInfo.accessToken = (string)tokenResponse.Token;
                    authInfo.embedUrl = (string)report.EmbedUrl;
                    authInfo.embedReportId = (string)report.Id;
                }

                requestData = JsonConvert.SerializeObject(authInfo);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                successful = false;
            }

        if (!successful) {
                return new BadRequestObjectResult("Issue processing the request.");
            } else {
                var responsejson = new OkObjectResult(requestData);
                return responsejson;
            }
        }
    }

    
    public class xAuthen
    {
        public string accessToken;
        public string embedUrl;
        public string embedReportId;
    }
}
