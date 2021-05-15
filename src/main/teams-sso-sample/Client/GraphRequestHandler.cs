using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using teams_sso_sample.Options;

namespace teams_sso_sample.Client
{
    public class GraphRequestHandler : IGraphRequestHandler
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IGraphApiClientFactory _graphApiClientFactory;
        private readonly IOptionsSnapshot<MSGraphOptions> _graphOptions;

        public GraphRequestHandler(
            ITokenAcquisition tokenAcquisition,
            IGraphApiClientFactory graphApiClientFactory,
            IOptionsSnapshot<MSGraphOptions> graphOptions)
        {
            _tokenAcquisition = tokenAcquisition;
            _graphApiClientFactory = graphApiClientFactory;
            _graphOptions = graphOptions;
        }

        public async Task<IActionResult> CheckConsent() =>
            await SendRequest(
                GetAccessTokenForUser,
                accessToken => new OkResult());

        public async Task<IActionResult> GetAccessToken() =>
                await SendRequest(
                    GetAccessTokenForUser,
                    accessToken => new JsonResult(new { data = accessToken }) { StatusCode = 200 });

        public async Task<IActionResult> SendGraphImageRequest(Func<GraphServiceClient, Task<Stream>> graphFunc) =>
                await SendRequest(async () =>
                    await graphFunc(await _graphApiClientFactory.Create(new MediaTypeWithQualityHeaderValue("image/jpg"))),
                    stream => new FileStreamResult(stream, "image/jpg"));

        public async Task<IActionResult> SendGraphRequest<T>(Func<GraphServiceClient, Task<T>> graphFunc) =>
                await SendRequest(async () =>
                    await graphFunc(await _graphApiClientFactory.Create()));

        private async Task<string> GetAccessTokenForUser() =>
            await _tokenAcquisition.GetAccessTokenForUserAsync(_graphOptions.Value.Scopes.Split(' '));

        private static async Task<IActionResult> SendRequest<T>(
            Func<Task<T>> sendFunc,
            Func<T, IActionResult> returnFunc = null)
        {
            try
            {
                var response = await sendFunc().ConfigureAwait(false);

                return returnFunc is null
                    ? new JsonResult(response)
                    : returnFunc(response);
            }
            catch (MicrosoftIdentityWebChallengeUserException ex) 
            when (ex.MsalUiRequiredException.Classification == UiRequiredExceptionClassification.ConsentRequired)
            {
                return new JsonResult(new { error = "consent_required" }) { StatusCode = 403 };
            }
            catch (ServiceException ex)
            {
                return new JsonResult(new { error = $"{ex.Message}" }) { StatusCode = 500 };
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"{ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
