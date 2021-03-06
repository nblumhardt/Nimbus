﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Nimbus.Configuration;
using Nimbus.InfrastructureContracts;
using Nimbus.IntegrationTests.Extensions;
using Nimbus.IntegrationTests.Tests.PoisonMessageTests.CommandHandlers;
using Nimbus.IntegrationTests.Tests.PoisonMessageTests.MessageContracts;
using Shouldly;

namespace Nimbus.IntegrationTests.Tests.PoisonMessageTests
{
    [TestFixture]
    public class WhenACommandFailsToBeHandledMoreThanNTimes : SpecificationFor<Bus>
    {
        private TestCommand _testCommand;
        private string _someContent;
        private List<TestCommand> _deadLetterMessages = new List<TestCommand>();

        private const int _maxDeliveryAttempts = 7;

        private ICommandBroker _commandBroker;
        private IRequestBroker _requestBroker;
        private IMulticastRequestBroker _multicastRequestBroker;
        private IMulticastEventBroker _multicastEventBroker;
        private ICompetingEventBroker _competingEventBroker;

        public override Bus Given()
        {
            _commandBroker = Substitute.For<ICommandBroker>();
            _commandBroker.When(cb => cb.Dispatch(Arg.Any<TestCommand>()))
                          .Do(callInfo => new TestCommandHandler().Handle(callInfo.Arg<TestCommand>()));
            _requestBroker = Substitute.For<IRequestBroker>();
            _multicastRequestBroker = Substitute.For<IMulticastRequestBroker>();
            _multicastEventBroker = Substitute.For<IMulticastEventBroker>();
            _competingEventBroker = Substitute.For<ICompetingEventBroker>();

            // Filter types we care about to only our own test's namespace. It's a performance optimisation because creating and
            // deleting queues and topics is slow.
            var typeProvider = new TestHarnessTypeProvider(new[] {GetType().Assembly}, new[] {GetType().Namespace});

            var bus = new BusBuilder().Configure()
                                      .WithNames("MyTestSuite", Environment.MachineName)
                                      .WithConnectionString(CommonResources.ConnectionString)
                                      .WithTypesFrom(typeProvider)
                                      .WithMaxDeliveryAttempts(_maxDeliveryAttempts)
                                      .WithCommandBroker(_commandBroker)
                                      .WithRequestBroker(_requestBroker)
                                      .WithMulticastRequestBroker(_multicastRequestBroker)
                                      .WithMulticastEventBroker(_multicastEventBroker)
                                      .WithCompetingEventBroker(_competingEventBroker)
                                      .WithDebugOptions(
                                          dc =>
                                          dc.RemoveAllExistingNamespaceElementsOnStartup(
                                              "I understand this will delete EVERYTHING in my namespace. I promise to only use this for test suites."))
                                      .Build();
            bus.Start();
            return bus;
        }

        public override void When()
        {
            _someContent = Guid.NewGuid().ToString();
            _testCommand = new TestCommand(_someContent);

            FetchAllDeadLetterMessages().WaitForResult(); // clear the dead letter queue
            Subject.Send(_testCommand).Wait();
            TimeSpan.FromSeconds(10).SleepUntil(() => _commandBroker.ReceivedCalls().Count() >= _maxDeliveryAttempts);
            _deadLetterMessages = FetchAllDeadLetterMessages().WaitForResult();
        }

        [Test]
        public void TheCommandBrokerShouldHaveBeenCalledExactlyNTimes()
        {
            _commandBroker.Received(_maxDeliveryAttempts).Dispatch(Arg.Any<TestCommand>());
        }

        [Test]
        public void ThereShouldBeExactlyOneMessageOnTheDeadLetterQueue()
        {
            _deadLetterMessages.Count.ShouldBe(1);
            _deadLetterMessages.Single().SomeContent.ShouldBe(_someContent);
        }

        private async Task<List<TestCommand>> FetchAllDeadLetterMessages()
        {
            var deadLetterMessages = new List<TestCommand>();
            while (true)
            {
                var message = await Subject.DeadLetterQueues.CommandQueue.Pop<TestCommand>();
                if (message == null) break;
                deadLetterMessages.Add(message);
            }
            return deadLetterMessages;
        }
    }
}