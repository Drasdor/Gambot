from bs4 import BeautifulSoup
from lxml import etree
from abc import ABC, abstractmethod
from selenium import webdriver
from colorama import just_fix_windows_console
import requests
import concurrent.futures


class bcolors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'


class WebPageHandler(ABC):
    reference = ""

    @abstractmethod
    def GetMainPageLinks(self, url):
        pass

    def ScrapeCategory(self, reference, money):
        urls = self.GetMainPageLinks(self.Url + reference)
        for url in urls:
            self.ScrapeMatch(url, money)

    @abstractmethod
    def ScrapeMatch(self, url):
        pass

    def GetHTML(self, url):

        # session = requests.Session()
        # response = session.get('https://google.com')
        # headers = ({'User-Agent':
        #    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36 OPR/99.0.0.0'})
        # webpage = requests.get(url, headers=headers, cookies=response.cookies)
        # webpage = requests.get(url)
        # print(BeautifulSoup(webpage.content, 'lxml'))
        # return BeautifulSoup(webpage.content, 'lxml')

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


"""class EasyOdds(WebPageHandler):
    Url = "https://easyodds.com"
    @abstractmethod
    def GetMatchDetails(self, reference):
        pass
    def GetMainPageLinks(self, url):
        homePageLinks = []
        soup = self.GetHTML(url)
        for link in soup.find_all('a', href=True):
            if ("-v-" in link['href']):
                homePageLinks.append(link['href'])
        return homePageLinks


class EasyOddsDraw(EasyOdds):
    def GetMatchDetails(self, reference):
        urls = self.GetMainPageLinks(self.Url + reference)
        matchDetails = []
        for url in urls:
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
                    "//html/body/div[11]/div[6]/div[3]/div[1]/div[2]/div/div[1]/div[1]/div[5]/div[1]/a")[0].text.replace(" ", "")
                draw = dom.xpath(
                    "//html/body/div[11]/div[6]/div[3]/div[1]/div[2]/div/div[2]/div[1]/div[5]/div[1]/a")[0].text.replace(" ", "")
                team2 = dom.xpath(
                    "//html/body/div[11]/div[6]/div[3]/div[1]/div[2]/div/div[3]/div[1]/div[5]/div[1]/a")[0].text.replace(" ", "")
                match.Team1 = self.ConvertToFloat(team1)
                match.Draw = self.ConvertToFloat(draw)
                match.Team2 = self.ConvertToFloat(team2)
                matchDetails.append(match)
            except Exception as error:
                print(error)
        return matchDetails"""


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
    def ScrapeMatch(self, url, money):
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
            match.Test(money)
        except:
            print("Could not display", url)
            pass


class OddsCheckerNoDraw(OddsChecker):
    def ScrapeMatch(self, url, money):
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
            match.Test(money)
        except Exception as error:
            print("Could not display", url)
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
        if True:
            print(self.Title)
            print("Team 1 - ", (spread[0] * money / total))
            print("Draw - ", (spread[1] * money / total))
            print("Team 2 - ", (spread[2] * money / total))
            profAdjusted = min * money / total
            print(bcolors.OKGREEN + "Profit - ", profAdjusted, bcolors.ENDC)
            print(self.Date)
            print()


class MatchNoDrawDetails(MatchDetails):
    def Test(self, money):
        spread = [1.0, self.Team1 / self.Team2]
        total = spread[0] + spread[1]
        prof1 = ((self.Team1 + 1) * spread[0]) - total
        prof2 = ((self.Team2 + 1) * spread[1]) - total
        min = prof1
        if prof2 < min:
            min = prof2
        if True:
            print(self.Title)
            print("Team 1 - ", (spread[0] * money / total))
            print("Team 2 - ", (spread[1] * money / total))
            profAdjusted = min * money / total
            print(bcolors.OKGREEN + "Profit - ", profAdjusted, bcolors.ENDC)
            print(self.Date)
            print()


def FindArbitrage(wbh, reference, money):
    matchDetails = wbh.ScrapeCategory(reference, money)


def main():
    just_fix_windows_console()
    ocd = OddsCheckerDraw()
    ocnd = OddsCheckerNoDraw()
    # FindArbitrage(ocd, "/football", 100)
    FindArbitrage(ocnd, "/tennis", 100)


main()
