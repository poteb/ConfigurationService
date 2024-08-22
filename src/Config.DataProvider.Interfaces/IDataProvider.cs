﻿using pote.Config.DbModel;

namespace pote.Config.DataProvider.Interfaces
{
    public interface IDataProvider : IEnvironmentDataAccess, IApplicationDataAccess, ISecretDataAccess
    {
        //Task<string> GetConfigurationJson(string name, string applicationId, string environment, CancellationToken cancellationToken);
        Task<Configuration> GetConfiguration(string name, string applicationId, string environment, CancellationToken cancellationToken);
        
        Task<ApiKeys> GetApiKeys(CancellationToken cancellationToken);
    }
}