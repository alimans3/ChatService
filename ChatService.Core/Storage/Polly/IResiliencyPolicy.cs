using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Core.Storage.Polly
{
    public interface IResiliencyPolicy
    {

        Task ExecuteAsync(Func<Task> action);

        Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action);

    }
}
