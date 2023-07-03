using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;

namespace Gambot
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("How much do you want to bet?");
            var input = Console.ReadLine();
            var amount = 10;
            try
            {
                amount = Int32.Parse(input);
                if (amount <= 0) { amount = 10; }
            }
            catch {}

            EasyOddsDraw easyOddsDraw = new EasyOddsDraw();
            EasyOddsNoDraw easyOddsNoDraw = new EasyOddsNoDraw();
            OddsCheckerDraw oddsCheckerDraw = new OddsCheckerDraw();
            
            FindMatches(easyOddsDraw, "/football", amount);            
            FindMatches(easyOddsNoDraw, "/tennis", amount);
            FindMatches(easyOddsNoDraw, "/cricket", amount);
            FindMatches(easyOddsDraw, "/rugby-union", amount);
            FindMatches(easyOddsDraw, "/rugby-league", amount);
            FindMatches(easyOddsNoDraw, "/us-football", amount);
            FindMatches(easyOddsNoDraw, "/baseball", amount);
            FindMatches(easyOddsNoDraw, "/aussie-rules", amount);
            //FindMatches(oddsCheckerDraw, "/football", amount);

            System.Console.WriteLine("Check complete");
            Console.ReadLine();
        }

        static void FindMatches(WebPageHandler wbh, string reference, int amount)
        {
            var matchDetails = wbh.GetMatchDetails(reference);
            foreach(var match in matchDetails)
            {
                match.Test(amount);
            }
            System.Console.WriteLine();
        }
    }

    public abstract class WebPageHandler
    {
        protected ScrapingBrowser _scrapingBrowser = new ScrapingBrowser();

        public abstract List<MatchDetails> GetMatchDetails(string reference);

        protected HtmlNode GetHtml(string url)
        {
            FakeUserAgent test = new FakeUserAgent("Opera/9.80 (X11; Linux i686; U; ru) Presto/2.8.131 Version/11.11", "Opera/9.80 (X11; Linux i686; U; ru) Presto/2.8.131 Version/11.11");
            _scrapingBrowser.UserAgent = test;
            WebPage webPage = _scrapingBrowser.NavigateToPage(new Uri(url));
            return webPage.Html;
        }

        protected static double FractionToDouble(string fraction) {
            double result;

            if(double.TryParse(fraction, out result)) {
                return result;
            }

            string[] split = fraction.Split(new char[] { ' ', '/' });

            if(split.Length == 2 || split.Length == 3) {
                int a, b;

                if(int.TryParse(split[0], out a) && int.TryParse(split[1], out b))
                {
                    if(split.Length == 2) {
                        return (double)a / b;
                    }

                    int c;

                    if(int.TryParse(split[2], out c)) {
                        return a + (double)b / c;
                    }
                }
            }

            throw new FormatException("Not a valid fraction.");
        }
    }

    public abstract class OddsChecker : WebPageHandler
    {
        protected string url = "https://oddschecker.com";
        protected List<String> GetMainPageLinks(string url)
        {
            var homePageLinks = new List<String>();
            var html = GetHtml(url);
            var links = html.CssSelect("a");
            foreach (var link in links)
            {
                if(link.Attributes["href"].Value.Contains("-v-"))
                {
                    homePageLinks.Add(link.Attributes["href"].Value);
                }
            }
            return homePageLinks;
        }
    }

    public abstract class EasyOdds : WebPageHandler
    {
        protected string url = "https://easyodds.com";
        protected List<String> GetMainPageLinks(string url)
        {
            var homePageLinks = new List<String>();
            var html = GetHtml(url);
            var links = html.CssSelect("a");
            foreach (var link in links)
            {
                if(link.Attributes["href"].Value.Contains("-v-"))
                {
                    homePageLinks.Add(link.Attributes["href"].Value);
                }
            }
            return homePageLinks;
        }
    }

    public class EasyOddsDraw : EasyOdds
    {
        public override List<MatchDetails> GetMatchDetails(string reference)
        {
            System.Console.WriteLine("Checking " + reference + " on Easy Odds");
            var urls = GetMainPageLinks(url + reference);
            var lstMatchDetails = new List<MatchDetails>();
            foreach(var matchUrl in urls)
            {
                var htmlNode = GetHtml(matchUrl);
                var matchDetails = new MatchDrawDetails();
                matchDetails.Title = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/head/title").InnerText;
                matchDetails.Url = matchUrl;
                try
                {
                    matchDetails.Date = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[5]/div/div/header/div[2]/div/p").InnerText;
                }
                catch {}
                if (TryLayout(matchDetails, htmlNode, 1, matchUrl))
                {
                    lstMatchDetails.Add(matchDetails);
                }
            }
            return lstMatchDetails;
        }

        private Boolean TryLayout(MatchDrawDetails matchDetails, HtmlNode htmlNode, int variation, string matchUrl)
        {
            if (variation == 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("Could not handle " + matchUrl);
                Console.ForegroundColor = ConsoleColor.Gray;
                return false;
            }
            else
            {
                try
                {
                    var team1 = "";
                    var draw = "";
                    var team2 = "";
                    if (variation == 1)
                    {
                        team1 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[6]/div[3]/div[1]/div[2]/div/div[1]/div[1]/div[5]/div[1]/a").InnerText.Replace(" ", "");
                        draw = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[6]/div[3]/div[1]/div[2]/div/div[2]/div[1]/div[5]/div[1]/a").InnerText.Replace(" ", "");
                        team2 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[6]/div[3]/div[1]/div[2]/div/div[3]/div[1]/div[5]/div[1]/a").InnerText.Replace(" ", "");
                    }
                    matchDetails.Team1 = FractionToDouble(team1);
                    matchDetails.Draw = FractionToDouble(draw);
                    matchDetails.Team2 = FractionToDouble(team2);
                    return true;
                }
                catch (System.Exception)
                {
                    return TryLayout(matchDetails, htmlNode, variation + 1, matchUrl);
                }
            }
        }
    }

    public class EasyOddsNoDraw : EasyOdds
    {
        public override List<MatchDetails> GetMatchDetails(string reference)
        {
            System.Console.WriteLine("Checking " + reference + " on Easy Odds");
            var urls = GetMainPageLinks(url + reference);
            var lstMatchDetails = new List<MatchDetails>();
            foreach(var matchUrl in urls)
            {
                var htmlNode = GetHtml(matchUrl);
                var matchDetails = new MatchNoDrawDetails();
                matchDetails.Title = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/head/title").InnerText;
                matchDetails.Url = matchUrl;
                try
                {
                    matchDetails.Date = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[5]/div/div/header/div[2]/div/p").InnerText;
                }
                catch {}
                if (TryLayout(matchDetails, htmlNode, 1, matchUrl))
                {
                    lstMatchDetails.Add(matchDetails);
                }
            }
            return lstMatchDetails;
        }

        private Boolean TryLayout(MatchNoDrawDetails matchDetails, HtmlNode htmlNode, int variation, string matchUrl)
        {
            if (variation == 4)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("Could not handle " + matchUrl);
                Console.ForegroundColor = ConsoleColor.Gray;
                return false;
            }
            else
            {
                try
                {
                    var team1 = "";
                    var team2 = "";
                    if (variation == 1)
                    {
                        team1 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[6]/div[3]/div[1]/div[2]/div/div[1]/div[1]/div[5]/div[1]/a").InnerText.Replace(" ", "");
                        team2 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[6]/div[3]/div[1]/div[2]/div/div[2]/div[1]/div[5]/div[1]/a").InnerText.Replace(" ", "");
                    }
                    else if (variation == 2)
                    {
                        team1 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[3]/div[3]/div[1]/div[1]/div[2]/div/div/span/a/span[2]/span[1]").InnerText.Replace(" ", "");
                        team2 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[3]/div[3]/div[1]/div[1]/div[2]/div/div/span/a/span[3]/span[1]").InnerText.Replace(" ", "");

                    }
                    else if (variation == 3)
                    {
                        team1 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[6]/div[3]/div[1]/div[1]/div/div[1]/div[1]/div[5]/div/a").InnerText.Replace(" ", "");
                        team2 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/div[11]/div[6]/div[3]/div[1]/div[1]/div/div[2]/div[1]/div[5]/div/a").InnerText.Replace(" ", "");
                    }
                    matchDetails.Team1 = FractionToDouble(team1);
                    matchDetails.Team2 = FractionToDouble(team2);
                    return true;
                }
                catch (System.Exception)
                {
                    return TryLayout(matchDetails, htmlNode, variation + 1, matchUrl);
                }
            }
        }
    }

    public class OddsCheckerDraw : OddsChecker
    {
        public override List<MatchDetails> GetMatchDetails(string reference)
        {
            System.Console.WriteLine("Checking " + reference + " on Easy Odds");
            var urls = GetMainPageLinks(url + reference);
            var lstMatchDetails = new List<MatchDetails>();
            foreach(var matchUrl in urls)
            {
                var htmlNode = GetHtml(matchUrl);
                var matchDetails = new MatchDrawDetails();
                matchDetails.Title = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/head/title").InnerText;
                if (TryLayout(matchDetails, htmlNode, 1, matchUrl))
                {
                    lstMatchDetails.Add(matchDetails);
                }
            }
            return lstMatchDetails;
        }

        private Boolean TryLayout(MatchDrawDetails matchDetails, HtmlNode htmlNode, int variation, string matchUrl)
        {
            if (variation == 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("Could not handle " + matchUrl);
                Console.ForegroundColor = ConsoleColor.Gray;
                return false;
            }
            else
            {
                try
                {
                    var team1 = "";
                    var draw = "";
                    var team2 = "";
                    if (variation == 1)
                    {
                        team1 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/main/div/div[4]/div/section/section/div[1]/article[1]/section[1]/div/div/div/div[1]/div[1]/button").InnerText.Replace(" ", "");
                        draw = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/main/div/div[4]/div/section/section/div[1]/article[1]/section[1]/div/div/div/div[1]/div[2]/button").InnerText.Replace(" ", "");
                        team2 = htmlNode.OwnerDocument.DocumentNode.SelectSingleNode("//html/body/main/div/div[4]/div/section/section/div[1]/article[1]/section[1]/div/div/div/div[1]/div[3]/button").InnerText.Replace(" ", "");
                    }
                    matchDetails.Team1 = FractionToDouble(team1);
                    matchDetails.Draw = FractionToDouble(draw);
                    matchDetails.Team2 = FractionToDouble(team2);
                    return true;
                }
                catch (System.Exception)
                {
                    return TryLayout(matchDetails, htmlNode, variation + 1, matchUrl);
                }
            }
        }
    }

    public abstract class MatchDetails
    {
        public string Title = "";
        public string Url = "";
        public double Team1 = 0;
        public double Team2 = 0;
        public string Date = "Couldn't find date";

        protected static double FractionToDouble(string fraction) {
            double result;

            if(double.TryParse(fraction, out result)) {
                return result;
            }

            string[] split = fraction.Split(new char[] { ' ', '/' });

            if(split.Length == 2 || split.Length == 3) {
                int a, b;

                if(int.TryParse(split[0], out a) && int.TryParse(split[1], out b))
                {
                    if(split.Length == 2) {
                        return (double)a / b;
                    }

                    int c;

                    if(int.TryParse(split[2], out c)) {
                        return a + (double)b / c;
                    }
                }
            }

            throw new FormatException("Not a valid fraction.");
        }

        public abstract void Test(int amount);
    }

    public class MatchDrawDetails : MatchDetails
    {
        public double Draw = 0;

        public override void Test(int amount)
        {
            double[] spread = new Double[] { 1.0, Team1 / Draw, Team1 / Team2};
            double total = spread[0] + spread[1] + spread[2];
            var prof1 = ((Team1 + 1) * spread[0]) - total;
            var profd = ((Draw + 1) * spread[1]) - total;
            var prof2 = ((Team2 + 1) * spread[2]) - total;
            var min = prof1;
            if (profd < min) { min = profd; }
            if (prof2 < min) { min = prof2; }

            if (min > 0) // TODO GET RID OF OR TRUE
            {
                Console.WriteLine(Title);
                Console.WriteLine("Team 1 - " + (spread[0] * amount / total));
                Console.WriteLine("Draw - " + (spread[1] * amount / total));
                Console.WriteLine("Team 2 - " + (spread[2] * amount / total));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Profit - " + (min * amount / total));
                Console.ForegroundColor = ConsoleColor.Gray;
                System.Console.WriteLine(Date);
                System.Console.WriteLine(Url);
                Console.WriteLine();
            }
        }
    }

    public class MatchNoDrawDetails : MatchDetails
    {

        public override void Test(int amount)
        {
            double[] spread = new Double[] {1, Team1 / Team2 };
            var total = spread[0] + spread[1];
            var prof1 = ((Team1 + 1) * spread[0]) - total;
            var prof2 = ((Team2 + 1) * spread[1]) - total;
            var min = prof1;
            if (prof2 < min) { min = prof2; }

            if (min > 0) // TODO: GET RID OF OR TRUE
            {
                System.Console.WriteLine(Title);
                System.Console.WriteLine("Team 1 - " + (spread[0] * amount / total));
                System.Console.WriteLine("Team 2 - " + (spread[1] * amount / total));
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("Profit - " + (min * amount / total));
                Console.ForegroundColor = ConsoleColor.Gray;
                System.Console.WriteLine(Date);
                System.Console.WriteLine(Url);
                System.Console.WriteLine();
            }
        }
    }    
}
