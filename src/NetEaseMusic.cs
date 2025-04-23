using ModelContextProtocol.Server;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NetEaseMusic_MCP
{
    [McpServerToolType]
    public sealed class NetEaseMusic
    {
        private static readonly string KEY_USE_DYNAMIC_PORT = "UseDynamicPort";
        private static readonly string KEY_STATIC_PORT = "StaticPort";

        private static readonly string DEFAULT_NETEASE_MUSIC_PATH = @"C:\Program Files\NetEase\CloudMusic\cloudmusic.exe";

        private static ChromeDriver? _chromeDriver = null;
        private static ChromeDriver ChromeDriver
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
                driver = Path.GetDirectoryName(Path.GetFullPath("chromedriver.exe"))!;
            }
            driver = Path.GetFullPath(driver);
            Console.WriteLine($"Using ChromeDriver: {driver}");
            var driverService = ChromeDriverService.CreateDefaultService(driver);

            _chromeDriver = new ChromeDriver(driverService, chromeOptions);
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
        enum ActionButton
        {
            Like = 0,
            Prev = 1,
            Play = 2,
            Next = 3,
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

        // 当前是否有活跃的播放列表
        [McpServerTool, Description("Check if there is an active playlist. If not, resume or pause are not allowed.")]
        public static bool HasPlayList()
        {
            var playButtons = FindActionButton(ActionButton.Play);
            return playButtons != null;
        }

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
        public static void Like()
        {
            if (!HasPlayList() && IsLike())
            {
                return;
            }
            var likeButton = FindActionButton(ActionButton.Like);
            likeButton?.Click();
            return;
        }

        // 取消喜欢
        [McpServerTool, Description("Unlike the current music if liked.")]
        public static void Unlike()
        {
            if (!HasPlayList() && !IsLike())
            {
                return;
            }
            var likeButton = FindActionButton(ActionButton.Like);
            likeButton?.Click();
            return;
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
    }
}
