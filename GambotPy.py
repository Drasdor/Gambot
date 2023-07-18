from bs4 import BeautifulSoup
from lxml import etree
from abc import ABC, abstractmethod
from selenium import webdriver
from colorama import just_fix_windows_console
import concurrent.futures

class WebPageHandler(ABC):
    reference = ""

    @abstractmethod
    def GetMainPageLinks(self, url):
        pass

    def ScrapeCategory(self, reference):
        urls = self.GetMainPageLinks(self.Url + reference)
        urls = list(dict.fromkeys(urls))
        with concurrent.futures.ThreadPoolExecutor() as executor:
            executor.map(self.ScrapeMatch, urls)

    @abstractmethod
    def ScrapeMatch(self, url):
        pass

    def GetHTML(self, url):
        options = webdriver.ChromeOptions()
        options.add_experimental_option('excludeSwitches', ['enable-logging'])
        driver = webdriver.Chrome(options=options)
        driver.get(url)

        return BeautifulSoup(driver.page_source, 'lxml')

    def ConvertToFloat(self, frac_str):
        try:
            return float(frac_str)
        except ValueError:
            num, denom = frac_str.split('/')
            try:
                leading, num = num.split(' ')
                whole = float(leading)
            except ValueError:
                whole = 0
            frac = float(num) / float(denom)
            return whole - frac if whole < 0 else whole + frac


class OddsChecker(WebPageHandler):
    Url = "https://www.oddschecker.com"

    def GetMainPageLinks(self, url):
        homePageLinks = []
        soup = self.GetHTML(url)
        for link in soup.find_all('a', href=True):
            if ("-v-" in link['href']):
                homePageLinks.append(self.Url + "/" + link['href'])
        return homePageLinks

    @abstractmethod
    def ScrapeMatch(self, url):
        pass


class OddsCheckerDraw(OddsChecker):
    money = 100

    def __init__(self, money):
        self.money = money

    def ScrapeMatch(self, url):
        soup = self.GetHTML(url)
        dom = etree.HTML(str(soup))
        match = MatchDrawDetails()
        match.Title = dom.xpath("//html/head/title")[0].text
        match.Url = url
        try:
            match.Date = dom.xpath(
                "//html/body/div[11]/div[5]/div/div/header/div[2]/div/p")[0].text
        except:
            pass
        try:
            team1 = dom.xpath(
                "//html/body/main/div/div[4]/div/section/section/div[1]/article[1]/section[1]/div/div/div/div[1]/div[1]/button")[0].text.replace(" ", "")
            draw = dom.xpath(
                "//html/body/main/div/div[4]/div/section/section/div[1]/article[1]/section[1]/div/div/div/div[1]/div[2]/button")[0].text.replace(" ", "")
            team2 = dom.xpath(
                "//html/body/main/div/div[4]/div/section/section/div[1]/article[1]/section[1]/div/div/div/div[1]/div[3]/button")[0].text.replace(" ", "")
            match.Team1 = self.ConvertToFloat(team1)
            match.Draw = self.ConvertToFloat(draw)
            match.Team2 = self.ConvertToFloat(team2)
            match.Test(self.money)
        except:
            pass


class OddsCheckerNoDraw(OddsChecker):
    money = 100

    def __init__(self, money):
        self.money = money

    def ScrapeMatch(self, url):
        soup = self.GetHTML(url)
        dom = etree.HTML(str(soup))
        match = MatchNoDrawDetails()
        match.Title = dom.xpath("//html/head/title")[0].text
        match.Url = url
        try:
            match.Date = dom.xpath(
                "//html/body/div[11]/div[5]/div/div/header/div[2]/div/p")[0].text
        except:
            pass
        try:
            team1 = dom.xpath(
                "//html/body/main/div/div[4]/div/section/section/div[1]/article[1]/section[1]/div/div/div/div[1]/div[1]/button")[0].text.replace(" ", "")
            team2 = dom.xpath(
                "//html/body/main/div/div[4]/div/section/section/div[1]/article[1]/section[1]/div/div/div/div[1]/div[2]/button")[0].text.replace(" ", "")
            match.Team1 = self.ConvertToFloat(team1)
            match.Team2 = self.ConvertToFloat(team2)
            match.Test(self.money)
        except:
            pass


class MatchDetails(ABC):
    Title = ""
    Url = ""
    Team1 = 0
    Team2 = 0
    Date = "Couldn't find date"

    @abstractmethod
    def Test(self, money):
        pass


class MatchDrawDetails(MatchDetails):
    Draw = 0

    def Test(self, money):
        spread = [1.0, self.Team1 / self.Draw, self.Team1 / self.Team2]
        total = spread[0] + spread[1] + spread[2]
        prof1 = ((self.Team1 + 1) * spread[0]) - total
        profd = ((self.Draw + 1) * spread[1]) - total
        prof2 = ((self.Team2 + 1) * spread[2]) - total
        min = prof1
        if profd < min:
            min = profd
        if prof2 < min:
            min = prof2
        if min > 0:
            team1Val = spread[0] * money / total
            drawVal = spread[1] * money / total
            team2Val = spread[2] * money / total
            profAdjusted = min * money / total
            print("%s\nTeam 1 - %f\nDraw - %f\nTeam 2 - %f\n\033[92mProfit - %f\033[0m\n%s\n%s\n\n" % (self.Title,
                  team1Val, drawVal, team2Val, profAdjusted, self.Date, self.Url))


class MatchNoDrawDetails(MatchDetails):
    def Test(self, money):
        spread = [1.0, self.Team1 / self.Team2]
        total = spread[0] + spread[1]
        prof1 = ((self.Team1 + 1) * spread[0]) - total
        prof2 = ((self.Team2 + 1) * spread[1]) - total
        min = prof1
        if prof2 < min:
            min = prof2
        if min > 0:
            team1Val = spread[0] * money / total
            team2Val = spread[1] * money / total
            profAdjusted = min * money / total
            print("%s\nTeam 1 - %f\nTeam 2 - %f\n\033[92mProfit - %f\033[0m\n%s\n%s\n\n" % (self.Title,
                  team1Val, team2Val, profAdjusted, self.Date, self.Url))


def main():
    just_fix_windows_console()
    ocd = OddsCheckerDraw(100)
    ocnd = OddsCheckerNoDraw(100)
    ocd.ScrapeCategory("/football")
    #ocnd.ScrapeCategory("/tennis")


main()
