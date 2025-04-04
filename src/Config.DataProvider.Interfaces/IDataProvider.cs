﻿using pote.Config.DbModel;

namespace pote.Config.DataProvider.Interfaces
{
    public interface IDataProvider : IEnvironmentDataAccess, IApplicationDataAccess
    {
        //Task<string> GetConfigurationJson(string name, string applicationId, string environment, CancellationToken cancellationToken);
        Task<Configuration> GetConfiguration(string name, string applicationId, string environment, CancellationToken cancellationToken);
        
        Task<ApiKeys> GetApiKeys(CancellationToken cancellationToken);
        
        Task<string> GetSecretValue(string name, string applicationId, string environmentId, CancellationToken cancellationToken);
    }
}