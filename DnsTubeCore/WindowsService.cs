using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace DnsTubeCore
{
    public class WindowsService : ServiceBase
    {
        public WindowsService()
        {
        }
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        protected override void OnPause()
        {
            base.OnPause();
        }
    }
}
