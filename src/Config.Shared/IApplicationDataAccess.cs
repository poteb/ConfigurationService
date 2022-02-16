namespace pote.Config.Shared;

public interface IApplicationDataAccess
{
    Task<List<DbModel.Application>> GetApplications(CancellationToken cancellationToken);
}