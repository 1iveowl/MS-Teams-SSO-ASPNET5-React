using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace teams_sso_sample.Client
{
    public class GraphRequestHandler
    {
        private readonly IGraphApiClientFactory _graphApiClientFactory;

        public GraphRequestHandler(IGraphApiClientFactory graphApiClientFactory)
        {
            _graphApiClientFactory = graphApiClientFactory;

        }

        //public async Task<object> SendRequest(string s)
        //{
        //    var graphClient = await _graphApiClientFactory.Create();


        //}
    }
}
