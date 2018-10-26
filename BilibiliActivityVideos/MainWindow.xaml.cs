using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BilibiliActivityVideos
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        string listStatus = "Incompleted";
        int activityId = 0;
        string activityName = "";
        public MainWindow()
        {
            InitializeComponent();

            if(Directory.Exists(Environment.CurrentDirectory + "\\History\\"))
            {
                var query = (from f in Directory.GetFiles(Environment.CurrentDirectory + "\\History\\", "*.dat")
                             let fi = new FileInfo(f)
                             orderby fi.CreationTime descending
                             select fi.FullName).Take(1);
                if (query.ToArray().Length != 0)
                {
                    string path = query.ToArray()[0];

                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    BinaryFormatter b = new BinaryFormatter();
                    fullList = (List<Video>)b.Deserialize(fileStream);
                    fileStream.Close();

                    if ((bool)filterBestBox.IsChecked)
                    {
                        filterBestBox_Checked(null, null);
                    }
                    else
                    {
                        filterBestBox_Unchecked(null, null);
                    }

                    statusBox.Text = "载入记录: " + path.Substring(path.LastIndexOf("\\") + 1);
                }
            }

            //urlBox.Text = "https://www.bilibili.com/blackboard/activity-xB5R0vfAK.html?spm_id_from=333.9.banner_link.13";
            pageSizeSlider.Value = 49;
            startBtn.Content = "开始";
            statusBox.Text += "  (Bulid1810263 by 瑄)";
        }

        [Serializable]
        class Video{
            public string title { get; set; }
            public int av { get; set; }
            public int like { get; set; }
            public int dislike { get; set; }
            public int calcLike { get; set; }
            public int page { get; set; }
            public string link { get; set; }
            public string pubdate { get; set; }
            public string name { get; set; }
        }

        bool runFlag;
        List<Video> fullList;
        List<Video> vList;
        bool IsBestOne = true;
        private void getList(string pageUrl)
        {
            //获取活动主页
            WebClient wc = new WebClient();
            Console.WriteLine("正在解析活动ID");
            Dispatcher.Invoke(new Action(() =>
            {
                statusBox.Text = "正在解析活动ID";
            }));
            string pageData = null;
            try
            {
                pageData = Encoding.UTF8.GetString(wc.DownloadData(pageUrl));
            }
            catch (WebException ex)
            {
                Console.WriteLine("已终止, 网络错误: " + ex.Message);
                Dispatcher.Invoke(new Action(() =>
                {
                    startBtn.Content = "开始";
                    statusBox.Text = "已终止, 网络错误: " + ex.Message;
                }));
                return;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("已终止, URL错误: " + ex.Message);
                Dispatcher.Invoke(new Action(() =>
                {
                    startBtn.Content = "开始";
                    statusBox.Text = "已终止, URL错误: " + ex.Message;
                }));
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine("已终止: " + ex.Message);
                Dispatcher.Invoke(new Action(() =>
                {
                    startBtn.Content = "开始";
                    statusBox.Text = "已终止: " + ex.Message;
                }));
                return;
            }
            //获取活动ID
            Match p = Regex.Match(pageData, "\"scoreId\":\"(?<scoreId>[0-9]*?)\"");
            Match n = Regex.Match(pageData, "\\<title\\>(?<name>.*?) \\- .*?\\</title\\>");
            if (!p.Success)
            {
                Console.WriteLine("已终止, 活动ID解析失败");
                Dispatcher.Invoke(new Action(() =>
                {
                    startBtn.Content = "开始";
                    statusBox.Text = "已终止, 活动ID解析失败";
                }));
                return;
            }
            activityId = int.Parse(p.Groups["scoreId"].Value);
            activityName = WebUtility.HtmlDecode(n.Groups["name"].Value);
            string url = "https://www.bilibili.com/activity/likes/list/" + p.Groups["scoreId"].Value;

            //准备创建列表
            listStatus = "Incompleted";
            fullList = new List<Video>();
            vList = new List<Video>();
            Dispatcher.Invoke(new Action(() =>
            {
                VideoList.Items.Clear();
            }));
            int Screened = 0;
            int i = 1;
            int count = 0;
            int pageCount = 0;
            int pageSize = 0;
            Dispatcher.Invoke(new Action(() => {
                pageSize = (int)pageSizeSlider.Value;
            }));
            if(pageSize < 1 || pageSize > 49)
            {
                Console.WriteLine("每页数量错误: " + pageSize);
                Dispatcher.Invoke(new Action(() =>
                {
                    startBtn.Content = "开始";
                    statusBox.Text = "每页数量错误: " + pageSize;
                    return;
                }));
            }
            //循环获取每页json
            for (int page = 1; ; page++)
            {
                //检测线程状态
                if(runFlag == false)
                {
                    return;
                }
                if(pageCount == 0)
                {
                    Console.WriteLine("正在获取分页: " + page);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        statusBox.Text = "正在获取分页: " + page;
                    }));
                }
                else
                {
                    Console.WriteLine("正在获取分页: " + page + " / " + pageCount);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        statusBox.Text = "正在获取分页: " + page + " / " + pageCount;
                    }));
                }
                
                //获取json
                url = Regex.Replace(url, "\\?.*", string.Empty) + "?pagesize=" + pageSize + "&page=" + page;
                string jsonData = null;
                try
                {
                    jsonData = Encoding.UTF8.GetString(wc.DownloadData(url));
                }
                catch (WebException ex)
                {
                    Console.WriteLine("已终止, 网络错误: " + ex.Message);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        startBtn.Content = "开始";
                        statusBox.Text = "已终止, 网络错误: " + ex.Message;
                    }));
                    return;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine("已终止, URL错误: " + ex.Message);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        startBtn.Content = "开始";
                        statusBox.Text = "已终止, URL错误: " + ex.Message;
                    }));
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("已终止: " + ex.Message);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        startBtn.Content = "开始";
                        statusBox.Text = "已终止: " + ex.Message;
                    }));
                    return;
                }
                //解析json
                bool realtimeSorting = false;
                Dispatcher.Invoke(new Action(() => {
                    realtimeSorting = (bool)realtimeSortingBox.IsChecked;
                }));
                Match pm = Regex.Match(jsonData, "\"count\":(?<pageCount>[0-9]*)");
                if (pm.Success)
                {
                    double.TryParse(pm.Groups["pageCount"].Value, out double vCount);
                    pageCount = (int)Math.Ceiling(vCount / pageSize) + 1;
                }
                MatchCollection vmc = Regex.Matches(jsonData, "\"wid\":(?<av>[0-9]*).*?\"title\":\"(?<title>.*?)\".*?\"pubdate\":(?<pubdate>[0-9]*).*?\"name\":\"(?<name>.*?)\".*?\"like\":(?<like>[0-9]*).*?\"dislike\":(?<dislike>[0-9]*)");
                if (vmc.Count == 0)
                {
                    page--;
                    Console.WriteLine("获取完成: 共 " + page + " 页, " + count + " 个投稿, 过滤掉" + Screened + "个投稿");
                    listStatus = "Completed";
                    Dispatcher.Invoke(new Action(() =>
                    {
                        startBtn.Content = "开始";
                        if (IsBestOne)
                        {
                            statusBox.Text = "获取完成: 共 " + page + " 页, " + count + " 个投稿, 过滤掉 " + Screened + " 个多次投稿";
                        }
                        else
                        {
                            statusBox.Text = "获取完成: 共" + page + " 页, " + count + " 个投稿";
                        }
                    }));
                    break;
                }
                //循环解析每个投稿
                foreach (Match vm in vmc)
                {
                    Video v = new Video();
                    v.title = Regex.Unescape(vm.Groups["title"].Value);
                    v.av = int.Parse(vm.Groups["av"].Value);
                    v.like = int.Parse(vm.Groups["like"].Value);
                    v.dislike = int.Parse(vm.Groups["dislike"].Value);
                    v.calcLike = v.like - v.dislike;
                    v.page = page;
                    v.link = @"https://www.bilibili.com/video/av" + v.av;
                    int timeStamp = int.Parse(vm.Groups["pubdate"].Value);
                    DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
                    long lTime = ((long)timeStamp * 10000000);
                    TimeSpan toNow = new TimeSpan(lTime);
                    DateTime targetDt = dtStart.Add(toNow);
                    v.pubdate = targetDt.ToString("yyyy/MM/dd HH:mm:ss");
                    v.name = Regex.Unescape(vm.Groups["name"].Value);
                    //查重添加
                    fullList.Add(v);
                    bool processed = false;
                    if (IsBestOne)
                    {
                        foreach (Video video in vList)
                        {
                            if (v.name == video.name)
                            {
                                if(v.like > video.like)
                                {
                                    vList.Remove(video);
                                    vList.Add(v);
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        VideoList.Items.Remove(video);
                                        VideoList.Items.Add(v);
                                        if (realtimeSorting)
                                        {
                                            VideoList.Items.Refresh();
                                        }
                                    }));
                                }
                                processed = true;
                                Screened++;
                                break;
                            }
                        }
                    }
                    //在结尾添加
                    if (!processed)
                    {
                        vList.Add(v);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            VideoList.Items.Add(v);
                            if (realtimeSorting)
                            {
                                VideoList.Items.Refresh();
                            }
                        }));
                    }
                    i++;
                    count++;
                }
            }
            Dispatcher.Invoke(new Action(() =>
            {
                VideoList.Items.Refresh();
            }));

            string time = DateTime.Now.ToString().Replace(':', '-').Replace('/', '-');
            Directory.CreateDirectory(Environment.CurrentDirectory + "\\History\\");
            FileStream fileStream = new FileStream(Environment.CurrentDirectory + "\\History\\" + activityId + "_" + listStatus + "_" + activityName + "_" + time + ".dat", FileMode.Create);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fileStream, fullList);
            fileStream.Close();
        }

        Thread T;
        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            if (T!=null && T.IsAlive)
            {
                runFlag = false;
                while (T.ThreadState == ThreadState.Running) ;
                T.Abort();
                T.Join();
                Console.WriteLine("已终止");
                startBtn.Content = "开始";
                statusBox.Text = "已终止";
            }
            else
            {
                string url = urlBox.Text;
                runFlag = true;
                T = new Thread(delegate () {
                    getList(url);
                    
                });
                startBtn.Content = "终止";
                T.Start();
            }
        }

        private void ServerList_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void VideoList_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if ((e.Column.Header.ToString() == "赞数" || e.Column.Header.ToString() == "综合评分") && e.Column.SortDirection == null)
            {
                e.Column.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
            }
        }

        private void VideoList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(VideoList.SelectedItem != null)
            {
                System.Diagnostics.Process.Start(((Video)VideoList.SelectedItem).link);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (T != null && T.IsAlive)
            {
                if (fullList != null && activityId != 0)
                {
                    string time = DateTime.Now.ToString().Replace(':', '-').Replace('/', '-');
                    Directory.CreateDirectory(Environment.CurrentDirectory + "\\History\\");
                    FileStream fileStream = new FileStream(Environment.CurrentDirectory + "\\History\\" + activityId + "_" + listStatus + "_" + activityName + "_" + time + ".dat", FileMode.Create);
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fileStream, fullList);
                    fileStream.Close();
                }

                runFlag = false;
                while (T.ThreadState == ThreadState.Running) ;
                T.Abort();
                T.Join();
                Console.WriteLine("已终止");
            }
        }

        private void VideoList_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void urlBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TextBox TB = (TextBox)sender;
            if (!TB.IsFocused)
            {
                TB.Focus();
                TB.SelectAll();
                e.Handled = true;
            }
        }

        private void urlBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBox TB = (TextBox)sender;
            if (!TB.IsFocused)
            {
                TB.Focus();
                TB.SelectAll();
                e.Handled = true;
            }
        }

        private void filterBestBox_Checked(object sender, RoutedEventArgs e)
        {
            IsBestOne = true;
            if (fullList != null)
            {
                int screened = 0;
                VideoList.Items.Clear();
                vList = new List<Video>();
                foreach (Video v in fullList)
                {
                    bool processed = false;
                    foreach (Video video in vList)
                    {
                        if (v.name == video.name)
                        {
                            if (v.like > video.like)
                            {
                                vList.Remove(video);
                                vList.Add(v);
                                VideoList.Items.Remove(video);
                                VideoList.Items.Add(v);
                            }
                            processed = true;
                            screened++;
                            break;
                        }
                    }
                    //在结尾添加
                    if (!processed)
                    {
                        vList.Add(v);
                        VideoList.Items.Add(v);
                    }

                }
                VideoList.Items.Refresh();
                statusBox.Text = "共" + fullList.Count + " 个投稿, 过滤掉 " + screened + " 个多次投稿";
            }
        }

        private void filterBestBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsBestOne = false;
            if (fullList != null)
            {
                VideoList.Items.Clear();
                vList = new List<Video>();
                foreach(Video v in fullList)
                {
                    VideoList.Items.Add(v);
                    vList.Add(v);
                }
                VideoList.Items.Refresh();
                statusBox.Text = "共" + fullList.Count + " 个投稿";
            }
            
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {              
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();

                FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                BinaryFormatter b = new BinaryFormatter();
                fullList = (List<Video>)b.Deserialize(fileStream);
                fileStream.Close();

                if ((bool)filterBestBox.IsChecked)
                {
                    filterBestBox_Checked(null, null);
                }
                else
                {
                    filterBestBox_Unchecked(null, null);
                }

                statusBox.Text = "载入记录: " + path.Substring(path.LastIndexOf("\\") + 1);
                activityId = 0;
                e.Handled = true;
            }
                
        }

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            ActivityList al = new ActivityList();
            al.ShowDialog();
            if(al.selectedUrl != null)
            {
                Console.WriteLine(al.selectedUrl);
                urlBox.Text = al.selectedUrl;
                startBtn_Click(null, null);
            }
        }
    }

}
