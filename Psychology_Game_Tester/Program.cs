using CsvHelper;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Psychology_Game_Tester
{
    class Program
    {
        static Regex pathregex = new Regex(@"^(?:[a-zA-Z]\:|\\\\[\w\.]+\\[\w.$]+)\\(?:[\w]+\\)*\w([\w.])+$");
        //Loop: Alexander_Scott_(Dichter)
        static void Main(string[] args)
        {
            Program program = new Program();
            Console.WriteLine("Select the corresponding Number for the specific mode");
            Console.WriteLine("[1] Start with random Article");
            Console.WriteLine("[2] Start with specific Article");
            Console.WriteLine("[3] Start with *.csv File");

            var input = Console.ReadLine();

            if (int.TryParse(input, out int i))
            {
                switch (i)
                {
                    case 1:
                        var (cnt, loop, pagelist, passed) = program.PsychologyGameTester(null);
                        program.DisplayResult(cnt, loop, pagelist);
                        break;
                    case 2:
                        Console.WriteLine("Please enter Startpage");
                        string title = Console.ReadLine();
                        if (!string.IsNullOrEmpty(title))
                            program.PsychologyGameTester(title);
                        break;
                    case 3:
                        Console.WriteLine("Please enter the Path to the CSV File");
                        var path = Console.ReadLine();
                        if (path.StartsWith("\""))
                        {
                            path = path.Substring(1, path.Length - 2);
                        }
                        if (!pathregex.IsMatch(path))
                        {
                            Console.WriteLine("Invalid Path");
                            return;
                        }
                        var items = program.ReadCSVFile(path);
                        foreach (var item in items)
                        {
                            var tmp = program.PsychologyGameTester(item.Title);
                            item.CntPages = tmp.cnt;
                            item.LoopDetected = tmp.loop;
                            item.Path = tmp.pagelist;
                            item.Passed = tmp.passed;
                        }
                        program.WriteCSVFile(path, items);
                        break;
                    default:
                        Console.WriteLine("Unrecoginzed Input");
                        break;
                }
            }
            Console.ReadKey();
        }

        public void DisplayResult(int i, bool l, string s)
        {
            if (!l)
                Console.WriteLine($"Anzahl durchläufe:{i} \r\n Folgende Seiten wurden besucht: \r\n {s}");
            else
                Console.WriteLine($"Loop detected after {i}");
        }

        string FinishArticleText = "Philosophie";
        Regex bracketRegex = new Regex(@"[\(\[](.*?)[\)\]]");
        //  [\(\[](.*?)[\)\]]
        List<string> pageTitles = new List<string>();
        int maxPageCount = 100;
        public (int cnt, bool loop, string pagelist, bool passed) PsychologyGameTester(string startpage)
        {
            pageTitles = new List<string>();
            using (IWebDriver driver = new FirefoxDriver())
            {
                int i = 0;
                if (i > maxPageCount)
                    return (i, false, string.Join(" => ", pageTitles), false);

                if (string.IsNullOrEmpty(startpage.Trim()))
                {
                    driver.Navigate().GoToUrl("http://de.wikipedia.org");
                    IWebElement randomArt = driver.FindElement(By.Id("n-randompage")).FindElement(By.TagName("a"));
                    randomArt.Click();
                }
                else
                {
                    driver.Navigate().GoToUrl("http://de.wikipedia.org/wiki/" + startpage);
                }

                while (driver.FindElement(By.Id("firstHeading")).Text != FinishArticleText)
                {
                    i++;


                    if (pageTitles.Count(x => x == driver.FindElement(By.Id("firstHeading")).Text) > 0)
                    {
                        //Console.WriteLine($"Loop detected after {i} with pageTitle {driver.FindElement(By.Id("firstHeading")).Text}");
                        return (i, true, string.Join(" => ", pageTitles), false);
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

                            if (link.Text == "" 
                                || IsElementInTable(link) 
                                || parent.GetAttribute("className").Contains("thumb") 
                                || parent.GetAttribute("className").Contains("IPA") 
                                || parent.GetAttribute("className").Contains("internal") 
                                || link.GetAttribute("className").Contains("internal")
                                || parent.TagName == "sup"
                                || parent.GetAttribute("className").Contains("IPA")
                                || link.GetAttribute("href").Contains("ogg"))
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
                return (i, false, string.Join(" => ", pageTitles), true);
             //   Console.WriteLine($"Anzahl durchläufe:{i} \r\n Folgende Seiten wurden besucht: \r\n {string.Join(" => ", pageTitles)}");
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

        public List<CSVEntry> ReadCSVFile(string path)
        {

            List<CSVEntry> retVal = new List<CSVEntry>();

            using (TextReader reader = new StreamReader(path))
            {
                var csv = new CsvReader(reader);
                csv.Configuration.HeaderValidated = null;
                csv.Configuration.MissingFieldFound = null;
                csv.Configuration.Delimiter = ";";
                return csv.GetRecords<CSVEntry>().ToList();
            }
        }
        public void WriteCSVFile(string path, List<CSVEntry> result)
        {
            using (TextWriter writer = new StreamWriter(path))
            {
                var csv = new CsvWriter(writer);
                csv.Configuration.Delimiter = ";";
                csv.WriteRecords(result);
            }

        }
    }
    public class CSVEntry
    {
        public string Title { get; set; }
        public bool? Passed { get; set; }
        public int? CntPages { get; set; }
        public bool? LoopDetected { get; set; }
        public string Path { get; set; }
    }
}
