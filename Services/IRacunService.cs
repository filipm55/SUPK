using SUPK.ViewModels;

namespace SUPK.Services
{
    public interface IRacunService
    {
        Task CreateAsync(RacunCreateViewModel vm);
        Task UpdateAsync(RacunEditViewModel vm);
    }
}