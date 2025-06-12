using Domain.Core.Base;
using Domain.Core.Models.Dto;
using Domain.Core.Models.Entity;
using Domain.Core.Ports.Outbound;
using Domain.UseCases.Sample.AddSampleTask;
using Domain.UseCases.Sample.GetSampleTask;
using Domain.UseCases.Sample.ListSampleTask;
using Domain.UseCases.Sample.UpdateSampleTaskTimer;
using MongoDB.Driver;
using System.Data.Common;
using System.Transactions;

namespace Adapters.Outbound.Database.NoSQL.Sample
{
    public class NoSQLSampleRepository : BaseNoSQLRepository, INoSQLSampleRepository, IDisposable
    {

        private readonly string _collectionName = "Tasks";
        private readonly string _correlationId;


        public NoSQLSampleRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {


        }


        public async ValueTask<(SampleTask, Exception exception)> AddSampleTaskAsync(TransactionAddSampleTask transaction)
        {

            using var operationContext = StartOperation("AddSampleTaskAsync", transaction.CorrelationId);

            var _sampleTask = transaction.getSampleTaskDto().MapSampleTask();

            if (_sampleTask == null) throw new ArgumentNullException(nameof(SampleTask));

            AddTraceProperty("Name", _sampleTask.Name);
            AddTraceProperty("Timer", _sampleTask.TimerOnMiliseconds.ToString());

            await _dbConnectionAdapter.ExecuteAsync(async (session) =>
            {
                var collection = _dbConnectionAdapter.GetCollection<SampleTask>(_collectionName);
                await collection.InsertOneAsync(session, _sampleTask);


            });

            LogInformation("Entidade adicionada com sucesso: {EntityId}", _sampleTask.Id);

            return (_sampleTask, null);

        }


        public async ValueTask<(bool, Exception exception)> UpdateSampleTaskTimerAsync(TransactionUpdateSampleTaskTimer transaction)
        {

            using var operationContext = StartOperation("UpdateSampleTaskTimerAsync", transaction.CorrelationId);

            var _sampleTaskDto = transaction.getSampleTaskDto();

            if (_sampleTaskDto == null) throw new ArgumentNullException(nameof(SampleTaskDto));

            AddTraceProperty("Id", _sampleTaskDto.Id.ToString());
            AddTraceProperty("Timer", _sampleTaskDto.TimerOnMilliseconds.ToString());

            var result = await _dbConnectionAdapter.ExecuteAsync(async (session) =>
            {
                var collection = _dbConnectionAdapter.GetCollection<SampleTask>(_collectionName);
                var filter = Builders<SampleTask>.Filter.Eq(u => u.Id, _sampleTaskDto.Id);
                var updateResult = await collection.ReplaceOneAsync(session, filter, _sampleTaskDto.MapSampleTask());
                return updateResult.ModifiedCount > 0;
            });

            LogInformation("Entidade atualizada com sucesso: {EntityId}", _sampleTaskDto.Id);

            return (result, null);

        }


        public async ValueTask<(SampleTask, Exception exception)> GetSampleTaskByIdAsync(TransactionGetSampleTask transaction)
        {

            using var operationContext = StartOperation("GetSampleTaskByIdAsync", transaction.CorrelationId);

            AddTraceProperty("Id", transaction.Id.ToString());

            var _result = await _dbConnectionAdapter.QueryAsync<SampleTask, SampleTask>(_collectionName, async (collection) =>
            {
                var filter = Builders<SampleTask>.Filter.Eq(u => u.Id, 1);
                return await collection.Find(filter).FirstOrDefaultAsync();
            });

            LogInformation("Entidade retornada com sucesso: {EntityId}", _result.Id);

            return (_result, null);

        }

        public async ValueTask<(List<SampleTask>, Exception exception)> ListAllSampleTaskAsync(TransactionListSampleTask transaction)
        {

            using var operationContext = StartOperation("ListAllSampleTaskAsync", transaction.CorrelationId);

            var _result = await _dbConnectionAdapter.QueryAsync<SampleTask, List<SampleTask>>(_collectionName, async (collection) =>
            {
                return await collection.Find(Builders<SampleTask>.Filter.Empty).ToListAsync();
            });

            return (_result, null);
        }


        #region Dispose

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbConnectionAdapter?.Dispose();
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
