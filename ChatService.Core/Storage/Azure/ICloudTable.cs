using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace ChatService.Core.Storage.Azure
{
    public interface ICloudTable
    {
        Task CreateIfNotExistsAsync();
        Task<TableResult> ExecuteAsync(TableOperation operation);
        Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation batchOperation);
        Task<TableQuerySegment<T>> ExecuteQuerySegmentedAsync<T>(TableQuery<T> query,TableContinuationToken token) where T: ITableEntity,new();
    }
}