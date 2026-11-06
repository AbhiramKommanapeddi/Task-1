using System;
using System.Collections.Generic;

namespace ResilientFormAutomation
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("===============================================================================");
            Console.WriteLine("  AUTOMATION PRACTICE FORM - RESILIENT SELENIUM TEST SUITE");
            Console.WriteLine("  Candidate: Kommanapeddi V S D Abhiram");
            Console.WriteLine("  Task 1: Automation that keeps working when the page changes");
            Console.WriteLine("===============================================================================");

            var config = Configuration.Parse(args);

            Console.WriteLine($"[Config] Target URL       : {config.TargetUrl}");
            Console.WriteLine($"[Config] Browser Mode     : {(config.Headless ? "Headless Chrome" : "Visible Chrome Window")}");
            Console.WriteLine($"[Config] Timeout          : {config.TimeoutSeconds}s");
            Console.WriteLine($"[Config] Input First Name : \"{config.FirstNameValue}\"");
            Console.WriteLine($"[Config] Select Gender    : \"{config.GenderValue}\"");
            Console.WriteLine($"[Config] Select State     : \"{config.StateValue}\"");
            Console.WriteLine($"[Config] Mutation Suite   : {(config.RunMutationsTest ? "ENABLED" : "Run with --test-mutations to execute")}");
            Console.WriteLine("===============================================================================\n");

            bool allPassed = true;

            using (var engine = new AutomationEngine(config))
            {
                if (config.RunMutationsTest)
                {
                    Console.WriteLine(">>> RUNNING RESILIENCE MUTATION SUITE (PROVING PAGE MUTATION TOLERANCE) <<<\n");

                    // 1. Mutation A: Reordered fields & Scrambled IDs
                    Console.WriteLine("*******************************************************************************");
                    Console.WriteLine("  MUTATION TEST 1: Reordered Fields (State at top, First Name at bottom),");
                    Console.WriteLine("                   Random GUID IDs, Classes Stripped");
                    Console.WriteLine("*******************************************************************************");
                    string mutationAUrl = ResilienceMutations.GenerateMutationA();
                    var resA = engine.ExecuteAutomation(mutationAUrl);
                    PrintSummaryTable("Mutation Test 1 (Reordered & Scrambled IDs)", resA);
                    if (resA.Exists(r => !r.Success)) allPassed = false;

                    // 2. Mutation B: Lookalike Fields Disambiguation
                    Console.WriteLine("\n*******************************************************************************");
                    Console.WriteLine("  MUTATION TEST 2: Lookalike Fields Challenge (Full vs Middle vs First vs Last Name,");
                    Console.WriteLine("                   Country vs State vs City dropdowns, Multiple Radio Groups)");
                    Console.WriteLine("*******************************************************************************");
                    string mutationBUrl = ResilienceMutations.GenerateMutationB();
                    var resB = engine.ExecuteAutomation(mutationBUrl);
                    PrintSummaryTable("Mutation Test 2 (Lookalike Fields Disambiguation)", resB);
                    if (resB.Exists(r => !r.Success)) allPassed = false;

                    // 3. Mutation C: Structural Hierarchy Change (Table layout without IDs)
                    Console.WriteLine("\n*******************************************************************************");
                    Console.WriteLine("  MUTATION TEST 3: Structural Hierarchy Change (HTML Table layout,");
                    Console.WriteLine("                   no traditional IDs or .form-group classes)");
                    Console.WriteLine("*******************************************************************************");
                    string mutationCUrl = ResilienceMutations.GenerateMutationC();
                    var resC = engine.ExecuteAutomation(mutationCUrl);
                    PrintSummaryTable("Mutation Test 3 (HTML Table Layout without IDs)", resC);
                    if (resC.Exists(r => !r.Success)) allPassed = false;

                    Console.WriteLine("\n>>> NOW EXECUTING AGAINST LIVE TARGET URL <<<\n");
                }

                // Primary Target URL execution
                Console.WriteLine("*******************************************************************************");
                Console.WriteLine($"  PRIMARY EXECUTION: {config.TargetUrl}");
                Console.WriteLine("*******************************************************************************");
                var primaryResults = engine.ExecuteAutomation();
                PrintSummaryTable($"Primary Form Automation ({config.TargetUrl})", primaryResults);
                if (primaryResults.Exists(r => !r.Success)) allPassed = false;
            }

            Console.WriteLine("\n===============================================================================");
            if (allPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  OVERALL RESULT: [PASS] All target fields automated & verified successfully!");
                Console.ResetColor();
                Console.WriteLine("  The resilient locator successfully adapted to all page variations.");
                Console.WriteLine("===============================================================================");
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  OVERALL RESULT: [FAIL] One or more fields failed verification.");
                Console.ResetColor();
                Console.WriteLine("===============================================================================");
                return 1;
            }
        }

        private static void PrintSummaryTable(string title, List<FieldAutomationResult> results)
        {
            Console.WriteLine("\n===============================================================================");
            Console.WriteLine($"  TEST RESULTS SUMMARY: {title}");
            Console.WriteLine("===============================================================================");
            Console.WriteLine($"{"Field Name",-25} | {"Status",-8} | {"Input / Target",-16} | {"Confirmed Value",-18} | {"Score",-6}");
            Console.WriteLine(new string('-', 83));

            foreach (var r in results)
            {
                string statusText = r.Success ? "PASS" : "FAIL";
                Console.ForegroundColor = r.Success ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"{r.FieldName,-25} | {statusText,-8} ");
                Console.ResetColor();
                Console.WriteLine($"| {r.AttemptedValue,-16} | {r.ConfirmedValue,-18} | {r.ConfidenceScore,5:F1}");
            }
            Console.WriteLine("===============================================================================\n");
        }
    }
}
