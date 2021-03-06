﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Shared.Util;

namespace PCS
{
    public class PCSImpl: PCSServices.PCSServicesBase
    {
        Program processCreationService;

        public PCSImpl(Program processCreationService) => this.processCreationService = processCreationService;

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

        public async override Task<UpdateReplicasNumberReply> UpdateReplicasNumber(UpdateReplicasNumberRequest request, ServerCallContext context)
        {
            return await Task.FromResult(UpdateReplicasNumberHandler(request));
        }

        public async override Task<CreatePartitionReply> CreatePartition(CreatePartitionRequest request, ServerCallContext context)
        {
            return await Task.FromResult(CreatePartitionHandler(request));
        }

        public async override Task<UpdateServersReply> UpdateServers(UpdateServersRequest request, ServerCallContext context)
        {
            return await Task.FromResult(UpdateServersHandler(request));
        }
        // -------- Handlers ---------

        public StartServerReply StartServerHandler(StartServerRequest request)
        {
            string[] args = Utilities.BuildArgsArrayFromArgsString(request.Args);
            processCreationService.StartServer(args);
            return new StartServerReply { StartServer = "OK" };
        }

        public StartClientReply StartClientHandler(StartClientRequest request)
        {
            string[] args = Utilities.BuildArgsArrayFromArgsString(request.Args);
            processCreationService.StartClient(args);
            return new StartClientReply { StartClient = "OK" };
        }

        public StatusReply GlobalStatusHandler()
        {
            processCreationService.GlobalStatus();
            return new StatusReply { Status = "OK" };
        }

        public FreezeReply FreezeHandler()
        {
            processCreationService.Freeze();
            return new FreezeReply { Freeze = "OK" };
        }

        public UnfreezeReply UnfreezeHandler()
        {
            processCreationService.Unfreeze();
            return new UnfreezeReply { Unfreeze = "OK" };
        }

        public CrashReply CrashHandler()
        {
            processCreationService.Crash();
            return new CrashReply { Crash = "OK" };
        }

        public UpdateReplicasNumberReply UpdateReplicasNumberHandler(UpdateReplicasNumberRequest request)
        {
            processCreationService.UpdateReplicationFactor(request.ReplicationFactor);
            return new UpdateReplicasNumberReply { UpdateReplicasNumber = "OK" };
        }

        public CreatePartitionReply CreatePartitionHandler(CreatePartitionRequest request)
        {
            string[] args = Utilities.BuildArgsArrayFromArgsString(request.Args);
            string replicationFactor = args[0];
            string partitionName = args[1];

            string[] serverIds = args.Skip(2)
                    .Take(args.Length)
                    .ToArray();

            processCreationService.CreatePartition(replicationFactor, partitionName, serverIds);
            return new CreatePartitionReply { CreatePartititon = "OK" };
        }

        public UpdateServersReply UpdateServersHandler(UpdateServersRequest request)
        {
            processCreationService.UpdateServers(request.ServerId, request.ServerUrl);
            return new UpdateServersReply { UpdateServers = "OK" };
        }
    }
}
