using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Psychology_Game_Tester
{
    class Program
    {
        //Loop: Alexander_Scott_(Dichter)
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter Startpage or leave Empty for random article");
            string title = Console.ReadLine();
            new Program().PsychologyGameTester(title);
            Console.ReadKey();
        }

        string FinishArticleText = "Philosophie";
        Regex bracketRegex = new Regex(@"[\(\[](.*?)[\)\]]");
        List<string> pageTitles = new List<string>();
        public void PsychologyGameTester(string startpage)
        {
            IWebDriver driver = new FirefoxDriver();
            int i = 0;

            if (string.IsNullOrEmpty(startpage.Trim()))
            {
                driver.Navigate().GoToUrl("http://de.wikipedia.org");
                IWebElement randomArt = driver.FindElement(By.Id("n-randompage")).FindElement(By.TagName("a"));
                randomArt.Click();
            }
            else
            {
                driver.Navigate().GoToUrl("http://de.wikipedia.org/wiki/"+ startpage);
            }

            while (driver.FindElement(By.Id("firstHeading")).Text != FinishArticleText)
            {
                i++;
                if (pageTitles.Count(x => x == driver.FindElement(By.Id("firstHeading")).Text) > 0)
                {
                    Console.WriteLine($"Loop detected after {i} with pageTitle {driver.FindElement(By.Id("firstHeading")).Text}");
                    break;
                }
                else
                {
                    pageTitles.Add(driver.FindElement(By.Id("firstHeading")).Text);
                }

                IWebElement content = driver.FindElement(By.Id("mw-content-text"));
                bool breakloob = false;
                foreach (IWebElement pcontent in content.FindElements(By.TagName("p")))
                {
                    foreach (IWebElement link in pcontent.FindElements(By.TagName("a")))
                    {
                        IWebElement parent = link;
                        do
                        {
                            parent = GetParent(parent);
                        } while (parent.Text == link.Text && link.Text != "");

                        if (link.Text == "" || IsElementInTable(link) || parent.GetAttribute("className").Contains("thumb") || parent.GetAttribute("className").Contains("IPA") || parent.GetAttribute("className").Contains("internal"))
                            continue;


                        bool isInBrackets = false;
                        foreach (Match match in bracketRegex.Matches(parent.Text))
                        {
                            if (match.Value.Contains(link.Text))
                            {
                                isInBrackets = true;
                                break;
                            }
                        }
                        if (isInBrackets)
                            continue;

                        link.Click();
                        breakloob = true;
                        break;
                    }
                    if (breakloob)
                        break;
                }
            }
            Console.WriteLine($"Anzahl durchläufe:{i} \r\n Folgende Seiten wurden besucht: \r\n {string.Join(" => ", pageTitles)}");
        }
        public bool IsElementInTable(IWebElement e)
        {
            IWebElement tmp = e;
            while (tmp != null)
            {
                if (tmp.TagName == "td")
                    return true;
                else if (tmp.TagName == "body")
                    return false;
                else
                {
                    tmp = tmp.FindElement(By.XPath(".."));
                }
            }
            return false;
        }
        public IWebElement GetParent(IWebElement e)
        {
            return e.FindElement(By.XPath(".."));
        }
    }
}
