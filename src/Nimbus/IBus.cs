﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nimbus.MessageContracts;
using Nimbus.PoisonMessages;

namespace Nimbus
{
    public interface IBus
    {
        Task Send<TBusCommand>(TBusCommand busCommand) where TBusCommand : IBusCommand;

        Task<TResponse> Request<TRequest, TResponse>(BusRequest<TRequest, TResponse> busRequest)
            where TRequest : IBusRequest<TRequest, TResponse>
            where TResponse : IBusResponse;

        Task<TResponse> Request<TRequest, TResponse>(BusRequest<TRequest, TResponse> busRequest, TimeSpan timeout)
            where TRequest : IBusRequest<TRequest, TResponse>
            where TResponse : IBusResponse;

        Task<IEnumerable<TResponse>> MulticastRequest<TRequest, TResponse>(BusRequest<TRequest, TResponse> busRequest, TimeSpan timeout)
            where TRequest : IBusRequest<TRequest, TResponse>
            where TResponse : IBusResponse;

        Task Publish<TBusEvent>(TBusEvent busEvent) where TBusEvent : IBusEvent;

        IDeadLetterQueues DeadLetterQueues { get; }
    }
}