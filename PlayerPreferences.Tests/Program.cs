using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Smod2.API;

namespace PlayerPreferences.Tests
{
    public class Program
    {
        private const string Path = "PlayerPrefs";

        private static void CheckForErrors(IReadOnlyCollection<string> errors)
        {
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length < 0)
            {
                Console.WriteLine("Specify a mode.");
            }

            switch (args[0])
            {
                case "io":
                    IOTest();
                    break;

                case "swap":
                    SwapTest();
                    break;
            }
        }

        public static void SwapTest()
        {
            Console.Write("P1 Rank: ");
            int thisRank = int.Parse(Console.ReadLine() ?? "0");
            Console.Write("P2 Rank: ");
            int otherRank = int.Parse(Console.ReadLine() ?? "0");

            Console.Write("P1 New Rank: ");
            int newThisRank = int.Parse(Console.ReadLine() ?? "0");
            Console.Write("P2 New Rank: ");
            int newOtherRank = int.Parse(Console.ReadLine() ?? "0");

            Console.Write("P1 Avg Rank: ");
            float avg1 = float.Parse(Console.ReadLine() ?? "0");
            Console.Write("P2 Avg Rank: ");
            float avg2 = float.Parse(Console.ReadLine() ?? "0");

            float thisDelta = thisRank - newThisRank + avg1;
            Console.WriteLine($"P1 Delta: {thisDelta}");
            float otherDelta = otherRank - newOtherRank + avg2;
            Console.WriteLine($"P2 Delta: {otherDelta}");

            float sumDelta = thisDelta + otherDelta;
            Console.WriteLine($"Sum Delta: {sumDelta}");

            // If it is a net gain of rankings or the other player is getting demoted but is equal to or above the other rank
            Console.WriteLine($"Swapping: {sumDelta > 0}");
        }

        public static void IOTest()
        {
            List<string> errors = new List<string>();
            new PpPlugin().LoadRoleData();

            Directory.Delete("PlayerPrefs", true);
            Preferences prefs = new Preferences(Path, x => errors.Add(x));

            prefs.Add("123456789", new Role[14]);
            CheckForErrors(errors);
            prefs.Read();
            CheckForErrors(errors);

            prefs.Add("1234567890", PpPlugin.Roles.Select(x => x.Value).ToArray());
            CheckForErrors(errors);
            prefs.Remove("1234567890");
            CheckForErrors(errors);
        }
    }
}
