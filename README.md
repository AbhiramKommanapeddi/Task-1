# Resilient Web Form Automation (C# & Selenium WebDriver)

**Candidate:** Kommanapeddi V S D Abhiram  
**Target Application:** `https://app.cloudqa.io/home/AutomationPracticeForm`  
**Key Objective:** Create an intelligent automation program for three fields (**First Name**, **Gender**, and **State**) that **keeps working even when the target page undergoes radical changes** (e.g. fields reordered, IDs/names/classes changed or removed, structural DOM altered, or lookalike fields present).

---

## 1. Why Traditional Locators Fail vs. Our Solution

| Failure Mode | Traditional Selector | Why It Breaks | How Our Resilient Locator Solves It |
| :--- | :--- | :--- | :--- |
| **Field Reordering** | `(//input)[1]` or `div:nth-child(2)` | Fails when fields change position or new sections are inserted. | Evaluates semantic relationships rather than DOM sequence or positional indices. |
| **Attribute Mutation** | `By.Id("fname")` or `.form-control` | Modern frameworks (React/Angular/Tailwind) frequently scramble or strip IDs and CSS classes across builds. | Multi-tier heuristic scoring considers explicit labels (`for`), enclosing containers, HTML5 attributes (`autocomplete="given-name"`), ARIA metadata, and fuzzy matching. |
| **DOM Hierarchy Changes** | `//form/div[2]/div/input` | Wrapper changes (divs, tables, fieldsets, flex containers) invalidate fixed paths. | Uses proximity queries, `.closest()` semantic containers, and label-to-input association without depending on fixed tree depth. |
| **Lookalike Elements** | `//input[contains(@placeholder, 'Name')]` | Collides with "Last Name", "Middle Name", "Full Name", or "Username". | Implements **negative penalty scoring**: candidates containing "last", "surname", or "middle" receive negative weights to prevent false positives. |
| **Dropdown / Option Mutations** | `//select[@id='state']/option[34]` | Hardcoded index breaks if states list is sorted differently or ID changes. | **Content-aware option inspection**: analyzes option values/text and uses smart text/value/partial matching. |

---

## 2. Multi-Signal Heuristic Scoring Architecture

Instead of binding to brittle single locators, the `ResilientLocator` inspects all visible candidate elements on the page and computes a weighted composite confidence score:

### A. First Name (Text Box)
1. **Explicit Label Association (`for` / `id` mapping)**:
   - Label text matching `First Name`, `Given Name`, `Forename` $\rightarrow$ `+50`
   - Label text containing `Last Name`, `Surname`, `Middle Name` $\rightarrow$ `-60` (Disambiguation penalty)
2. **Proximity / Enclosing Container Context**:
   - Nearby text within container contains `First Name` $\rightarrow$ `+40`
   - Contains `Last Name` or `Middle Name` $\rightarrow$ `-40`
3. **HTML5 Semantic Attributes**:
   - `autocomplete="given-name"` $\rightarrow$ `+45`
   - `autocomplete="family-name"` $\rightarrow$ `-50`
4. **Attribute Fuzzy Semantics (`name`, `id`, `placeholder`, `aria-label`)**:
   - Matches `first-name`, `fname` $\rightarrow$ `+35` to `+40`
   - Matches `last-name`, `lname`, `surname` $\rightarrow$ `-50`
5. **Visibility & Interactivity**:
   - Displayed and Enabled $\rightarrow$ `+15`

### B. Gender (Radio Button)
1. **Value Attribute Match**:
   - `value` matches target option (`Male`, `Female`, `Transgender`) $\rightarrow$ `+50`
   - `value` matches a different gender $\rightarrow$ `-50`
2. **Adjacent Label / Sibling Text**:
   - Next sibling `<span>`, parent `<label>`, or connected label matching target $\rightarrow$ `+45`
3. **Radio Group Semantics**:
   - `name` attribute contains `gender` or `sex` $\rightarrow$ `+30`
   - Container has keyword `Gender` or `Sex` $\rightarrow$ `+25`
   - Penalize non-gender radio groups (`subscription`, `marital`, `newsletter`) $\rightarrow$ `-40`
4. **Safe Interaction**:
   - Scroll into view, standard click with automatic JavaScript click fallback if custom styling/labels intercept clicks.

### C. State (Dropdown / Select)
1. **Label Association**:
   - Label matching `State`, `Province`, `Region` $\rightarrow$ `+50`
   - Label matching lookalikes (`Country`, `City`, `Nationality`) $\rightarrow$ `-35`
2. **Container Context & Attributes**:
   - `name` or `id` matching `state` $\rightarrow$ `+35` to `+40`
   - Matching `country` or `city` $\rightarrow$ `-35`
