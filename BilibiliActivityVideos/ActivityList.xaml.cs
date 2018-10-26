using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using System.Windows.Shapes;

namespace BilibiliActivityVideos
{
    /// <summary>
    /// ActivityList.xaml 的交互逻辑
    /// </summary>
    public partial class ActivityList : Window
    {
        public ActivityList()
        {
            InitializeComponent();
            T = new Thread(delegate ()
            {
                getList();
            });
            runFlag = true;
            T.Start();
            
        }

        bool runFlag;
        private void getList()
        {
            int count = 0, avaliableCount = 0;
            for(int page=1; ; page++)
            {
                if (runFlag == false)
                {
                    return;
                }
                WebClient wc = new WebClient();
                string pageData = null;
                Console.WriteLine("正在获取页码: " + page);
                Dispatcher.Invoke(new Action(() =>
                {
                    statusBox.Text = "正在获取页码: " + page;
                }));

                try
                {
                    pageData = Encoding.UTF8.GetString(wc.DownloadData("https://www.bilibili.com/activity/page/list?plat=1,3&page=" + page));
                }
                catch (WebException ex)
                {
                    Console.WriteLine("已终止, 网络错误: " + ex.Message);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        statusBox.Text = "已终止, 网络错误: " + ex.Message;
                    }));
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("已终止: " + ex.Message);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        statusBox.Text = "已终止: " + ex.Message;
                    }));
                    return;
                }

                MatchCollection amc = Regex.Matches(pageData, "\"name\":\"(?<name>.*?)\".*?\"pc_url\":\"(?<url>.*?)\"");
                if (amc.Count == 0)
                {
                    page--;
                    Console.WriteLine("获取完成: 共 " + page + " 页, " + avaliableCount + " / " + count + "个活动");
                    Dispatcher.Invoke(new Action(() =>
                    {
                        statusBox.Text = "获取完成: 共 " + page + " 页, " + avaliableCount + " / " + count + "个活动";
                    }));
                    return;
                }
                foreach (Match m in amc)
                {
                    if (runFlag == false)
                    {
                        return;
                    }
                    string aPageData = null;
                    try
                    {
                        aPageData = Encoding.UTF8.GetString(wc.DownloadData(Regex.Unescape(m.Groups["url"].Value)));
                    }
                    catch { }
                    //获取活动ID
                    Match p = Regex.Match(aPageData, "\"scoreId\":\"(?<scoreId>[0-9]*?)\"");
                    
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ListBoxItem lbi = new ListBoxItem() { Content = Regex.Unescape(m.Groups["name"].Value), Tag = Regex.Unescape(m.Groups["url"].Value) };
                        lbi.PreviewMouseDoubleClick += Lbi_PreviewMouseDoubleClick;
                        if (!p.Success)
                        {
                            lbi.IsEnabled = false;
                        }
                        else
                        {
                            avaliableCount++;
                        }
                        aList.Items.Add(lbi);
                        count++;
                    }));
                   
                }
            
            }

        }

        public string selectedUrl = null;
        private void Lbi_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine(((ListBoxItem)e.Source).Tag);
            selectedUrl = ((ListBoxItem)e.Source).Tag.ToString();
            this.Close();
        }

        Thread T;
        private void Window_Closed(object sender, EventArgs e)
        {
            runFlag = false;
            while (T.ThreadState == ThreadState.Running) ;
            T.Abort();
            T.Join();
        }

    }
}
