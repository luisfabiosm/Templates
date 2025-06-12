using Domain.Core.Ports.Outbound;
using Domain.Core.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Data;
using System.Data.Common;

namespace Domain.Core.Base
{
    public class BaseNoSQLRepository : BaseService, IDisposable
    {
        protected INoSQLConnectionAdapter _dbConnectionAdapter;
        protected readonly IOptions<DBSettings> _dbsettings;

        public BaseNoSQLRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _dbConnectionAdapter = serviceProvider.GetRequiredService<INoSQLConnectionAdapter>();
            _dbsettings = serviceProvider.GetRequiredService<IOptions<DBSettings>>();
        }

        #region DISPOSE

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
                    (_dbConnectionAdapter as IDisposable)?.Dispose();
                    _dbConnectionAdapter = null;
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
