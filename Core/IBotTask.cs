namespace RoKBot.Core
{
    public interface IBotTask
    {
        // The name shown in logs (e.g., "Training Troops")
        string Name { get; }

        // Can the user turn this off?
        bool IsEnabled { get; set; }

        // Lower number = Higher priority (e.g., Heals = 1, Mining = 10)
        int Priority { get; }

        // Check if we SHOULD run this now (e.g., "Is the timer finished?")
        bool ShouldRun();

        // The actual work
        void Run();
    }
}