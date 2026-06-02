using MediAlert.DTOs.OpenFda;

namespace MediAlert.Services.OpenFda.Interfaces;

public interface IOpenFdaDrugClient
{
    Task<OpenFdaClientResult<OpenFdaDrugSearchResponse>> SearchDrugLabelsAsync(
        OpenFdaDrugSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<OpenFdaClientResult<OpenFdaLabelApiResponse>> GetRawDrugLabelAsync(
        string query,
        int? limit = null,
        CancellationToken cancellationToken = default);
}
