using System;

namespace ResilientFormAutomation
{
    public class Configuration
    {
        public string TargetUrl { get; set; } = "https://app.cloudqa.io/home/AutomationPracticeForm";
        public bool Headless { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 15;
        public string FirstNameValue { get; set; } = "Abhiram";
        public string GenderValue { get; set; } = "Male";
        public string StateValue { get; set; } = "India";
        public bool RunMutationsTest { get; set; } = false;

        public static Configuration Parse(string[] args)
        {
            var config = new Configuration();

            // Check environment variable fallback
            var envUrl = Environment.GetEnvironmentVariable("TARGET_URL");
            if (!string.IsNullOrWhiteSpace(envUrl))
            {
                config.TargetUrl = envUrl;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].Trim();

                if (arg.Equals("--url", StringComparison.OrdinalIgnoreCase) || arg.Equals("-u", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) config.TargetUrl = args[++i];
                }
                else if (arg.StartsWith("--url=", StringComparison.OrdinalIgnoreCase))
                {
                    config.TargetUrl = arg.Substring("--url=".Length);
                }
                else if (arg.Equals("--headless", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && bool.TryParse(args[i + 1], out bool h))
                    {
                        config.Headless = h;
                        i++;
                    }
                    else
                    {
                        config.Headless = true;
                    }
                }
                else if (arg.Equals("--no-headless", StringComparison.OrdinalIgnoreCase) || arg.Equals("--headed", StringComparison.OrdinalIgnoreCase))
                {
                    config.Headless = false;
                }
                else if (arg.Equals("--firstName", StringComparison.OrdinalIgnoreCase) || arg.Equals("-fn", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) config.FirstNameValue = args[++i];
                }
                else if (arg.Equals("--gender", StringComparison.OrdinalIgnoreCase) || arg.Equals("-g", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) config.GenderValue = args[++i];
                }
                else if (arg.Equals("--state", StringComparison.OrdinalIgnoreCase) || arg.Equals("-s", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) config.StateValue = args[++i];
                }
                else if (arg.Equals("--timeout", StringComparison.OrdinalIgnoreCase) || arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int t))
                    {
                        config.TimeoutSeconds = t;
                        i++;
                    }
                }
                else if (arg.Equals("--test-mutations", StringComparison.OrdinalIgnoreCase) || arg.Equals("--resilience-test", StringComparison.OrdinalIgnoreCase))
                {
                    config.RunMutationsTest = true;
                }
                else if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                    Environment.Exit(0);
                }
                else if (i == 0 && !arg.StartsWith("-"))
                {
                    // Allow first positional argument to be the URL
                    config.TargetUrl = arg;
                }
            }

            return config;
        }

        public static void PrintHelp()
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("  Resilient Form Automation - CLI Usage Options");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("  --url <URL>            : Target URL to test (default: https://app.cloudqa.io/home/AutomationPracticeForm)");
            Console.WriteLine("  --headless [true|false]: Run browser in headless mode (default: true)");
            Console.WriteLine("  --firstName <string>   : Value to input for First Name (default: 'Abhiram')");
            Console.WriteLine("  --gender <string>      : Gender to select (e.g., Male, Female, Transgender)");
            Console.WriteLine("  --state <string>       : State / Country dropdown option (default: 'India')");
            Console.WriteLine("  --timeout <seconds>    : Wait timeout in seconds (default: 15)");
            Console.WriteLine("  --test-mutations       : Run comprehensive DOM mutation resilience test suite");
            Console.WriteLine("  --help, -h             : Show this help message");
            Console.WriteLine("===============================================================================");
        }
    }
}

