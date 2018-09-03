// チェックボックス付きでサーバー名をリスト表示
// ログインとサーバー一覧をタブページで分けた
// フォームを閉じたときにDiscordSocketClientの処理入れた

using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using FNF.Utility;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordReader
{
    public partial class Form1 : Form
    {

        DiscordSocketClient client = new DiscordSocketClient();

        public Form1()
        {
            Console.WriteLine("InitializeComponent()");
            InitializeComponent();


            // Win7以前のOSはWebSocketに対応してないので、代わりにWS4Netを使って通信する

            // OSのバージョン情報を取得する
            System.OperatingSystem os = System.Environment.OSVersion;

            // メジャーバージョンが5以下 または
            // メジャーバージョンが6 かつ マイナーバージョンが1以下 ならばWin7以前のOS
            if ((os.Version.Major <= 5) || (os.Version.Major == 6) && (os.Version.Minor <= 1))
            {
                Console.WriteLine("メジャーバージョン : " + os.Version.Major);
                Console.WriteLine("マイナーバージョン : " + os.Version.Minor);
                Console.WriteLine("OSがWin7以前");

                client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    WebSocketProvider = WS4NetProvider.Instance,
                    //UdpSocketProvider = UDPClientProvider.Instance,
                });
            }

            // 接続完了時のイベントを追加
            client.Connected += DiscordConnected;

            // 切断完了時のイベントを追加
            client.Disconnected += DiscordDisconnected;

            // メッセージ受信時のイベントを追加
            client.MessageReceived += MessageReceived;
        }

        delegate void ListDelegate(string text);
        
        void addList(string text)
        {
            // リストを追加
            checkedListBox1.Items.Add(text);
        }


        //Delegate を宣言しておく
        delegate void MyDelegate(string text);

        //リストボックスにテキストを追加していくメソッド
        internal void AddText(string text)
        {
            checkedListBox1.Items.Add(text);
        }



        // 接続完了時のイベント
        private Task DiscordConnected()
        {
            // ログインしたことがわかるように、読み上げ開始のお知らせ
            var task = Task.Run(() =>
            {
                using (var start = new BouyomiChanClient())
                {
                    Console.WriteLine("ディスコードの読み上げを開始しました。");
                    start.AddTalkTask("ディスコードの読み上げを開始しました。");
                }
            });


            Console.WriteLine("\nclient.Guilds.Count : " + client.Guilds.Count);    // サーバー数

            //IEnumerator em = client.Guilds.GetEnumerator();

            var gggg = client.Guilds;
            
            foreach (SocketGuild sg in gggg)
            {
                //CheckedListBox にサーバー名を追加する
                Invoke(new MyDelegate(AddText), sg.Name);

                Console.WriteLine("\nsg : " + sg);    // そのまま出力するとサーバー名になるっぽい

                SocketGuildUser su = sg.CurrentUser;
                Console.WriteLine("sg.CurrentUser : " + su);    // 自分の名前#番号

                Console.WriteLine("sg.Name : " + sg.Name);  // サーバー名
                Console.WriteLine("sg.IconUrl : " + sg.IconUrl);    // サーバーのアイコンのURL(Id.jpg)
                Console.WriteLine("sg.IconId : " + sg.IconId);  // サーバーのアイコンのID

                Console.WriteLine("sg.Owner : " + sg.Owner);    // サーバーのオーナー名 名前#番号
                Console.WriteLine("sg.OwnerId : " + sg.OwnerId);    // サーバーのオーナーID

                Console.WriteLine("sg.Users : " + sg.Users);    // ????
                Console.WriteLine("sg.MemberCount : " + sg.MemberCount);    // サーバーに参加している人数

                Console.WriteLine("sg.TextChannels : " + sg.TextChannels);  // ????
                Console.WriteLine("sg.VoiceChannels : " + sg.VoiceChannels);    // ????

                Console.WriteLine("sg.Id : " + sg.Id);  // サーバーID
                Console.WriteLine("sg.Channels : " + sg.Channels);  //????
                Console.WriteLine("sg.CategoryChannels : " + sg.CategoryChannels);  // ????

                var gid = su.Guild.Id;
                Console.WriteLine("su.Guild.Id : " + gid);  // サーバーID

                var gc = sg.GetChannel(gid);
                Console.WriteLine("sg.GetChannel(gid) : " + gc);    // チャンネルのリスト？
            }

            return task;
            // 戻り値が「ない」とか「おかしい」ってエラー出たので直感でtaskにしてみたら動いた
            // 理解してないので他への影響がないか不安

            //throw new NotImplementedException();
        }



        private Task DiscordDisconnected(Exception arg)
        {

            // ログアウトしたことがわかるように、読み上げ終了のお知らせ
            var task = Task.Run(() =>
            {
                using (var end = new BouyomiChanClient())
                {
                    Console.WriteLine("ディスコードの読み上げを終了しました。");
                    end.AddTalkTask("ディスコードの読み上げを終了しました。");
                }
            });

            return task;
            //throw new NotImplementedException();
        }



        /// <summary>
        /// メッセージを受け取った時の処理
        /// </summary>
        public async Task MessageReceived(SocketMessage arg)
        {
            await Task.Run(() =>
            {
                // 棒読みちゃんに読み上げさせる
                // using を使うとリソース解放してくれるので Dispose() しなくてすむ
                using (var bc = new BouyomiChanClient())
                {
                    bc.AddTalkTask(arg.Content);
                }

                

                /*/
                // チャンネル一覧にある・ユーザーID一覧にあるなら
                if (Properties.Settings.Default.TextChannels.IndexOf(arg.Channel.Id.ToString()) >= 0 ||
                Properties.Settings.Default.UserIDs.IndexOf(arg.Author.Id.ToString()) >= 0)
                {
                    // 読み上げ
                    using (var bc = new BouyomiChanClient())
                    {
                        bc.AddTalkTask(arg.Content);
                    }
                }
                //*/
            });
        }





        // 読み上げ 開始/停止 ボタンを押したときの処理
        // チェックボックスにチェックが入っているかどうかを、スイッチのON/OFFに見立ててトグル式のボタンにできる
        // AppearanceプロパティをButtonにする
        private async void checkBox1_CheckedChangedAsync(object sender, EventArgs e)
        {
            CheckBox chkBox = (CheckBox)sender;

            if (chkBox.Checked)
            {

                // 起動時はチェックが入っておらず、表示が開始になっている
                // チェックが入ると表示を停止に変更
                chkBox.Text = "停止";

                // テキストボックスに入力してあるメールアドレスを取得
                string email = textBox1.Text;
                Console.Write("メールアドレス : " + email);

                // テキストボックスに入力してあるパスワードを取得
                string password = textBox2.Text;
                Console.Write("\nパスワード : " + password);

                // メールアドレスとパスワードでトークンを取得する処理
                // 入力ミスでトークンを取得できなかった時などの例外処理が必要かも？
                await CheckTokenAsync(email, password);

                // ログイン処理
                // 「TokenType.User は旧形式です」という警告がでてるけど、どうしたらいいかわからない
                Console.WriteLine("\nログイン処理中…");
                await client.LoginAsync(TokenType.User, Properties.Settings.Default.Token);
                await client.StartAsync();
                Console.WriteLine("ログイン完了");

            }
            else
            {

                // チェックを外した状態は停止中、表示を開始に変更
                chkBox.Text = "開始";

                // ログアウト処理
                Console.WriteLine("ログアウト処理中…");
                await client.StopAsync();
                await client.LogoutAsync();
                Console.WriteLine("ログアウト完了");

                /*/
                // ログアウトしたことがわかるように、読み上げ終了のお知らせ
                var task = Task.Run(() =>
                {
                    using (var end = new BouyomiChanClient())
                    {
                        Console.WriteLine("ディスコードの読み上げを終了しました。");
                        end.AddTalkTask("ディスコードの読み上げを終了しました。");
                    }
                });
                //*/
            }
        }





        /// <summary>
        /// トークンを取得する処理
        /// </summary>
        static async Task CheckTokenAsync(string email, string password)
        {
            // 設定の保存先が空ならメールアドレスとパスワードでトークンを取得
            while (string.IsNullOrEmpty(Properties.Settings.Default.Token) == true)
            {
                try
                {
                    Properties.Settings.Default.Token = await DiscordApiHelper.LogInAsync(email, password);
                    Properties.Settings.Default.Save();
                    Console.WriteLine("\n取得したトークン : " + Properties.Settings.Default.Token);
                }
                catch
                {
                    Console.WriteLine("ログイン失敗");
                }
            }
        }



        // フォームを閉じたときのイベント
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

            // リソース開放
            Console.WriteLine("リソース開放 : client.Dispose();");
            client.Dispose();
        }
    }
}
