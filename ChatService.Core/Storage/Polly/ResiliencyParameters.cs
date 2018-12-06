using System;
using System.Collections.Generic;
using System.Text;

namespace ChatService.Core.Storage.Polly
{
    public class ResiliencyParameters
    {
        public double CircuitBreakerFailureThreshold { get; set; }
        public int CircuitBreakerSamplingDuration { get; set; }
        public int CircuitBreakerMinimumThroughput { get; set; }
        public int CircuitBreakerDurationOfBreak { get; set; }
        public int NumberOfRetries { get; set; }

    }
}
