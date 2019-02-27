using System;
using System.Threading;
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

        [Fact]
        public void Given_one_host_then_ensure_lock_is_released()
        {
            var roundRobin = new RoundRobin
            {
                new UpstreamHost("http://localhost:5001/")
            };
            var upstreamHost = roundRobin.Next();
            upstreamHost.ToString().ShouldBe("http://localhost:5001/");
            var thread = new Thread(() =>
            {
                roundRobin.Add(new UpstreamHost("http://localhost:5002/"));
            });
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(1)).ShouldBe(true);
        }
    }
}
