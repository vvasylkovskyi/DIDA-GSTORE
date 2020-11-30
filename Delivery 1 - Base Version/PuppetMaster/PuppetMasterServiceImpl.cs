using System;
using System.Threading.Tasks;
using Grpc.Core;
using PuppetMaster.Protos;

namespace PuppetMaster
{
    public class PuppetMasterServiceImpl : PuppetMasterServices.PuppetMasterServicesBase
    {

        public PuppetMasterServiceImpl() { }

        public async override Task<NotifyPuppetMasterReply> NotifyPuppetMaster(NotifyPuppetMasterRequest request, ServerCallContext context)
        {
            return await Task.FromResult(NotifyPuppetMasterHandler(request));
        }

        // -------- Handlers ---------

        public NotifyPuppetMasterReply NotifyPuppetMasterHandler(NotifyPuppetMasterRequest request)
        {

            ConnectionUtils.EstablishPCSConnection(request.Port);
            return new NotifyPuppetMasterReply { Port = "1" };
        }
    }
}
