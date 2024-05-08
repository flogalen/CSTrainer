using CounterStrikeSharp.API;

namespace OpenPrefirePrac.Utils 
{
    public static class Helpers
    {
        public static void Log(string message, string level = "INFO")
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
        }

        public static void SetConVar(string convarName, string value)
        {
            Server.ExecuteCommand($"{convarName} {value}");
        }
    }
}