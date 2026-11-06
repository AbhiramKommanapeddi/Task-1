using System;
using System.Collections.Generic;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ResilientFormAutomation
{
    public class AutomationEngine : IDisposable
    {
        private readonly Configuration _config;
        private IWebDriver? _driver;
        private ResilientLocator? _locator;

        public AutomationEngine(Configuration config)
        {
            _config = config;
        }

        public void InitializeDriver()
        {
            Console.WriteLine($"[Setup] Launching Chrome (Headless: {_config.Headless})...");
            var options = new ChromeOptions();
            if (_config.Headless)
            {
                options.AddArgument("--headless=new");
            }
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--ignore-certificate-errors");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

            _locator = new ResilientLocator(_driver, _config.TimeoutSeconds);
        }

        public List<FieldAutomationResult> ExecuteAutomation(string? overrideUrl = null)
        {
            if (_driver == null || _locator == null)
            {
                InitializeDriver();
            }

            string url = overrideUrl ?? _config.TargetUrl;
            Console.WriteLine($"\n[Navigation] Navigating to target URL: {url}");
            _driver!.Navigate().GoToUrl(url);

            // Wait for DOM readiness
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_config.TimeoutSeconds));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");

            Console.WriteLine($"[Navigation] Page Loaded: '{_driver.Title}'\n");

            var results = new List<FieldAutomationResult>();

            // 1. First Name Field
            results.Add(AutomateFirstName(_config.FirstNameValue));

            // 2. Gender Radio Button
            results.Add(AutomateGender(_config.GenderValue));

            // 3. State Dropdown
            results.Add(AutomateState(_config.StateValue));

            return results;
        }

        public FieldAutomationResult AutomateFirstName(string nameValue)
        {
            var result = new FieldAutomationResult
            {
                FieldName = "First Name (Text Box)",
                AttemptedValue = nameValue
            };

            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine($"[Test 1/3] Automating 'First Name' with value: \"{nameValue}\"");

            try
            {
                var candidate = _locator!.FindFirstNameInput();
                var element = candidate.Element;

                Console.WriteLine($"  [Locator] Selected element: <{candidate.ElementTag} id='{candidate.ElementId}' name='{candidate.ElementName}'>");
                Console.WriteLine($"  [Confidence] Heuristic Score: {candidate.Score:F1}");
                Console.WriteLine($"  [Signals] {string.Join(" | ", candidate.MatchedSignals)}");

                ScrollIntoView(element);

                // Clear existing content and type new value
                element.Clear();
                element.SendKeys(nameValue);

                // Small pause for any onchange / React state bindings
                Thread.Sleep(200);

                string confirmedValue = element.GetAttribute("value") ?? "";
                bool success = string.Equals(confirmedValue, nameValue, StringComparison.Ordinal);

                result.Success = success;
                result.ConfirmedValue = confirmedValue;
                result.WinningStrategy = string.Join(", ", candidate.MatchedSignals);
                result.ConfidenceScore = candidate.Score;
                result.Details = success
                    ? $"SUCCESS: Typed \"{nameValue}\" and verified attribute value equals \"{confirmedValue}\"."
                    : $"MISMATCH: Expected \"{nameValue}\" but retrieved \"{confirmedValue}\".";

                Console.WriteLine($"  [Verification] {result.Details}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Details = $"EXCEPTION: {ex.Message}";
                Console.WriteLine($"  [Error] Failed to automate First Name: {ex.Message}");
            }

            return result;
        }

        public FieldAutomationResult AutomateGender(string genderOption)
        {
            var result = new FieldAutomationResult
            {
                FieldName = "Gender (Radio Button)",
                AttemptedValue = genderOption
            };

            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine($"[Test 2/3] Automating 'Gender' with option: \"{genderOption}\"");

            try
            {
                var candidate = _locator!.FindGenderRadio(genderOption);
                var element = candidate.Element;

                Console.WriteLine($"  [Locator] Selected element: <{candidate.ElementTag} id='{candidate.ElementId}' name='{candidate.ElementName}'>");
                Console.WriteLine($"  [Confidence] Heuristic Score: {candidate.Score:F1}");
                Console.WriteLine($"  [Signals] {string.Join(" | ", candidate.MatchedSignals)}");

                ScrollIntoView(element);

                // Click radio button with JS fallback if overlay/label intercepts
                SafeClick(element);

                Thread.Sleep(200);

                bool isSelected = element.Selected;
                result.Success = isSelected;
                result.ConfirmedValue = isSelected ? genderOption : "Not Selected";
                result.WinningStrategy = string.Join(", ", candidate.MatchedSignals);
                result.ConfidenceScore = candidate.Score;
                result.Details = isSelected
                    ? $"SUCCESS: Selected radio button \"{genderOption}\" (Verified Selected: true)."
                    : $"FAILURE: Radio button was clicked but Selected property is false.";

                Console.WriteLine($"  [Verification] {result.Details}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Details = $"EXCEPTION: {ex.Message}";
                Console.WriteLine($"  [Error] Failed to automate Gender: {ex.Message}");
            }

            return result;
        }

        public FieldAutomationResult AutomateState(string stateOption)
        {
            var result = new FieldAutomationResult
            {
                FieldName = "State (Dropdown)",
                AttemptedValue = stateOption
            };

            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine($"[Test 3/3] Automating 'State' with option: \"{stateOption}\"");

            try
            {
                var candidate = _locator!.FindStateSelect(stateOption);
                var element = candidate.Element;

                Console.WriteLine($"  [Locator] Selected element: <{candidate.ElementTag} id='{candidate.ElementId}' name='{candidate.ElementName}'>");
                Console.WriteLine($"  [Confidence] Heuristic Score: {candidate.Score:F1}");
                Console.WriteLine($"  [Signals] {string.Join(" | ", candidate.MatchedSignals)}");

                ScrollIntoView(element);

                var selectObj = new SelectElement(element);
                bool selected = false;
                string confirmed = "";

                // Attempt 1: Exact visible text
                try
                {
                    selectObj.SelectByText(stateOption);
                    confirmed = selectObj.SelectedOption.Text.Trim();
                    selected = true;
                }
                catch
                {
                    // Attempt 2: Select by value
                    try
                    {
                        selectObj.SelectByValue(stateOption);
                        confirmed = selectObj.SelectedOption.Text.Trim();
                        selected = true;
                    }
                    catch
                    {
                        // Attempt 3: Case-insensitive / partial match
                        foreach (var opt in selectObj.Options)
                        {
                            if (opt.Text.IndexOf(stateOption, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                (opt.GetAttribute("value")?.IndexOf(stateOption, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                            {
                                opt.Click();
                                confirmed = opt.Text.Trim();
                                selected = true;
                                break;
                            }
                        }
                    }
                }

                result.Success = selected;
                result.ConfirmedValue = confirmed;
                result.WinningStrategy = string.Join(", ", candidate.MatchedSignals);
                result.ConfidenceScore = candidate.Score;
                result.Details = selected
                    ? $"SUCCESS: Selected dropdown option \"{confirmed}\"."
                    : $"FAILURE: Could not find matching option for \"{stateOption}\".";

                Console.WriteLine($"  [Verification] {result.Details}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Details = $"EXCEPTION: {ex.Message}";
                Console.WriteLine($"  [Error] Failed to automate State: {ex.Message}");
            }

            return result;
        }

        private void ScrollIntoView(IWebElement element)
        {
            try
            {
                ((IJavaScriptExecutor)_driver!).ExecuteScript(
                    "arguments[0].scrollIntoView({behavior: 'instant', block: 'center', inline: 'nearest'});",
                    element);
            }
            catch {}
        }

        private void SafeClick(IWebElement element)
        {
            try
            {
                element.Click();
            }
            catch (ElementClickInterceptedException)
            {
                // Fallback to JS click if custom label overlays the radio input
                ((IJavaScriptExecutor)_driver!).ExecuteScript("arguments[0].click();", element);
            }
            catch (WebDriverException)
            {
                ((IJavaScriptExecutor)_driver!).ExecuteScript("arguments[0].click();", element);
            }
        }

        public void Dispose()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch {}
        }
    }
}

