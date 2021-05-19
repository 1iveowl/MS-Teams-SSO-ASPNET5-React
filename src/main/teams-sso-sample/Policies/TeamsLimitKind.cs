using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace teams_sso_sample.Policies
{
    public enum TeamsLimitKind
    {
        TeamAndChannelConcurrencyLimit,
        TeamsGet,
        TeamsPostPut,
    }
}
