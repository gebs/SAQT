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
        static void Main(string[] args)
        {
            new Program().PsychologyGameTester();
            Console.ReadKey();
        }

        string FinishArticleText = "Philosophie";
        Regex bracketRegex = new Regex(@"[\(\[](.*?)[\)\]]");
        public void PsychologyGameTester()
        {
            using (IWebDriver driver = new FirefoxDriver())
            {
                driver.Navigate().GoToUrl("http://de.wikipedia.org");
                IWebElement randomArt = driver.FindElement(By.Id("n-randompage")).FindElement(By.TagName("a"));
                randomArt.Click();

                while (driver.FindElement(By.Id("firstHeading")).Text != FinishArticleText)
                {

                    IWebElement content = driver.FindElement(By.Id("mw-content-text"));
                    bool breakloob = false;
                    foreach (IWebElement pcontent in content.FindElements(By.TagName("p"))){
                        foreach (IWebElement link in pcontent.FindElements(By.TagName("a")))
                        {
                            IWebElement parent = link;
                            do
                            {
                                parent = GetParent(parent);
                            } while (parent.Text == link.Text && link.Text != "");

                            if (link.Text == "" || IsElementInTable(link) || parent.GetAttribute("className").Contains("thumb") || parent.GetAttribute("className").Contains("IPA"))
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
            }
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
