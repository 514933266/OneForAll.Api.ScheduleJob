using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleJob.Host
{
    public interface ITenantProvider
    {
        Guid GetTenantId();
    }
}
