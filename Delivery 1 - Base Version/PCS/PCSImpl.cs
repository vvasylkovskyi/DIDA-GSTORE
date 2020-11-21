﻿using System.Threading.Tasks;
using Grpc.Core;
using Shared.Util;

namespace PCS
{
    public class PCSImpl: PCSServices.PCSServicesBase
    {
        ProcessCreationService processCreationService;

        public PCSImpl(ProcessCreationService processCreationService)
        {
            this.processCreationService = processCreationService;
        }

        public async override Task<StartServerReply> StartServer(StartServerRequest request, ServerCallContext context)
        {
            return await Task.FromResult(StartServerHandler(request));
        }

        public async override Task<StartClientReply> StartClient(StartClientRequest request, ServerCallContext context)
        {
            return await Task.FromResult(StartClientHandler(request));
        }

        public async override Task<FreezeReply> Freeze(FreezeRequest request, ServerCallContext context)
        {
            return await Task.FromResult(FreezeHandler());
        }

        public async override Task<UnfreezeReply> Unfreeze(UnfreezeRequest request, ServerCallContext context)
        {
            return await Task.FromResult(UnfreezeHandler());
        }

        public async override Task<CrashReply> Crash(CrashRequest request, ServerCallContext context)
        {
            return await Task.FromResult(CrashHandler());
        }

        public async override Task<StatusReply> GlobalStatus(StatusRequest request, ServerCallContext context)
        {
            return await Task.FromResult(GlobalStatusHandler());
        }


        // -------- Handlers ---------

        public StartServerReply StartServerHandler(StartServerRequest request)
        {
            string[] args = Utilities.BuildArgsArrayFromArgsString(request.Args);
            processCreationService.StartServer(args);
            return new StartServerReply { StartServer = "1" };
        }

        public StartClientReply StartClientHandler(StartClientRequest request)
        {
            string[] args = Utilities.BuildArgsArrayFromArgsString(request.Args);
            processCreationService.StartClient(args);
            return new StartClientReply { StartClient = "1" };
        }

        public StatusReply GlobalStatusHandler()
        {
            processCreationService.GlobalStatus();
            return new StatusReply { Status = "1" };
        }

        public FreezeReply FreezeHandler()
        {
            processCreationService.Freeze();
            return new FreezeReply { Freeze = "1" };
        }

        public UnfreezeReply UnfreezeHandler()
        {
            processCreationService.Unfreeze();
            return new UnfreezeReply { Unfreeze = "1" };
        }

        public CrashReply CrashHandler()
        {
            processCreationService.Crash();
            return new CrashReply { Crash = "1" };
        }
    }
}
