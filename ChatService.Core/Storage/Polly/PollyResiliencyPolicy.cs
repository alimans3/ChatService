using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;

namespace ChatService.Core.Storage.Polly
{
    public class PollyResiliencyPolicy<T> :IResiliencyPolicy
        where T: Exception 
    {

        private readonly RetryPolicy retryPolicy;
        private readonly CircuitBreakerPolicy circuitBreakerPolicy;
        private readonly PolicyWrap policies;

        public PollyResiliencyPolicy(ResiliencyParameters parameters)
        {

            circuitBreakerPolicy = Policy.Handle<T>().AdvancedCircuitBreakerAsync(
                failureThreshold: parameters.CircuitBreakerFailureThreshold,
                samplingDuration: TimeSpan.FromSeconds(parameters.CircuitBreakerDurationOfBreak),
                minimumThroughput: parameters.CircuitBreakerSamplingDuration,
                durationOfBreak: TimeSpan.FromSeconds(parameters.CircuitBreakerMinimumThroughput));

            retryPolicy = Policy.Handle<T>().RetryAsync(parameters.NumberOfRetries);

            this.policies = Policy.WrapAsync(circuitBreakerPolicy, retryPolicy);
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
             await policies.ExecuteAsync(action);
        }

        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
        {
            return await policies.ExecuteAsync(action);
        }
    }
}
