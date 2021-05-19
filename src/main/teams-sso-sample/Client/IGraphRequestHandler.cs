using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using System;
using System.IO;
using System.Threading.Tasks;

namespace teams_sso_sample.Client
{
    public interface IGraphRequestHandler
    {
        Task<IActionResult> CheckConsent();

        Task<IActionResult> GetAccessToken();

        Task<IActionResult> SendGraphRequest<T>(Func<GraphServiceClient, Task<T>> graphFunc);

        Task<IActionResult> SendGraphImageRequest(Func<GraphServiceClient, Task<Stream>> graphFunc);
    }
}
