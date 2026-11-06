using System;
using System.IO;
using System.Collections.Generic;

namespace ResilientFormAutomation
{
    public static class ResilienceMutations
    {
        public static string GenerateMutationA()
        {
            // Mutation A: Fields in reverse order, IDs scrambled with GUIDs, classes stripped
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mutation_a_reordered.html");
            string html = @"<!DOCTYPE html>
<html>
<head><title>Mutation A: Reordered & Scrambled Attributes</title></head>
<body style='font-family: sans-serif; padding: 20px;'>
  <h2>Employee Registration (Modified Page - Reordered Fields)</h2>
  <form action='#' method='post'>
    
    <!-- 1. State dropdown moved to TOP, ID is random guid, no class -->
    <div style='margin-bottom: 15px;'>
      <label for='uuid_9831_state'>State / Province</label>
      <select id='uuid_9831_state' name='dynamic_state_field'>
        <option value=''>-- Select State --</option>
        <option value='California'>California</option>
        <option value='India'>India</option>
        <option value='Texas'>Texas</option>
        <option value='New York'>New York</option>
      </select>
    </div>

    <!-- 2. Gender in the middle, options reversed, custom wrapper -->
    <div style='margin-bottom: 15px;'>
      <span style='font-weight: bold;'>Select Gender:</span>
      <div>
        <label><input type='radio' name='cust_gender' value='Transgender' id='g_trans'> Transgender</label>
        <label><input type='radio' name='cust_gender' value='Female' id='g_female'> Female</label>
        <label><input type='radio' name='cust_gender' value='Male' id='g_male'> Male</label>
      </div>
    </div>

    <!-- 3. First Name placed at BOTTOM, ID is random guid, name is 'forename' -->
    <div style='margin-bottom: 15px;'>
      <label for='guid_7712_fname'>First Name</label>
      <input type='text' id='guid_7712_fname' name='forename' placeholder='Given name' autocomplete='given-name' />
    </div>

    <button type='submit'>Save Changes</button>
  </form>
</body>
</html>";
            File.WriteAllText(path, html);
            return new Uri(path).AbsoluteUri;
        }

        public static string GenerateMutationB()
        {
            // Mutation B: Lookalike Fields Challenge (First vs Last vs Middle vs Full Name, Country vs State vs City)
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mutation_b_lookalikes.html");
            string html = @"<!DOCTYPE html>
<html>
<head><title>Mutation B: Lookalike Fields Disambiguation</title></head>
<body style='font-family: sans-serif; padding: 20px;'>
  <h2>Complex Profile Form (Lookalikes Challenge)</h2>
  <form action='#' method='post'>
    
    <!-- Multiple similar name text boxes -->
    <div>
      <label for='user_fullname'>Full Name</label>
      <input type='text' id='user_fullname' name='fullName' placeholder='Full Name' />
    </div>
    
    <div>
      <label for='user_middlename'>Middle Name</label>
      <input type='text' id='user_middlename' name='middleName' placeholder='Middle Name' />
    </div>

    <div>
      <!-- The Target: First Name -->
      <label for='user_firstname'>First Name *</label>
      <input type='text' id='user_firstname' name='user_first_name' placeholder='First Name' />
    </div>

    <div>
      <label for='user_lastname'>Last Name *</label>
      <input type='text' id='user_lastname' name='lastName' placeholder='Last Name / Surname' />
    </div>

    <!-- Multiple radio groups -->
    <fieldset>
      <legend>Membership Tier</legend>
      <label><input type='radio' name='tier' value='Free'> Free</label>
      <label><input type='radio' name='tier' value='Pro'> Pro</label>
    </fieldset>

    <fieldset>
      <legend>Gender</legend>
      <label><input type='radio' name='gender_selection' value='Female'> Female</label>
      <label><input type='radio' name='gender_selection' value='Male'> Male</label>
      <label><input type='radio' name='gender_selection' value='Transgender'> Transgender</label>
    </fieldset>

    <!-- Multiple dropdowns: Country vs State vs City -->
    <div>
      <label for='country_select'>Country</label>
      <select id='country_select' name='country'>
        <option>United States</option>
        <option>India</option>
        <option>United Kingdom</option>
      </select>
    </div>

    <div>
      <!-- The Target: State -->
      <label for='state_select'>State / Region</label>
      <select id='state_select' name='state_or_province'>
        <option value=''>-- Choose State --</option>
        <option value='California'>California</option>
        <option value='India'>India</option>
        <option value='Ontario'>Ontario</option>
      </select>
    </div>

    <div>
      <label for='city_select'>City</label>
      <select id='city_select' name='city'>
        <option>San Jose</option>
        <option>Hyderabad</option>
      </select>
    </div>

    <button type='submit'>Register</button>
  </form>
</body>
</html>";
            File.WriteAllText(path, html);
            return new Uri(path).AbsoluteUri;
        }

        public static string GenerateMutationC()
        {
            // Mutation C: Structural Hierarchy Change (Table-based layout without traditional IDs or form-groups)
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mutation_c_table_layout.html");
            string html = @"<!DOCTYPE html>
<html>
<head><title>Mutation C: Table Layout without IDs</title></head>
<body style='font-family: sans-serif; padding: 20px;'>
  <h2>Account Setup (Legacy Table Layout)</h2>
  <form action='#'>
    <table border='0' cellpadding='8'>
      <tr>
        <td><strong>First Name:</strong></td>
        <td><input type='text' name='fn' placeholder='Enter given name here' /></td>
      </tr>
      <tr>
        <td><strong>Gender:</strong></td>
        <td>
          <input type='radio' name='rad_gender' value='Male' /> <span>Male</span> &nbsp;
          <input type='radio' name='rad_gender' value='Female' /> <span>Female</span> &nbsp;
          <input type='radio' name='rad_gender' value='Transgender' /> <span>Transgender</span>
        </td>
      </tr>
      <tr>
        <td><strong>State:</strong></td>
        <td>
          <select name='user_state'>
            <option value=''>Please select your state</option>
            <option value='Texas'>Texas</option>
            <option value='India'>India</option>
            <option value='Washington'>Washington</option>
          </select>
        </td>
      </tr>
    </table>
    <br/>
    <input type='submit' value='Submit Application' />
  </form>
</body>
</html>";
            File.WriteAllText(path, html);
            return new Uri(path).AbsoluteUri;
        }
    }
}

