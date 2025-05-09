﻿using ModelContextProtocol.Server;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace NetEaseMusic_MCP
{
    [McpServerToolType]
    public sealed class NetEaseMusic
    {
        private static readonly string KEY_USE_DYNAMIC_PORT = "UseDynamicPort";
        private static readonly string KEY_STATIC_PORT = "StaticPort";

        private static readonly string DEFAULT_NETEASE_MUSIC_PATH = @"C:\Program Files\NetEase\CloudMusic\cloudmusic.exe";

        private static ChromeDriver? _chromeDriver = null;
        public static ChromeDriver ChromeDriver
        {
            get
            {
                return _chromeDriver ?? throw new InvalidOperationException("Not initialized!");
            }
        }

        // 启动网易云音乐
        public static void StartNetEaseMusic()
        {
            if (_chromeDriver != null)
            {
                return;
            }

            ChromeOptions chromeOptions = new();

            // 设置调试端口
            bool useDynamicPort = ConfigReader.GetConfig<bool>(KEY_USE_DYNAMIC_PORT);
            int port;
            if (useDynamicPort)
            {
                port = PortUtilities.FindFreePort();
                Console.WriteLine($"Using dynamic port: {port}");
            }
            else
            {
                port = ConfigReader.GetConfig<int>(KEY_STATIC_PORT);
                Console.WriteLine($"Using static port: {port}");
            }
            chromeOptions.AddArgument($"remote-debugging-port={port}");

            // 设置网易云音乐可执行文件路径
            string? netease = ConfigReader.GetConfig<string>("NetEaseMusicPath");
            if (string.IsNullOrEmpty(netease) || !File.Exists(netease))
            {
                netease = DEFAULT_NETEASE_MUSIC_PATH;
            }
            netease = Path.GetFullPath(netease);
            Console.WriteLine($"Using NetEase Music: {netease}");
            chromeOptions.BinaryLocation = netease;

            // 指定 ChromeDriver 路径
            string? driver = ConfigReader.GetConfig<string>("ChromeDriverPath");
            Console.WriteLine(driver);
            if (string.IsNullOrEmpty(driver) || !Directory.Exists(driver))
            {
                driver = Path.Combine(AppContext.BaseDirectory, "chromedriver", "win64");
            }
            driver = Path.GetFullPath(driver);
            Console.WriteLine($"Using ChromeDriver: {driver}");
            var driverService = ChromeDriverService.CreateDefaultService(driver);

            _chromeDriver = new ChromeDriver(driverService, chromeOptions);
        }

        public static void StopNetEaseMusic()
        {
            if (_chromeDriver != null)
            {
                _chromeDriver.Quit();
                _chromeDriver.Dispose();
                _chromeDriver = null;
            }
        }
    }

    [McpServerToolType]
    public sealed class NetEaseMusicPlayController
    {
        private static ChromeDriver ChromeDriver => NetEaseMusic.ChromeDriver;

        // 当前是否正在播放
        [McpServerTool, Description("Check if the music is playing.")]
        public static bool IsPlaying()
        {
            var playButton = FindActionButton(ActionButton.Play);
            if (playButton?.GetAttribute("class")?.Contains("play-pause-btn") ?? false)
            {
                return true;
            }
            return false;
        }

        // 播放
        [McpServerTool, Description("Play/Resume the music.")]
        public static string Resume()
        {
            if (!HasPlayList() || IsPlaying())
            {
                return "OK";
            }
            var playButton = FindActionButton(ActionButton.Play);
            playButton?.Click();
            return "OK";
        }

        // 暂停
        [McpServerTool, Description("Pause the music.")]
        public static string Pause()
        {
            if (!HasPlayList() || !IsPlaying())
            {
                return "OK";
            }
            var playButton = FindActionButton(ActionButton.Play);
            playButton?.Click();
            return "OK";
        }

        // 上一曲
        [McpServerTool, Description("Play the previous music.")]
        public static string Previous()
        {
            if (!HasPlayList())
            {
                return "OK";
            }
            var prevButton = FindActionButton(ActionButton.Prev);
            prevButton?.Click();
            return "OK";
        }

        // 下一曲
        [McpServerTool, Description("Play the next music.")]
        public static string Next()
        {
            if (!HasPlayList())
            {
                return "OK";
            }
            var nextButton = FindActionButton(ActionButton.Next);
            nextButton?.Click();
            return "OK";
        }

        // 当前歌曲是否已经喜欢
        [McpServerTool, Description("Check if the current music is liked.")]
        public static bool IsLike()
        {
            if (!HasPlayList())
            {
                return false;
            }
            var likeButton = FindActionButton(ActionButton.Like);
            if (likeButton?.GetAttribute("data-log")?.Contains("\"0\"") ?? false)
            {
                return true;
            }
            return false;
        }

        // 喜欢
        [McpServerTool, Description("Like the current music if not liked.")]
        public static bool Like()
        {
            if (!HasPlayList())
            {
                return false;
            }
            if (IsLike())
            {
                return true;
            }
            var likeButton = FindActionButton(ActionButton.Like);
            likeButton?.Click();
            return true;
        }

        // 取消喜欢
        [McpServerTool, Description("Unlike the current music if liked.")]
        public static bool Unlike()
        {
            if (!HasPlayList())
            {
                return false;
            }
            if (!IsLike())
            {
                return true;
            }
            var likeButton = FindActionButton(ActionButton.Like);
            likeButton?.Click();
            return true;
        }

        // 获取音量
        [McpServerTool, Description("Get the current volume.")]
        public static int GetVolume()
        {
            var volumeButton =
                ChromeDriver.FindElements(By.XPath("//button[contains(@data-log, 'btn_pc_minibar_volume')]"))
                    .Where(i => i.Displayed).FirstOrDefault()
                ?? throw new InvalidOperationException("Volume button not found.");

            // hover 出浮层
            var action = new OpenQA.Selenium.Interactions.Actions(ChromeDriver);
            action.MoveToElement(volumeButton).Perform();

            // 等待滑块出现
            var wait = new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(1));
            var volumeSlider = wait.Until(driver =>
                driver.FindElements(By.XPath("//*[contains(@class, 'VolumnSlider_')]"))
                    .Where(i => i.Displayed).FirstOrDefault());
            var input = volumeSlider.FindElement(By.TagName("input"));
            double percent = double.Parse(input.GetAttribute("value") ?? "0");

            action.MoveToLocation(0, 0).Perform();

            return (int)(percent * 100);
        }

        // 设置音量
        [McpServerTool, Description("Set the volume.")]
        public static string SetVolume([Description("The volume, must be between 0 and 100.")] int volume)
        {
            if (volume < 0 || volume > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 100.");
            }
            var volumeButton =
                ChromeDriver.FindElements(By.XPath("//button[contains(@data-log, 'btn_pc_minibar_volume')]"))
                    .Where(i => i.Displayed).FirstOrDefault()
                ?? throw new InvalidOperationException("Volume button not found.");

            // hover 出浮层
            var action = new OpenQA.Selenium.Interactions.Actions(ChromeDriver);
            action.MoveToElement(volumeButton).Perform();

            // 等待滑块出现
            var wait = new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(1));
            var volumeSlider = wait.Until(driver =>
                driver.FindElements(By.XPath("//*[contains(@class, 'VolumnSlider_')]"))
                    .Where(i => i.Displayed).FirstOrDefault());
            var height = volumeSlider.Size.Height;
            // -height/2 是 100%，height/2 是 0%
            var offsetY = (int)(height * (0.5 - volume / 100.0));
            action.MoveToElement(volumeSlider, 0, offsetY).Click().Perform();

            if (volume == 0)
            {
                // 点击最低能调到 1，静音点一下按钮
                volumeButton.Click();
            }

            action.MoveToLocation(0, 0).Perform();

            return "OK";
        }

        // 当前是否有活跃的播放列表
        [McpServerTool, Description("Check if there is an active playlist. If not, resume or pause are not allowed.")]
        public static bool HasPlayList()
        {
            var playButtons = FindActionButton(ActionButton.Play);
            return playButtons != null;
        }

        // 清空播放列表
        [McpServerTool, Description("Clear the current playlist.")]
        public static bool ClearPlayList()
        {
            if (!HasPlayList())
            {
                return false;
            }

            CloseSideSheet();

            // 点击打开播放列表浮层
            var playlistButton = ChromeDriver.FindElements(By.XPath("//span[contains(@aria-label, 'playlist')]/../.."))
                .Where(i => i.Displayed).FirstOrDefault() ?? throw new InvalidOperationException("Playlist button not found.");
            playlistButton.Click();
            // 等待浮层
            var wait = new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(5));
            var playlistDiv = wait.Until(driver =>
            {
                var div = driver.FindElements(By.Id("page_pc_playlist"))
                    .Where(i => i.Displayed).FirstOrDefault();
                if (div == null)
                {
                    return null;
                }
                return div;
            });
            // aria-label="delete"/../..
            var clearButton = playlistDiv.FindElements(By.XPath("//*[contains(@aria-label, 'delete')]/../.."))
                .Where(i => i.Displayed).FirstOrDefault() ?? throw new InvalidOperationException("Clear button not found.");
            clearButton.Click();

            // wait confirm window
            var confirmContainer = wait.Until(driver =>
            {
                var div = driver.FindElements(By.XPath("//*[contains(@class, 'PortalWrapper_')]"))
                    .Where(i => i.Displayed).FirstOrDefault();
                if (div == null)
                {
                    return null;
                }
                return div;
            });
            // click class="PortalWrapper_*" -> aria-label="confirm"
            var confirmButton = confirmContainer.FindElements(By.XPath("//*[contains(@aria-label, 'confirm')]"))
                .Where(i => i.Displayed).FirstOrDefault() ?? throw new InvalidOperationException("Confirm button not found.");
            confirmButton.Click();

            CloseSideSheet();

            return true;
        }

        // 播放每日推荐
        [McpServerTool, Description("Play the daily music list(每日推荐).")]
        public static async Task<bool> PlayDailyMusicList()
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    // 切换到 “推荐”
                    var dailyMusicListTab = ChromeDriver.FindElement(By.XPath("//div[contains(@data-log, 'cell_pc_main_tab_entrance')]"));
                    dailyMusicListTab.Click();
                    await Task.Delay(500);

                    // hover DailyRecommendWrapper_
                    var dailyMusicListWrapper = ChromeDriver.FindElement(By.XPath("//div[contains(@class, 'DailyRecommendWrapper_')]"));
                    var action = new OpenQA.Selenium.Interactions.Actions(ChromeDriver);
                    action.MoveToElement(dailyMusicListWrapper).Perform();

                    // hover 并点击播放
                    var button = dailyMusicListWrapper.FindElement(By.TagName("button"));
                    action.MoveToElement(button).Click().Perform();

                    return true;
                }
                catch { }
            }

            return false;
        }

        // 获取当前播放音乐
        [McpServerTool, Description("Get the current music info. Empty if no playlist.")]
        public static string GetCurrentPlayingMusic()
        {
            if (!HasPlayList())
            {
                return "";
            }
            var musicInfo = ChromeDriver.FindElement(By.XPath("//div[contains(@class, 'songPlayInfo_')]"));
            var musicNameAndArtists = musicInfo.FindElement(By.ClassName("title")).Text;
            return musicNameAndArtists;
        }

        private enum ActionButton
        {
            Like = 0,
            Prev = 1,
            Play = 2,
            Next = 3,
        }

        private static ReadOnlyCollection<IWebElement> FindActionButtons()
        {
            var playButtons = ChromeDriver.FindElements(By.Id("btn_pc_minibar_play")).Where(i => i.Displayed);
            foreach (var button in playButtons)
            {
                // parent
                var buttonsContainer = button.FindElement(By.XPath(".."));
                return buttonsContainer.FindElements(By.TagName("button"));
            }
            return new([]);
        }

        private static IWebElement? FindActionButton(ActionButton index)
        {
            var buttons = FindActionButtons();
            if (index < 0 || (int)index >= buttons.Count)
            {
                return null;
            }
            return buttons[(int)index];
        }

        private static bool IsSideSheetOpen()
        {
            var sideSheet = ChromeDriver.FindElements(By.ClassName("cmd-sidesheet"))
                .Where(i => i.Displayed).FirstOrDefault();
            return sideSheet != null;
        }

        private static void CloseSideSheet()
        {
            if (!IsSideSheetOpen())
            {
                return;
            }
            var sideSheet = ChromeDriver.FindElements(By.ClassName("cmd-sidesheet"))
                .Where(i => i.Displayed).FirstOrDefault();
            sideSheet?.Click();
        }
    }

    [McpServerToolType]
    public sealed class NetEaseMusicSearcher
    {
        private static ChromeDriver ChromeDriver => NetEaseMusic.ChromeDriver;

        private enum SearchResultType
        {
            Music = 0,
            MusicList = 1,
        }

        private static IWebElement? _searchResultDiv = null;
        private static SearchResultType _searchResultType = SearchResultType.Music;
        private static ReadOnlyCollection<IWebElement> _searchResults = new([]);

        // 单曲搜索
        [McpServerTool, Description("Search music with keyword.")]
        public static async Task<string> SearchMusic([Description("The keyword")] string keyword)
        {
            var resultDiv = await DoSearch(keyword);
            // 切换到“单曲”
            var tab1 = resultDiv.FindElement(By.Id("cmdTab1"))
                ?? throw new Exception("Tab1 not found.");
            tab1.Click();
            await Task.Delay(1000);
            var prompt = resultDiv.FindElement(By.ClassName("prompt"));
            var searchResultCountMatch = new Regex("\\d+").Match(prompt.Text);
            if (!searchResultCountMatch.Success)
            {
                return $"No result for keyword: {keyword}";
            }
            var musicCount = int.Parse(searchResultCountMatch.Value);

            // 获取搜索结果
            var resultList = resultDiv.FindElement(By.XPath("//*[contains(@class, 'ReactVirtualized_')]"));
            var resultItems = resultList.FindElements(By.XPath("//div[contains(@data-log, 'cell_pc_songlist_song')]"));

            _searchResultDiv = resultDiv;
            _searchResultType = SearchResultType.Music;
            _searchResults = resultItems;

            string ret = $"Total: {musicCount}\n---\n" + string.Join("\n---\n", resultItems.Select(item => $"""
                Index: {item.FindElement(By.ClassName("td-num")).Text}
                Name: {item.FindElement(By.ClassName("title")).Text}
                Artists: {item.FindElement(By.ClassName("artists")).GetAttribute("title")}
                Album: {item.FindElement(By.ClassName("td-album")).Text}
                """));
            ret += "\n\nThis result may imcomplete.\nYou can use PlayInSearchResult or PlayAllInSearchResult to play.";
            return ret;
        }

        // 歌单搜索
        [McpServerTool, Description("Search music list with keyword.")]
        public static async Task<string> SearchMusicList(string keyword)
        {
            var resultDiv = await DoSearch(keyword);
            // 切换到“歌单”
            var tab2 = resultDiv.FindElement(By.Id("cmdTab2"))
                ?? throw new Exception("Tab2 not found.");
            tab2.Click();
            await Task.Delay(1000);
            var prompt = resultDiv.FindElement(By.ClassName("prompt"));
            var searchResultCountMatch = new Regex("\\d+").Match(prompt.Text);
            if (!searchResultCountMatch.Success)
            {
                return $"No result for keyword: {keyword}";
            }
            var musicListCount = int.Parse(searchResultCountMatch.Value);

            // 获取搜索结果
            var resultList = resultDiv.FindElement(By.XPath("//*[contains(@class, 'ReactVirtualized_')]"));
            var resultItems = resultList.FindElements(By.XPath("//div[contains(@data-log, 'cell_pc_common_playlist')]"));

            _searchResultDiv = resultDiv;
            _searchResultType = SearchResultType.MusicList;
            _searchResults = resultItems;

            string ret = $"Total: {musicListCount}\n---\n" + string.Join("\n---\n", resultItems.Select(item => $"""
                Index: {item.FindElement(By.ClassName("td-num")).Text}
                Name: {item.FindElement(By.ClassName("title")).Text}
                MusicCount: {item.FindElement(By.ClassName("td-trackCount")).Text}
                PlayCount: {item.FindElement(By.ClassName("td-playCount")).Text}
                """));
            ret += "\n\nThis result may imcomplete.\nYou can use PlayInSearchResult to play.";
            return ret;
        }

        // 专辑搜索
        [McpServerTool, Description("Search album with keyword.")]
        public static async Task<string> SearchAlbum(string keyword)
        {
            var resultDiv = DoSearch(keyword).Result;
            // 切换到“专辑”
            var tab7 = resultDiv.FindElement(By.Id("cmdTab7"))
                ?? throw new Exception("Tab7 not found.");
            tab7.Click();
            await Task.Delay(1000);
            var prompt = resultDiv.FindElement(By.ClassName("prompt"));
            var searchResultCountMatch = new Regex("\\d+").Match(prompt.Text);
            if (!searchResultCountMatch.Success)
            {
                return $"No result for keyword: {keyword}";
            }
            var albumCount = int.Parse(searchResultCountMatch.Value);

            // 获取搜索结果
            var resultList = resultDiv.FindElement(By.XPath("//*[contains(@class, 'ReactVirtualized_')]"));
            var resultItems = resultList.FindElements(By.XPath("//div[contains(@data-log, 'cell_pc_albumlist_album')]"));

            _searchResultDiv = resultDiv;
            _searchResultType = SearchResultType.MusicList;
            _searchResults = resultItems;

            string ret = $"Total: {albumCount}\n---\n" + string.Join("\n---\n", resultItems.Select(item => $"""
                Index: {item.FindElement(By.ClassName("td-num")).Text}
                Name: {item.FindElement(By.ClassName("title")).Text}
                Artists: {item.FindElement(By.ClassName("td-artists")).Text}
                PublishTime: {item.FindElement(By.ClassName("td-publishTime")).Text}
                """));
            ret += "\n\nThis result may imcomplete.\nYou can use PlayInSearchResult to play.";
            return ret;
        }

        // 播放搜索结果
        [McpServerTool, Description("Play music or music list in search result.")]
        public static bool PlayInSearchResult([Description("The 'Index' in search result")] string index)
        {
            if (string.IsNullOrEmpty(index))
            {
                return false;
            }

            if (_searchResults.Count == 0)
            {
                return false;
            }

            if (_searchResultType == SearchResultType.Music)
            {
                var item = _searchResults.FirstOrDefault(i => i.FindElement(By.ClassName("td-num")).Text == index)
                                    ?? throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
                var action = new OpenQA.Selenium.Interactions.Actions(ChromeDriver);
                action.MoveToElement(item).DoubleClick().Perform();
                return true;
            }
            else if (_searchResultType == SearchResultType.MusicList)
            {
                var item = _searchResults.FirstOrDefault(i => i.FindElement(By.ClassName("td-num")).Text == index)
                                    ?? throw new ArgumentOutOfRangeException(nameof(index), "Index out of range.");
                // oid: btn_pc_cover_play
                var playButton = item.FindElement(By.XPath(".//button[contains(@data-log, 'btn_pc_cover_play')]"));
                var action = new OpenQA.Selenium.Interactions.Actions(ChromeDriver);
                action.MoveToElement(item).Click(playButton).Perform();
                return true;
            }

            return false;
        }

        // 播放搜索结果中的所有歌曲
        [McpServerTool, Description("Play all the music in search result.")]
        public static bool PlayAllInSearchResult()
        {
            if (_searchResultDiv == null)
            {
                return false;
            }

            if (_searchResultType == SearchResultType.Music)
            {
                var playAllButton = _searchResultDiv.FindElement(By.XPath("//button[contains(@class, 'play-all')]"));
                playAllButton.Click();
                return true;
            }
            else if (_searchResultType == SearchResultType.MusicList)
            {
                Console.WriteLine("Play all music list is not supported.");
            }

            return false;
        }

        private static async Task<IWebElement> DoSearch(string keyword)
        {
            var searchWrapper = ChromeDriver.FindElements(By.XPath("//div[contains(@class, 'SearchWrapper_')]"))
                  .Where(i => i.Displayed).FirstOrDefault()
                  ?? throw new InvalidOperationException("Search button not found.");
            var searchInput = searchWrapper.FindElements(By.TagName("input")).FirstOrDefault()
                ?? throw new InvalidOperationException("Search input not found.");
            var action = new OpenQA.Selenium.Interactions.Actions(ChromeDriver);
            action.MoveToElement(searchWrapper).Click().Perform();

            try
            {
                // 允许不存在
                var clearButton = searchWrapper.FindElement(By.ClassName("cmd-input-clearbtn"));
                clearButton?.Click();
            }
            catch { }

            await Task.Delay(1000);
            searchInput.SendKeys(keyword);
            await Task.Delay(1000);
            searchInput.SendKeys(Keys.Enter);

            var wait = new WebDriverWait(ChromeDriver, TimeSpan.FromSeconds(5));
            var resultDiv = wait.Until(driver =>
            {
                // 修改搜索关键词时，元素还在但是内容不同
                var result = driver.FindElements(By.Id("page_pc_search_result"))
                        .Where(i => i.Displayed).FirstOrDefault();
                var resultText = result?.FindElement(By.ClassName("keyword"))?.Text;
                if (keyword != resultText)
                {
                    return null;
                }
                return result;
            });

            return resultDiv;
        }
    }
}