using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using FNF.Utility;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordReader
{
    public partial class Form1 : Form
    {

        // Win7以前はWebSocketに対応してないので、代わりにWS4Netを使って通信する
        // OSを判別して使用するSocketを変更する条件分岐コードを追加したい
        DiscordSocketClient client = new DiscordSocketClient(new DiscordSocketConfig
        {
            WebSocketProvider = WS4NetProvider.Instance,
            //UdpSocketProvider = UDPClientProvider.Instance,
        });



        public Form1()
        {
            InitializeComponent();

            // メッセージ受信時のイベントを追加
            client.MessageReceived += MessageReceived;
        }





        /// <summary>
        /// メッセージを受け取った時の処理
        /// </summary>
        static async Task MessageReceived(SocketMessage arg)
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
                Console.WriteLine("ログイン処理中…");
                await client.LoginAsync(TokenType.User, Properties.Settings.Default.Token);
                await client.StartAsync();
                Console.WriteLine("ログイン完了");

                // ログインしたことがわかるように、読み上げ開始のお知らせ
                var task = Task.Run(() =>
                {
                    using (var start = new BouyomiChanClient())
                    {
                        Console.WriteLine("ディスコードの読み上げを開始しました。");
                        start.AddTalkTask("ディスコードの読み上げを開始しました。");
                    }
                });

            }
            else
            {
                // チェックを外した状態は停止中、表示を開始するためのボタンに変更する
                chkBox.Text = "開始";

                // ログアウト処理
                Console.WriteLine("ログアウト処理中…");
                await client.StopAsync();
                await client.LogoutAsync();
                Console.WriteLine("ログアウト完了");

                // ログアウトしたことがわかるように、読み上げ終了のお知らせ
                var task = Task.Run(() =>
                {
                    using (var end = new BouyomiChanClient())
                    {
                        Console.WriteLine("ディスコードの読み上げを終了しました。");
                        end.AddTalkTask("ディスコードの読み上げを終了しました。");
                    }
                });
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





    }
}
