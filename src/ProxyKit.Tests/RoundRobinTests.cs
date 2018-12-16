using System;
using System.Collections.Generic;
using System.Text;
using Shouldly;
using Xunit;

namespace ProxyKit
{
    public class RoundRobinTests
    {
        [Fact]
        public void Given_two_hosts_then_round_robin_should_distribute_evenly()
        {
            var roundRobin = new RoundRobin
            {
                "http://localhost:5001",
                "http://localhost:5002"
            };

            var upstreamHost = roundRobin.Next();
            upstreamHost.ToString().ShouldBe("http://localhost:5001/");

            upstreamHost = roundRobin.Next();
            upstreamHost.ToString().ShouldBe("http://localhost:5002/");

            upstreamHost = roundRobin.Next();
            upstreamHost.ToString().ShouldBe("http://localhost:5001/");
        }

        [Fact]
        public void Given_two_hosts_with_weight_then_round_robin_should_distribute_evenly()
        {
            var roundRobin = new RoundRobin
            {
                new UpstreamHost("http://localhost:5001/"),
                new UpstreamHost("http://localhost:5002/", 2)
            };

            var upstreamHost = roundRobin.Next();
            upstreamHost.ToString().ShouldBe("http://localhost:5001/");

            upstreamHost = roundRobin.Next();
            upstreamHost.ToString().ShouldBe("http://localhost:5002/");

            upstreamHost = roundRobin.Next();
            upstreamHost.ToString().ShouldBe("http://localhost:5002/");

            upstreamHost = roundRobin.Next();
            upstreamHost.ToString().ShouldBe("http://localhost:5001/");
        }
    }
}
