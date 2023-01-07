namespace pote.Config.DataProvider.Interfaces;

public interface IApplicationDataAccess
{
    Task<List<DbModel.Application>> GetApplications(CancellationToken cancellationToken);
}