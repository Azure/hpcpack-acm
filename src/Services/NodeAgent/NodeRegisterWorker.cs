namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using T = System.Threading.Tasks;

    public class NodeRegisterWorker : ServerObject, IWorker
    {
        private NodeRegisterWorkerOptions options;
        private NodeRegister register;

        public NodeRegisterWorker(IOptions<NodeRegisterWorkerOptions> options, NodeRegister register)
        {
            this.options = options.Value;
            this.register = register;
        }

        public T.Task InitializeAsync(CancellationToken token)
        {
            this.Logger.Information("Initializing the NodeRegisterWorker");
            return T.Task.CompletedTask;
        }

        private ComputeClusterRegistrationInformation GetRegistrationInfo()
        {
            return new ComputeClusterRegistrationInformation(this.ServerOptions.HostName, Environment.ProcessorCount, 1, 0)
            {
                DistroInfo = Environment.OSVersion.VersionString,
            };
        }

        public async T.Task DoWorkAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (this.options.Enabled)
                    {
                        await this.register.RegisterNodeAsync(this.GetRegistrationInfo(), token);
                    }
                }
                catch(Exception ex)
                {
                    this.Logger.Error(ex, "NodeRegister exception");
                }

                await T.Task.Delay(TimeSpan.FromSeconds(this.Utilities.Option.RegistrationIntervalSeconds), token);
            }
        }
    }
}