3. **Content-Aware Option Inspection**:
   - Inspects the `<option>` elements inside the candidate `<select>`:
     - Contains target state/country option $\rightarrow$ `+35`
     - Prompt option mentions `State` (`-- Select State --`) $\rightarrow$ `+30`
4. **Resilient Selection Strategy**:
   - Tries exact visible text $\rightarrow$ fallback to value attribute $\rightarrow$ fallback to partial text match.

---

## 3. Project Structure

```
Task-1/
├── Program.cs               # CLI entry point, test orchestration, and executive reporting
├── Configuration.cs         # CLI arguments parser (--url, --headless, field inputs)
├── ResilientLocator.cs      # Core multi-strategy heuristic scoring engine
├── AutomationEngine.cs      # Selenium browser lifecycle, driver configuration, and actions
├── FieldModels.cs           # Data structures for candidate scores, signals, and results
├── ResilienceMutations.cs   # Built-in mutation test suite (Reordering, Scrambled IDs, Lookalikes)
├── Task-1.csproj            # .NET 8.0 SDK project file referencing Selenium 4.52.0
├── run.bat                  # Windows batch launcher
├── run.ps1                  # PowerShell launcher
└── README.md                # Documentation and architecture breakdown
```

---

## 4. How to Run

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Google Chrome (or Microsoft Edge)

### Quick Run (Default Target URL in Headless Mode)
```powershell
./run.ps1
```
or via .NET CLI:
```powershell
dotnet run
```

### Passing a Configurable / Modified Page URL
You can point the automation to any modified version of the page via the `--url` argument:
```powershell
dotnet run -- --url "https://app.cloudqa.io/home/AutomationPracticeForm"
```
Or with custom field values:
```powershell
dotnet run -- --url "https://my-custom-test-server/form" --firstName "Abhiram" --gender "Male" --state "California"
```

### Running with a Visible Browser Window (Headed Mode)
```powershell
dotnet run -- --no-headless
```

### Running the Built-In Resilience Mutation Test Suite
To verify that the locator maintains 100% accuracy across scrambled IDs, reordered fields, lookalike forms, and table-based layouts:
```powershell
dotnet run -- --test-mutations
```

---

## 5. Verification & Test Execution Results

### A. Live Production Page (`app.cloudqa.io/home/AutomationPracticeForm`)
```text
===============================================================================
  TEST RESULTS SUMMARY: Primary Form Automation (https://app.cloudqa.io/home/AutomationPracticeForm)
===============================================================================
Field Name                | Status   | Input / Target   | Confirmed Value    | Score 
-----------------------------------------------------------------------------------
First Name (Text Box)     | PASS     | Abhiram          | Abhiram            | 195.0
Gender (Radio Button)     | PASS     | Male             | Male               | 175.0
State (Dropdown)          | PASS     | India            | India              | 220.0
===============================================================================
```

### B. Mutation Test 1: Reordered Fields & Random GUID IDs
*Simulates: State dropdown moved to top, Gender reversed in middle, First Name at bottom; all IDs replaced by random GUIDs; classes stripped.*
```text
Field Name                | Status   | Input / Target   | Confirmed Value    | Score 
-----------------------------------------------------------------------------------
First Name (Text Box)     | PASS     | Abhiram          | Abhiram            | 185.0
Gender (Radio Button)     | PASS     | Male             | Male               | 140.0
State (Dropdown)          | PASS     | India            | India              | 175.0
```

### C. Mutation Test 2: Lookalike Fields Disambiguation
*Simulates: Multiple lookalike inputs ("Full Name", "Middle Name", "First Name", "Last Name"), multiple dropdowns ("Country", "State", "City"), multiple radio groups ("Membership", "Gender").*
```text
Field Name                | Status   | Input / Target   | Confirmed Value    | Score 
-----------------------------------------------------------------------------------
First Name (Text Box)     | PASS     | Abhiram          | Abhiram            | 215.0
Gender (Radio Button)     | PASS     | Male             | Male               | 140.0
State (Dropdown)          | PASS     | India            | India              | 175.0
```

### D. Mutation Test 3: Structural Hierarchy Change (HTML Table Layout without IDs)
*Simulates: Complete HTML structural change using legacy table layouts (`<table><tr><td>`), without IDs or Bootstrap classes.*
```text
Field Name                | Status   | Input / Target   | Confirmed Value    | Score 
-----------------------------------------------------------------------------------
First Name (Text Box)     | PASS     | Abhiram          | Abhiram            |  90.0
Gender (Radio Button)     | PASS     | Male             | Male               | 165.0
State (Dropdown)          | PASS     | India            | India              |  95.0
```

---

## 6. Summary

The program achieves complete independence from fragile, hardcoded XPaths or CSS selectors. By employing accessibility standards, contextual DOM proximity analysis, content-aware option inspection, and disambiguation penalties, the automation remains resilient across arbitrary layout and attribute changes.

