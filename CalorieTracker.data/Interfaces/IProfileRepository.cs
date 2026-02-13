using CalorieTracker.data.Models;
using CalorieTracker.data.Models.Events;

namespace CalorieTracker.data.Interfaces
{
    public interface IProfileRepository
    {
        UserProfile? CurrentProfile { get; }
        UserSettings? CurrentSettings { get; }
        List<WeightLog> WeightLogs { get; }

        bool HasUnsavedChanges { get; }
        bool IsSaving { get; }
        bool IsInitialized { get; }

        Task InitializeAsync();
        Task UpdateProfileAsync(Action<UserProfile> updateAction);
        Task UpdateSettingsAsync(Action<UserSettings> updateAction);
        Task<WeightLog> AddWeightLogAsync(WeightLog weightLog);
        Task<bool> UpdateWeightLogAsync(WeightLog weightLog);
        Task<bool> DeleteWeightLogAsync(int id);
        Task SaveChangesAsync();

        bool CanUndo { get; }
        bool CanRedo { get; }
        Task UndoAsync();
        Task RedoAsync();
    }
}
