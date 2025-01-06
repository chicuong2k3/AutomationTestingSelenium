using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System.Text.Json;
using Xunit;


namespace TestManagementAutomationTest
{
    internal class Program
    {
        private static int passedTests = 0;
        private static int failedTests = 0;
        static void Main(string[] args)
        {
            var browsers = new List<string> { "Firefox", "Chrome", "Edge" };

            foreach (var browser in browsers)
            {
                Console.WriteLine($"Running tests on {browser}...");
                IWebDriver driver = GetWebDriver(browser);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);

                RunLoginTests(driver);
                RunViewTestCaseTests(driver);
                RunAddTestPlanTests(driver);

                Console.WriteLine($"Total test cases: {passedTests + failedTests}");
                Console.WriteLine($"Passed: {passedTests}");
                Console.WriteLine($"Failed: {failedTests}");

                passedTests = 0;
                failedTests = 0;
                driver.Quit();
            }




        }

        private static IWebDriver GetWebDriver(string browser)
        {
            return browser switch
            {
                "Chrome" => new ChromeDriver(),
                "Firefox" => new FirefoxDriver(),
                "Edge" => new EdgeDriver(),
                _ => throw new ArgumentException("Unsupported browser")
            };
        }

        private static List<T> LoadTestCases<T>(string filePath)
        {
            try
            {
                var jsonData = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new DateTimeConverter() }
                };

