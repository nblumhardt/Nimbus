﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Nimbus.IntegrationTests.Tests.MulticastRequestResponseTests.MessageContracts;
using Shouldly;

namespace Nimbus.IntegrationTests.Tests.MulticastRequestResponseTests
{
    [TestFixture]
    public class WhenSendingASixSecondMulticastRequest : SpecificationForBus
    {
        private IEnumerable<BlackBallResponse> _response;

        public override async Task WhenAsync()
        {
            var request = new BlackBallRequest
            {
                ProspectiveMemberName = "Fred Flintstone",
            };

            _response = await Subject.MulticastRequest(request, TimeSpan.FromSeconds(6));
        }

        [Test]
        public void WeShouldReceiveThreeResponses()
        {
            _response.Count().ShouldBe(3);
        }
    }
}