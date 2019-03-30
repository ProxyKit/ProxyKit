using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ProxyKit.Infra
{
    public class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly string _name;

        public XunitLoggerProvider(ITestOutputHelper outputHelper, string name)
        {
            _outputHelper = outputHelper;
            _name = name;
        }

        public void Dispose() { }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestOutputHelperLogger(categoryName, _outputHelper, _name);
        }

        public class TestOutputHelperLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly ITestOutputHelper _outputHelper;
            private readonly string _name;

            public TestOutputHelperLogger(string categoryName, ITestOutputHelper outputHelper, string name)
            {
                _categoryName = categoryName;
                _outputHelper = outputHelper;
                _name = name;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }
                
                _outputHelper.WriteLine($"{_name} {logLevel}: {_categoryName}[{eventId.Id}]:{Environment.NewLine}  {formatter(state, exception)}");
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
        }
    }
}