                return JsonSerializer.Deserialize<List<T>>(jsonData, options) ?? [];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading test data: {ex.Message}");
                return [];
            }
        }
        private static void RunLoginTests(IWebDriver driver)
        {
            string jsonFilePath = "login-data.json";
            var testCases = LoadTestCases<LoginTestData>(jsonFilePath);

            try
            {
                string baseUrl = "http://localhost:4000";
                driver.Navigate().GoToUrl(baseUrl);

                foreach (var testCase in testCases)
                {
                    RunLoginTest(driver, testCase.Email, testCase.Password);
                    driver.Navigate().Refresh();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running login tests: {ex.Message}");
            }
        }
        private static void RunLoginTest(IWebDriver driver, string email, string password)
        {
            var emailField = driver.FindElement(By.Id("fEmail"));
            emailField.Clear();
            emailField.SendKeys(email);

            var passwordField = driver.FindElement(By.Id("fPassword"));
            passwordField.Clear();
            passwordField.SendKeys(password);

            var loginButton = driver.FindElement(By.XPath("/html/body/main/main/div/div[2]/div/form/div[3]/button"));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", loginButton);

            Thread.Sleep(500);

            string currentUrl = driver.Url;

            if (currentUrl.Contains("dashboard"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Test Passed (Login Successfully) for Email: '{email}', Password: '{password}'");
                Console.ResetColor();
                passedTests++;
            }
            else
            {
                var error = string.Empty;
                try
                {
                    error = driver.FindElement(By.ClassName("alert-danger")).Text;
                }
                catch (NoSuchElementException)
                {
                    try
                    {
                        var inputs = driver.FindElements(By.ClassName("invalid-feedback"));
                        foreach (var input in inputs)
                        {
                            if (!string.IsNullOrEmpty(input.Text))
                            {
                                error += input.Text + " ";
                            }
                        }
                    }
                    catch (NoSuchElementException)
                    {

                    }
                }

                if (!string.IsNullOrEmpty(error))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Test Passed for Email: '{email}', Password: '{password}'. Displayed Error: {error}");
                    Console.ResetColor();
                    passedTests++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Test Failed for Email: '{email}', Password: '{password}'. No error message found.");
                    Console.ResetColor();
                    failedTests++;
                }
            }

            Thread.Sleep(500);
        }

        private static void Login(IWebDriver driver, string email, string password)
        {
            driver.Navigate().GoToUrl("http://localhost:4000/login");

            var emailField = driver.FindElement(By.Id("fEmail"));
            emailField.Clear();
            emailField.SendKeys(email);
            var passwordField = driver.FindElement(By.Id("fPassword"));
            passwordField.Clear();
            passwordField.SendKeys(password);
            var loginButton = driver.FindElement(By.XPath("/html/body/main/main/div/div[2]/div/form/div[3]/button"));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", loginButton);

            Thread.Sleep(500);
        }
        private static void RunViewTestCaseTests(IWebDriver driver)
        {
            string jsonFilePath = "view-tc-list-data.json";
            var testCases = LoadTestCases<ViewTestCaseTestData>(jsonFilePath);


            try
            {
                Login(driver, "admin1@mail.com", "123456");

                foreach (var testCase in testCases)
                {
                    RunViewTestCasesTest(driver, testCase);
                    driver.Navigate().Refresh();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running view test case tests: {ex.Message}");
            }
        }

        private static void RunViewTestCasesTest(IWebDriver driver, ViewTestCaseTestData testCase)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(testCase.Keyword))
            {
                queryParams.Add($"testCaseKeyword={Uri.EscapeDataString(testCase.Keyword)}");
            }

            if (!string.IsNullOrEmpty(testCase.SortField))
            {
                queryParams.Add($"sortField={Uri.EscapeDataString(testCase.SortField)}");
            }

            if (!string.IsNullOrEmpty(testCase.SortOrder))
            {
                queryParams.Add($"sortOrder={Uri.EscapeDataString(testCase.SortOrder)}");
            }

            queryParams.Add($"page={testCase.Page}");

            string baseUrl = "http://localhost:4000/project/66fa0056e0d5ccc14eab0a2e/test-case";
            string queryString = string.Join("&", queryParams);
            string urlWithQuery = $"{baseUrl}?{queryString}";

            driver.Navigate().GoToUrl(urlWithQuery);

            var tableElement = driver.FindElement(By.CssSelector("table.table"));
            var rows = tableElement.FindElements(By.CssSelector("tbody tr"));
            int MAX_PAGE = 2;
            try
            {
                if (string.IsNullOrEmpty(testCase.Keyword))
                {
                    Assert.True(rows.Count > 0);
                }
                else if (testCase.Keyword == "verify user")
                {
                    // Kiểm tra khi keyword khớp với tiêu đề
                    foreach (var row in rows)
                    {
                        var title = row.FindElement(By.CssSelector("td:nth-child(3)")).Text;
                        Assert.True(title.Contains("verify user", StringComparison.OrdinalIgnoreCase));
                    }
                }
                else if (testCase.Keyword == "remove user")
                {
                    // Kiểm tra khi keyword không khớp với bất kỳ tiêu đề nào
                    Assert.True(rows.Count == 0);
                }

                // Kiểm tra Page
                if (testCase.Page < 1 || testCase.Page > MAX_PAGE)
                {
                    // Trang nằm ngoài giới hạn, phải hiển thị trang đầu tiên
                    Assert.True(driver.Url.Contains("page=1"));
                }
                else
                {
                    Assert.True(driver.Url.Contains($"page={testCase.Page}"));
                }

                // Kiểm tra Sort Field
                if (testCase.SortField == "title" || testCase.SortField == "created-date" || testCase.SortField == "case-code")
                {
                    Assert.True(driver.Url.Contains($"sortField={testCase.SortField}"));

                    List<string> columnData = new List<string>();

                    if (testCase.SortField == "title")
                    {
                        columnData = rows.Select(row => row.FindElement(By.CssSelector("td:nth-child(3)")).Text).ToList();
                    }
                    else if (testCase.SortField == "created-date")
                    {
                        columnData = rows.Select(row => row.FindElement(By.CssSelector("td:nth-child(4)")).Text).ToList();
                    }
                    else if (testCase.SortField == "case-code")
                    {
                        columnData = rows.Select(row => row.FindElement(By.CssSelector("td:nth-child(1)")).Text).ToList();
                    }

                    var sortedData = new List<string>(columnData);


                    if (testCase.SortOrder == "asc")
                    {
                        sortedData.Sort(StringComparer.OrdinalIgnoreCase);
                    }
                    else if (testCase.SortOrder == "desc")
                    {
                        sortedData.Sort(StringComparer.OrdinalIgnoreCase);
                        sortedData.Reverse();
                    }

                    Assert.True(columnData.SequenceEqual(sortedData));
                }
                else
                {
                    // Trường hợp không hợp lệ, mặc định về created-date
                    Assert.True(driver.Url.Contains("sortField=created-date"));
                }

                // Kiểm tra Sort Order
                var uri = new Uri(driver.Url);
                var qParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var sortOrderValue = qParams.Get("sortOrder");
                if (testCase.SortOrder == "asc" || testCase.SortOrder == "desc")
                {
                    var titles = rows.Select(row => row.FindElement(By.CssSelector("td:nth-child(3)")).Text).ToList();

                    var sortedTitles = new List<string>(titles);

                    if (testCase.SortOrder == "asc")
                    {
                        sortedTitles.Sort(StringComparer.OrdinalIgnoreCase);
                    }
                    else if (testCase.SortOrder == "desc")
                    {
                        sortedTitles.Sort(StringComparer.OrdinalIgnoreCase);
                        sortedTitles.Reverse();
                    }

                    Assert.True(titles.SequenceEqual(sortedTitles));
                }
                else
                {
                    Assert.True(sortOrderValue == "asc");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Test Passed: Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}");
                Console.ResetColor();
                passedTests++;
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Test Failed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}");
                Console.ResetColor();
                failedTests++;
            }

            Thread.Sleep(500);
        }

        private static void RunAddTestPlanTests(IWebDriver driver)
        {
            string jsonFilePath = "add-test-plan-data.json";
            var testCases = LoadTestCases<AddTestPlanData>(jsonFilePath);

            Login(driver, "admin1@mail.com", "123456");

            string baseUrl = "http://localhost:4000/project/66fa0056e0d5ccc14eab0a2e/test-plan";

            foreach (var testCase in testCases)
            {
                driver.Navigate().GoToUrl(baseUrl);

                var addButton = driver.FindElement(By.XPath("//button[@data-bs-target='#addTestPlan']"));
                addButton.Click();

                var testPlanNameField = driver.FindElement(By.Id("name"));
                testPlanNameField.Clear();
                testPlanNameField.SendKeys(testCase.TestPlanName);

                var releaseIdDropdown = driver.FindElement(By.Id("releaseId"));
                var selectReleaseId = new SelectElement(releaseIdDropdown);

                try
                {
                    selectReleaseId.SelectByValue(testCase.ReleaseId);
                }
                catch (NoSuchElementException)
                {
                    continue;
                }

                var startDateField = driver.FindElement(By.Id("startDate"));
                startDateField.Clear();
                startDateField.SendKeys(testCase.FromDate.ToString("MM/dd/yyyy"));

                var endDateField = driver.FindElement(By.Id("endDate"));
                endDateField.Clear();
                endDateField.SendKeys(testCase.ToDate.ToString("MM/dd/yyyy"));

                var descriptionField = driver.FindElement(By.Id("description"));
                descriptionField.Clear();
                descriptionField.SendKeys(testCase.Description);

                var submitButton = driver.FindElement(By.XPath("//button[@type='submit']"));
                submitButton.Click();

                // Kiểm tra TestPlanName rỗng
                if (string.IsNullOrEmpty(testCase.TestPlanName))
                {
                    try
                    {
                        var validationMessage = (string)((IJavaScriptExecutor)driver).ExecuteScript(
                            "let field = arguments[0]; field.reportValidity(); return field.validationMessage;", testPlanNameField);

                        Assert.True(!string.IsNullOrEmpty(validationMessage));

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Test Passed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}.");
                        Console.ResetColor();
                        passedTests++;
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Test Failed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}");
                        Console.ResetColor();
                        failedTests++;
                    }
                    finally
                    {
                        try
                        {
                            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                            wait.Until(driver =>
                            {
                                try
                                {
                                    IAlert alert = driver.SwitchTo().Alert();
                                    Console.ForegroundColor = ConsoleColor.Green;

                                    alert.Accept(); // Đóng alert
                                    return true;
                                }
                                catch (NoAlertPresentException)
                                {
                                    return false;
                                }
                            });

                            Console.ResetColor();
                        }
                        catch (WebDriverTimeoutException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unexpected error while handling alert: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }
                // Kiểm tra TestPlanName dài hơn 250 ký tự
                else if (testCase.TestPlanName.Length > 250)
                {
                    try
                    {
                        var validationMessage = (string)((IJavaScriptExecutor)driver).ExecuteScript(
                            "let field = arguments[0]; field.reportValidity(); return field.validationMessage;", testPlanNameField);

                        Assert.True(!string.IsNullOrEmpty(validationMessage));

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Test Passed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}.");
                        Console.ResetColor();

                        passedTests++;
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Test Failed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}");
                        Console.ResetColor();
                        failedTests++;
                    }
                    finally
                    {
                        try
                        {
                            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                            wait.Until(driver =>
                            {
                                try
                                {
                                    IAlert alert = driver.SwitchTo().Alert();
                                    Console.ForegroundColor = ConsoleColor.Green;

                                    alert.Accept(); // Đóng alert
                                    return true;
                                }
                                catch (NoAlertPresentException)
                                {
                                    return false;
                                }
                            });

                            Console.ResetColor();
                        }
                        catch (WebDriverTimeoutException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unexpected error while handling alert: {ex.Message}");
                            Console.ResetColor();
                        }

                    }
                }
                // Kiểm tra ReleaseId rỗng
                else if (string.IsNullOrEmpty(testCase.ReleaseId))
                {
                    try
                    {
                        var validationMessage = (string)((IJavaScriptExecutor)driver).ExecuteScript("let field = arguments[0]; field.reportValidity(); return field.validationMessage;", releaseIdDropdown);

                        Assert.True(!string.IsNullOrEmpty(validationMessage));

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Test Passed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}.");
                        Console.ResetColor();
                        passedTests++;
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Test Failed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}");
                        Console.ResetColor();
                        failedTests++;
                    }
                    finally
                    {
                        try
                        {
                            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                            wait.Until(driver =>
                            {
                                try
                                {
                                    IAlert alert = driver.SwitchTo().Alert();
                                    Console.ForegroundColor = ConsoleColor.Green;

                                    alert.Accept(); // Đóng alert
                                    return true;
                                }
                                catch (NoAlertPresentException)
                                {
                                    return false;
                                }
                            });

                            Console.ResetColor();
                        }
                        catch (WebDriverTimeoutException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unexpected error while handling alert: {ex.Message}");
                            Console.ResetColor();
                        }
                    }

                }
                // Kiểm tra FromDate > ToDate
                else if (testCase.FromDate > testCase.ToDate)
                {
                    Thread.Sleep(1500);
                    try
                    {
                        var sDateField = driver.FindElement(By.Id("startDate"));

                        string classAttribute = sDateField.GetDomAttribute("class");

                        Assert.True(classAttribute.Contains("is-invalid"));

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Test Passed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}.");
                        Console.ResetColor();
                        passedTests++;
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Test Failed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}");
                        Console.ResetColor();
                        failedTests++;
                    }
                    finally
                    {
                        try
                        {
                            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                            wait.Until(driver =>
                            {
                                try
                                {
                                    IAlert alert = driver.SwitchTo().Alert();
                                    Console.ForegroundColor = ConsoleColor.Green;

                                    alert.Accept(); // Đóng alert
                                    return true;
                                }
                                catch (NoAlertPresentException)
                                {
                                    return false;
                                }
                            });

                            Console.ResetColor();
                        }
                        catch (WebDriverTimeoutException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unexpected error while handling alert: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }
                // Kiểm tra Description dài hơn 500 ký tự
                else if (testCase.Description != null && testCase.Description.Length > 500)
                {
                    try
                    {
                        // Description dài hơn 500 ký tự phải báo lỗi
                        var validationMessage = (string)((IJavaScriptExecutor)driver).ExecuteScript(
                            "let field = arguments[0]; field.reportValidity(); return field.validationMessage;", descriptionField);

                        Assert.True(validationMessage.Contains("Description không được vượt quá 500 ký tự."));

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Test Passed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}.");
                        Console.ResetColor();
                        passedTests++;
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Test Failed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}");
                        Console.ResetColor();
                        failedTests++;
                    }
                    finally
                    {
                        try
                        {
                            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                            wait.Until(driver =>
                            {
                                try
                                {
                                    IAlert alert = driver.SwitchTo().Alert();
                                    Console.ForegroundColor = ConsoleColor.Green;

                                    alert.Accept(); // Đóng alert
                                    return true;
                                }
                                catch (NoAlertPresentException)
                                {
                                    return false;
                                }
                            });

                            Console.ResetColor();
                        }
                        catch (WebDriverTimeoutException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unexpected error while handling alert: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }
                // Tất cả điều kiện hợp lệ
                else
                {
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

                    bool isAlertPresent = wait.Until(driver =>
                    {
                        try
                        {
                            driver.SwitchTo().Alert();
                            return true;
                        }
                        catch (NoAlertPresentException)
                        {
                            return false;
                        }
                    });

                    try
                    {
                        Assert.True(isAlertPresent);


                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Test Passed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}");
                        Console.ResetColor();
                        passedTests++;
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Test Failed. Test Case: {JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}");
                        Console.ResetColor();
                        failedTests++;
                    }
                    finally
                    {
                        try
                        {
                            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                            wait.Until(driver =>
                            {
                                try
                                {
                                    IAlert alert = driver.SwitchTo().Alert();
                                    Console.ForegroundColor = ConsoleColor.Green;

                                    alert.Accept(); // Đóng alert
                                    return true;
                                }
                                catch (NoAlertPresentException)
                                {
                                    return false;
                                }
                            });

                            Console.ResetColor();
                        }
                        catch (WebDriverTimeoutException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unexpected error while handling alert: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }


                Thread.Sleep(500);

            }

        }


    }
}
