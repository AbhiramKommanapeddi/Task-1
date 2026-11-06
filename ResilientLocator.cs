using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ResilientFormAutomation
{
    public class ResilientLocator
    {
        private readonly IWebDriver _driver;
        private readonly int _timeoutSeconds;

        public ResilientLocator(IWebDriver driver, int timeoutSeconds = 15)
        {
            _driver = driver;
            _timeoutSeconds = timeoutSeconds;
        }

        #region First Name Finder

        /// <summary>
        /// Locates the First Name input field using multi-signal weighted heuristic ranking.
        /// It is resilient against:
        /// - ID/Name/Class attribute changes or removal
        /// - Visual/DOM reordering
        /// - Structural HTML changes (nested in divs, tables, fieldsets)
        /// - Lookalike fields (Last Name, Middle Name, Full Name, Username)
        /// </summary>
        public CandidateScore FindFirstNameInput()
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_timeoutSeconds));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");

            var allInputs = _driver.FindElements(By.TagName("input"));
            var candidates = new List<CandidateScore>();

            // Collect all labels once to map 'for' attributes quickly
            var allLabels = _driver.FindElements(By.TagName("label")).ToList();

            foreach (var input in allInputs)
            {
                try
                {
                    string type = (input.GetAttribute("type") ?? "text").ToLowerInvariant();
                    // Exclude non-text inputs
                    if (type is "radio" or "checkbox" or "hidden" or "submit" or "button" or "reset" or "password" or "file" or "image")
                    {
                        continue;
                    }

                    var candidate = new CandidateScore
                    {
                        Element = input,
                        ElementTag = input.TagName,
                        ElementId = input.GetAttribute("id") ?? "",
                        ElementName = input.GetAttribute("name") ?? "",
                        ElementType = type
                    };

                    double score = 0;

                    // 1. Accessibility & Visibility
                    if (input.Displayed && input.Enabled)
                    {
                        score += 15;
                        candidate.MatchedSignals.Add("Visible&Enabled (+15)");
                    }
                    else
                    {
                        // Strongly deprioritize hidden elements
                        score -= 50;
                    }

                    // 2. Explicit Label 'for' association
                    if (!string.IsNullOrEmpty(candidate.ElementId))
                    {
                        var matchingLabel = allLabels.FirstOrDefault(l =>
                            string.Equals(l.GetAttribute("for"), candidate.ElementId, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(l.GetAttribute("name"), candidate.ElementId, StringComparison.OrdinalIgnoreCase));

                        if (matchingLabel != null)
                        {
                            string labelText = matchingLabel.Text?.Trim() ?? "";
                            if (Regex.IsMatch(labelText, @"(?i)\bfirst\s*name\b|\bgiven\s*name\b|\bforename\b"))
                            {
                                score += 50;
                                candidate.MatchedSignals.Add($"LabelForMatchesFirstName('{labelText}') (+50)");
                            }
                            else if (Regex.IsMatch(labelText, @"(?i)\blast\s*name\b|\bsurname\b|\bmiddle\b|\bfull\s*name\b|\buser\s*name\b"))
                            {
                                score -= 60;
                                candidate.MatchedSignals.Add($"LabelForMatchesLookalikeField('{labelText}') (-60)");
                            }
                            else if (Regex.IsMatch(labelText, @"(?i)\bname\b"))
                            {
                                score += 15;
                                candidate.MatchedSignals.Add($"LabelForGenericName('{labelText}') (+15)");
                            }
                        }
                    }

                    // 3. Proximity / Enclosing Container Label Analysis
                    string surroundingText = GetSurroundingContextText(input);
                    if (!string.IsNullOrEmpty(surroundingText))
                    {
                        if (Regex.IsMatch(surroundingText, @"(?i)\bfirst\s*name\b|\bgiven\s*name\b|\bforename\b"))
                        {
                            score += 40;
                            candidate.MatchedSignals.Add("ContextHasFirstName (+40)");
                        }
                        if (Regex.IsMatch(surroundingText, @"(?i)\blast\s*name\b|\bsurname\b|\bmiddle\b"))
                        {
                            score -= 40;
                            candidate.MatchedSignals.Add("ContextHasLookalikeLastOrMiddle (-40)");
                        }
                    }

                    // 4. Attribute Semantics: Name
                    string name = candidate.ElementName;
                    if (Regex.IsMatch(name, @"(?i)first[-_\s]?name|^fname$"))
                    {
                        score += 40;
                        candidate.MatchedSignals.Add($"NameAttributeMatches('{name}') (+40)");
                    }
                    else if (Regex.IsMatch(name, @"(?i)last[-_\s]?name|^lname$|middle|user|surname"))
                    {
                        score -= 50;
                        candidate.MatchedSignals.Add($"NameAttributeLookalike('{name}') (-50)");
                    }

                    // 5. Attribute Semantics: ID
                    string id = candidate.ElementId;
                    if (Regex.IsMatch(id, @"(?i)first[-_\s]?name|^fname$"))
                    {
                        score += 35;
                        candidate.MatchedSignals.Add($"IdAttributeMatches('{id}') (+35)");
                    }
                    else if (Regex.IsMatch(id, @"(?i)last[-_\s]?name|^lname$|middle|user|surname"))
                    {
                        score -= 50;
                        candidate.MatchedSignals.Add($"IdAttributeLookalike('{id}') (-50)");
                    }

                    // 6. Attribute Semantics: Placeholder
                    string placeholder = input.GetAttribute("placeholder") ?? "";
                    if (Regex.IsMatch(placeholder, @"(?i)\bfirst\s*name\b|\bgiven\s*name\b"))
                    {
                        score += 35;
                        candidate.MatchedSignals.Add($"PlaceholderMatches('{placeholder}') (+35)");
                    }
                    else if (Regex.IsMatch(placeholder, @"(?i)^\s*name\s*$"))
                    {
                        score += 15;
                        candidate.MatchedSignals.Add($"PlaceholderGenericName('{placeholder}') (+15)");
                    }
                    else if (Regex.IsMatch(placeholder, @"(?i)\blast\s*name\b|\bsurname\b|\bemail\b|\bmobile\b"))
                    {
                        score -= 45;
                        candidate.MatchedSignals.Add($"PlaceholderLookalike('{placeholder}') (-45)");
                    }

                    // 7. HTML5 Autocomplete Standard
                    string autocomplete = input.GetAttribute("autocomplete") ?? "";
                    if (autocomplete.Equals("given-name", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 45;
                        candidate.MatchedSignals.Add("AutocompleteGivenName (+45)");
                    }
                    else if (autocomplete.Equals("family-name", StringComparison.OrdinalIgnoreCase))
                    {
                        score -= 50;
                        candidate.MatchedSignals.Add("AutocompleteFamilyName (-50)");
                    }

                    // 8. ARIA Attributes
                    string ariaLabel = input.GetAttribute("aria-label") ?? "";
                    if (Regex.IsMatch(ariaLabel, @"(?i)\bfirst\s*name\b"))
                    {
                        score += 35;
                        candidate.MatchedSignals.Add($"AriaLabelMatches('{ariaLabel}') (+35)");
                    }

                    candidate.Score = score;
                    candidates.Add(candidate);
                }
                catch (StaleElementReferenceException)
                {
                    // Ignore stale candidates during DOM re-renders
                }
            }

            var best = candidates.OrderByDescending(c => c.Score).FirstOrDefault();
            if (best == null || best.Score <= 10)
            {
                throw new NoSuchElementException($"Could not resiliently locate 'First Name' input. Top candidate scored {best?.Score ?? 0}.");
            }

            return best;
        }

        #endregion

        #region Gender Radio Finder

        /// <summary>
        /// Locates the specific Gender radio button for the target option (e.g. "Male", "Female", "Transgender").
        /// It is resilient against:
        /// - Radio button order changes
        /// - Wrapper layout changes (divs, tables, inline spans, labels)
        /// - Attribute changes (id, name)
        /// - Disambiguates against other radio groups (e.g. Marital Status, Newsletter, etc.)
        /// </summary>
        public CandidateScore FindGenderRadio(string targetOption)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_timeoutSeconds));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");

            var allRadios = _driver.FindElements(By.CssSelector("input[type='radio']"));
            var candidates = new List<CandidateScore>();

            foreach (var radio in allRadios)
            {
                try
                {
                    var candidate = new CandidateScore
                    {
                        Element = radio,
                        ElementTag = radio.TagName,
                        ElementId = radio.GetAttribute("id") ?? "",
                        ElementName = radio.GetAttribute("name") ?? "",
                        ElementType = "radio"
                    };

                    double score = 0;

                    // 1. Accessibility & Visibility
                    if (radio.Displayed && radio.Enabled)
                    {
                        score += 15;
                        candidate.MatchedSignals.Add("Visible&Enabled (+15)");
                    }

                    // 2. Value Attribute Matching
                    string value = radio.GetAttribute("value") ?? "";
                    if (string.Equals(value, targetOption, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 50;
                        candidate.MatchedSignals.Add($"ValueMatchesTarget('{value}') (+50)");
                    }
                    else if (!string.IsNullOrEmpty(value) && !value.Equals(targetOption, StringComparison.OrdinalIgnoreCase))
                    {
                        score -= 50;
                        candidate.MatchedSignals.Add($"ValueMatchesDifferentOption('{value}') (-50)");
                    }

                    // 3. Radio Id matching target option
                    string id = candidate.ElementId;
                    if (string.Equals(id, targetOption, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 35;
                        candidate.MatchedSignals.Add($"IdMatchesTarget('{id}') (+35)");
                    }
                    else if (!string.IsNullOrEmpty(id) && IsKnownOppositeGender(id, targetOption))
                    {
                        score -= 40;
                        candidate.MatchedSignals.Add($"IdMatchesOppositeGender('{id}') (-40)");
                    }

                    // 4. Radio Group Name Semantics
                    string name = candidate.ElementName;
                    if (Regex.IsMatch(name, @"(?i)gender|sex"))
                    {
                        score += 30;
                        candidate.MatchedSignals.Add($"GroupNameHasGender('{name}') (+30)");
                    }
                    else if (Regex.IsMatch(name, @"(?i)marital|status|newsletter|agree|hobbies"))
                    {
                        score -= 40;
                        candidate.MatchedSignals.Add($"GroupNameDifferentCategory('{name}') (-40)");
                    }

                    // 5. Adjacent Label / Span / Text Analysis
                    // Check parent and adjacent siblings (handles <label for="...">Male</label> and <span>Male</span>)
                    string adjacentText = GetAdjacentRadioText(radio);
                    if (!string.IsNullOrEmpty(adjacentText))
                    {
                        if (Regex.IsMatch(adjacentText, $@"(?i)\b{Regex.Escape(targetOption)}\b"))
                        {
                            score += 45;
                            candidate.MatchedSignals.Add($"AdjacentTextMatchesTarget('{adjacentText}') (+45)");
                        }
                        else if (IsKnownOppositeGender(adjacentText, targetOption))
                        {
                            score -= 45;
                            candidate.MatchedSignals.Add($"AdjacentTextMatchesOppositeGender('{adjacentText}') (-45)");
                        }
                    }

                    // 6. Enclosing Container Context (checks if container mentions 'Gender')
                    string containerText = GetSurroundingContextText(radio);
                    if (Regex.IsMatch(containerText, @"(?i)\bgender\b|\bsex\b"))
                    {
                        score += 25;
                        candidate.MatchedSignals.Add("ContainerHasGenderKeyword (+25)");
                    }

                    candidate.Score = score;
                    candidates.Add(candidate);
                }
                catch (StaleElementReferenceException)
                {
                    // Ignore stale elements during DOM updates
                }
            }

            var best = candidates.OrderByDescending(c => c.Score).FirstOrDefault();
            if (best == null || best.Score <= 15)
            {
                throw new NoSuchElementException($"Could not resiliently locate Gender radio button for '{targetOption}'. Top score: {best?.Score ?? 0}.");
            }

            return best;
        }

        #endregion

        #region State Dropdown Finder

        /// <summary>
        /// Locates the State dropdown (select element) using multi-signal heuristic ranking.
        /// It is resilient against:
        /// - ID/Name changes (e.g., id stripped or replaced with random GUID)
        /// - Class name changes
        /// - Visual reordering (moved to top, middle, or bottom)
        /// - Lookalikes: Distinguishes State from Country, City, Nationality, etc.
        /// - Content-aware option inspection: Analyzes dropdown options even if labels are missing!
        /// </summary>
        public CandidateScore FindStateSelect(string preferredOption = "")
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_timeoutSeconds));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");

            var allSelects = _driver.FindElements(By.TagName("select"));
            var candidates = new List<CandidateScore>();
            var allLabels = _driver.FindElements(By.TagName("label")).ToList();

            foreach (var select in allSelects)
            {
                try
                {
                    var candidate = new CandidateScore
                    {
                        Element = select,
                        ElementTag = select.TagName,
                        ElementId = select.GetAttribute("id") ?? "",
                        ElementName = select.GetAttribute("name") ?? "",
                        ElementType = "select"
                    };

                    double score = 0;

                    // 1. Accessibility & Visibility
                    if (select.Displayed && select.Enabled)
                    {
                        score += 15;
                        candidate.MatchedSignals.Add("Visible&Enabled (+15)");
                    }

                    // 2. Explicit Label 'for' association
                    if (!string.IsNullOrEmpty(candidate.ElementId))
                    {
                        var matchingLabel = allLabels.FirstOrDefault(l =>
                            string.Equals(l.GetAttribute("for"), candidate.ElementId, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(l.GetAttribute("name"), candidate.ElementId, StringComparison.OrdinalIgnoreCase));

                        if (matchingLabel != null)
                        {
                            string labelText = matchingLabel.Text?.Trim() ?? "";
                            if (Regex.IsMatch(labelText, @"(?i)\bstate\b|\bprovince\b|\bregion\b"))
                            {
                                score += 50;
                                candidate.MatchedSignals.Add($"LabelForMatchesState('{labelText}') (+50)");
                            }
                            else if (Regex.IsMatch(labelText, @"(?i)\bcountry\b|\bcity\b|\bnationality\b"))
                            {
                                score -= 35;
                                candidate.MatchedSignals.Add($"LabelForMatchesLookalikeField('{labelText}') (-35)");
                            }
                        }
                    }

                    // 3. Proximity / Enclosing Container Context
                    string surroundingText = GetSurroundingContextText(select);
                    if (!string.IsNullOrEmpty(surroundingText))
                    {
                        if (Regex.IsMatch(surroundingText, @"(?i)\bstate\b|\bprovince\b|\bregion\b"))
                        {
                            score += 35;
                            candidate.MatchedSignals.Add("ContextHasStateKeyword (+35)");
                        }
                        if (Regex.IsMatch(surroundingText, @"(?i)\bcountry\b") && !Regex.IsMatch(surroundingText, @"(?i)\bstate\b"))
                        {
                            score -= 30;
                            candidate.MatchedSignals.Add("ContextHasCountryWithoutState (-30)");
                        }
                    }

                    // 4. Name Attribute Semantics
                    string name = candidate.ElementName;
                    if (Regex.IsMatch(name, @"(?i)\bstate\b|province|region"))
                    {
                        score += 40;
                        candidate.MatchedSignals.Add($"NameAttributeMatchesState('{name}') (+40)");
                    }
                    else if (Regex.IsMatch(name, @"(?i)country|city|nationality"))
                    {
                        score -= 35;
                        candidate.MatchedSignals.Add($"NameAttributeLookalike('{name}') (-35)");
                    }

                    // 5. ID Attribute Semantics
                    string id = candidate.ElementId;
                    if (Regex.IsMatch(id, @"(?i)\bstate\b|province|region"))
                    {
                        score += 35;
                        candidate.MatchedSignals.Add($"IdAttributeMatchesState('{id}') (+35)");
                    }
                    else if (Regex.IsMatch(id, @"(?i)country|city|nationality"))
                    {
                        score -= 35;
                        candidate.MatchedSignals.Add($"IdAttributeLookalike('{id}') (-35)");
                    }

                    // 6. Content-Aware Option Inspection
                    // Even if IDs, labels, and classes are completely wiped, the options reveal the element purpose!
                    try
                    {
                        var selectObj = new SelectElement(select);
                        var options = selectObj.Options;

                        if (options.Count > 1)
                        {
                            score += 10;
                            candidate.MatchedSignals.Add($"HasOptions({options.Count}) (+10)");

                            // Check if preferred option is in this select
                            if (!string.IsNullOrWhiteSpace(preferredOption))
                            {
                                bool hasOption = options.Any(o =>
                                    string.Equals(o.Text.Trim(), preferredOption, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(o.GetAttribute("value")?.Trim(), preferredOption, StringComparison.OrdinalIgnoreCase));

                                if (hasOption)
                                {
                                    score += 35;
                                    candidate.MatchedSignals.Add($"ContainsTargetOption('{preferredOption}') (+35)");
                                }
                            }

                            // Check for prompt option mentioning State
                            string firstOptionText = options[0].Text?.Trim() ?? "";
                            if (Regex.IsMatch(firstOptionText, @"(?i)select\s*state|select\s*province|select\s*region"))
                            {
                                score += 30;
                                candidate.MatchedSignals.Add($"FirstOptionIsStatePrompt('{firstOptionText}') (+30)");
                            }
                        }
                    }
                    catch
                    {
                        // Ignore option read failure on custom controls
                    }

                    candidate.Score = score;
                    candidates.Add(candidate);
                }
                catch (StaleElementReferenceException)
                {
                    // Ignore stale elements
                }
            }

            var best = candidates.OrderByDescending(c => c.Score).FirstOrDefault();
            if (best == null || best.Score <= 15)
            {
                throw new NoSuchElementException($"Could not resiliently locate State dropdown. Top candidate scored {best?.Score ?? 0}.");
            }

            return best;
        }

        #endregion

        #region Helper DOM Context Analysis

        private string GetSurroundingContextText(IWebElement element)
        {
            try
            {
                // Execute JS to get closest container text without deep subtree clutter
                string js = @"
                    var el = arguments[0];
                    var container = el.closest('.form-group, fieldset, tr, .row, [role=""group""], div');
                    return container ? container.innerText : '';
                ";
                var result = ((IJavaScriptExecutor)_driver).ExecuteScript(js, element);
                return result?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string GetAdjacentRadioText(IWebElement radio)
        {
            try
            {
                // Check following sibling span/label, parent label, or label[for=id]
                string js = @"
                    var el = arguments[0];
                    var text = '';
                    
                    // 1. Parent label
                    if (el.parentElement && el.parentElement.tagName.toLowerCase() === 'label') {
                        text = el.parentElement.innerText;
                    }
                    
                    // 2. Next sibling element (e.g. span or label)
                    if (!text && el.nextElementSibling) {
                        text = el.nextElementSibling.innerText;
                    }
                    
                    // 3. Connected label via id
                    if (!text && el.id) {
                        var lbl = document.querySelector('label[for=""' + el.id + '""]');
                        if (lbl) text = lbl.innerText;
                    }
                    
                    return text.trim();
                ";
                var res = ((IJavaScriptExecutor)_driver).ExecuteScript(js, radio);
                return res?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private bool IsKnownOppositeGender(string text, string target)
        {
            var options = new[] { "male", "female", "transgender", "other" };
            string lowerTarget = target.ToLowerInvariant();
            string lowerText = text.ToLowerInvariant();

            foreach (var opt in options)
            {
                if (opt != lowerTarget && Regex.IsMatch(lowerText, $@"\b{opt}\b"))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}

