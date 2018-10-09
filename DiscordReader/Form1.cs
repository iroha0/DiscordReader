// 【実装履歴】
// Discordに接続(ログイン？)して棒読みちゃんに読み上げさせる
// メールアドレスとパスワードを入力する欄と開始ボタンを追加
// サーバー名やチャンネルをチェックボックス付きのツリーで表示
// ログインとサーバーの一覧をタブページで分けた
// フォームを閉じたときにDiscordSocketClientのDisposeを入れた
// パスワード入力を「*」で隠すようにした
// ツリー表示を サーバー,カテゴリ,テキスト・ボイスチャンネル の並びをDiscordと同じにした
// チャンネルの種類がわかるようにアイコンをつけた
// Discordと同じようにカテゴリの開閉でアイコンが変化するようにした
// サーバーのアイコンを表示するようにした
// ユーザーをIDで登録してチェックボックスで読み上げのON/OFF設定ができるようにした
// サーバーやチャンネルのチェックボックスで読み上げのON/OFF設定ができるようにした
// メールアドレスとパスワードを記憶するチェックボックスをつけた
// ユーザーIDを登録する入力欄の文字数や文字種のチェックを追加
//   警告メッセージを表示するようにした
// 使用中にサーバーとチャンネルの追加と削除があると反映されるようにした
// 不測の事態に備えて全ミュートできるようにした (読み上げ機能のON/OFFをつけた)
// 最小化でタスクトレイに収納する設定を追加
//   アプリのアイコンを用意した
//   右クリックメニューを追加した
//     ミュート (読み上げ機能のON/OFFと連動)
// 
// 
// 
// 
// 【構想】
// アイコンがないサーバーの表示をどうするか
// 
// 終了時に設定を保存して次回起動時に復元する
// 
// DMの処理が必要っぽい？
// 
// サーバーのアイコンが変更されたらTreeViewにも反映する？(更新ボタンでもよさげ)
// 
// 
// 読み上げ内容に名前をつけるかどうかの設定
// 
// 
// 
// nullチェックなど省略してるので、入力内容が間違ってたりするとエラーになるのを直す
// 
// 
// サーバーに参加しているユーザーの一覧 (取得方法がないっぽいので無理？)
// 
// 
// 
// 【Release時の注意事項】
// デバッグの度に入力するのは面倒なので入力済みにしてある。削除してね
// メールアドレスも目隠ししてあるのでプロパティ戻してね
// 
// 


using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using FNF.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordReader
{
    public partial class Form1 : Form
    {

        // Discord と連携するためのインスタンスを生成しておく
        DiscordSocketClient client = new DiscordSocketClient();

        public Form1()
        {
            Console.WriteLine("InitializeComponent()");
            InitializeComponent();


            
            // ログインするための通信で WebSocket とやらを使うのだが、
            // Win7以前のOSは WebSocket に対応してないらしい
            // 代わりに NuGet から WS4Net をインストールして使う

            // OSのバージョン情報を取得する
            System.OperatingSystem os = System.Environment.OSVersion;

            // メジャーバージョンが5以下 または
            // メジャーバージョンが6 かつ マイナーバージョンが1以下 ならばWin7以前のOS
            if ((os.Version.Major <= 5) || (os.Version.Major == 6) && (os.Version.Minor <= 1))
            {
                Console.WriteLine("メジャーバージョン : {0}", os.Version.Major);
                Console.WriteLine("マイナーバージョン : {0}", os.Version.Minor);
                Console.WriteLine("Win7以前のOSなので WebSocket を WS4Net に変更");

                client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    WebSocketProvider = WS4NetProvider.Instance,
                    //UdpSocketProvider = UDPClientProvider.Instance
                });
            }



            // イメージリストの設定
            imageList1.ImageSize = new Size(16, 16);        // 画像のサイズを指定
            treeView1.ImageList = imageList1;       // ツリービューにイメージリストを関連付け


            // 表とデータを関連付けるデータバインドの設定
            //dataGridView1.DataSource = userinfo;      // BindingList を使う場合
            bs = new BindingSource(userinfo, string.Empty);     // BindingSource を経由してListを使う場合
            dataGridView1.DataSource = bs;


            // カラム名を指定
            dataGridView1.Columns[0].HeaderText = "有効化";
            dataGridView1.Columns[1].HeaderText = "名前";
            dataGridView1.Columns[2].HeaderText = "ID";

            // 編集不可にする
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[1].ReadOnly = true;
            dataGridView1.Columns[2].ReadOnly = true;


            // 列ヘッダーの高さを変更できないようにする
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            // 行ヘッダーの幅を変更できないようにする
            dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // 列の幅をユーザーが変更できないようにする
            dataGridView1.AllowUserToResizeColumns = false;
            //行の高さをユーザーが変更できないようにする
            dataGridView1.AllowUserToResizeRows = false;


            // 列の幅を変更できないようにする
            dataGridView1.Columns[0].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[1].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[2].Resizable = DataGridViewTriState.False;

            // 列ヘッダーとすべてのセルの内容に合わせて、列の幅を自動調整する
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;



            // イベント

            // サーバーの設定が変更されたときのイベント (未実装)
            client.GuildUpdated += DiscordGuildUpdated;

            // サーバー追加時・削除時のイベント
            client.JoinedGuild += DiscordJoinedGuild;     // サーバーの参加・追加時
            client.LeftGuild += DiscordLeftGuild;       // サーバーの脱退・削除時

            // カテゴリ・チャンネル追加時のイベント
            client.ChannelCreated += DiscordChannelCreated;      // チャンネル追加時
            client.ChannelDestroyed += DiscordChannelDestroyed;    // チャンネル削除時

            // 切断完了時のイベントを追加
            client.Disconnected += DiscordDisconnected;

            // 接続完了時のイベントを追加
            client.Connected += DiscordConnected;

            // カテゴリを開閉したときのイベントを追加
            treeView1.AfterExpand += NodeAfterExpand;
            treeView1.AfterCollapse += NodeAfterCollapse;

            // メッセージ受信時のイベントを追加
            client.MessageReceived += MessageReceivedAsync;

        }   // Form1 ここまで





        // サーバー情報が変更されたときのイベント？
        // 参加してるサーバーの数だけイベントが発生してる
        // 引数？が2個あって、両方同じIDがでてくる
        private Task DiscordGuildUpdated(SocketGuild arg1, SocketGuild arg2)
        {
/*/
            Console.WriteLine("\nサーバー情報が変更されたときのイベント");

            Console.WriteLine("arg1.Id : {0}", arg1.Id);
            Console.WriteLine("arg2.Id : {0}", arg2.Id);
            
               
            // WEB上のサーバーアイコンを取得する処理
            // アイコンを設定してないサーバーもあるのでNullチェックが必要
            if (SG.IconUrl != null)
            {       // サーバーアイコンがあるとき

                string url = SG.IconUrl;            // URLを取得
                WebClient wc = new WebClient();
                Stream stream = wc.OpenRead(url);
                Bitmap bitmap = new Bitmap(stream);
                stream.Close();

                // イメージリストにサーバーアイコンを追加
                // keyで指定できるようにIDを入れておく
                imageList1.Images.Add(SG.IconId, bitmap);

                GuildNode.ImageIndex = imageList1.Images.IndexOfKey(SG.IconId);          // ノードが非選択時のアイコンを設定
                GuildNode.SelectedImageIndex = imageList1.Images.IndexOfKey(SG.IconId);  // ノードが選択状態のときのアイコンを設定

            }
            else
            {       // サーバーアイコンがないとき

                // NoImage のようなアイコンを表示しておこうかな？
                //GuildNode.ImageIndex = ;          // 非選択時のアイコンを設定
                //GuildNode.SelectedImageIndex = ;  // 選択状態のときのアイコンを設定

            }
//*/


            return null;
            //throw new NotImplementedException();
        }



        // サーバーを脱退・削除したときのイベント
        private Task DiscordLeftGuild(SocketGuild arg)
        {
            Console.WriteLine("\nサーバーを脱退・削除したときのイベント");

            // TreeViewをクリアして、起動時のようにまるごと取得しなおす

            // 「親スレッド」側で処理を行ってもらうコードを
            // デリゲートで記述
            Invoke(new Action(() => {
                treeView1.Nodes.Clear();        // TreeViewをクリアする
            }));

            // TreeViewに サーバー,カテゴリ,チャンネル を追加
            // await/async の非同期処理で異なるスレッドからフォームを操作するには
            // Invoke Delegate を使う
            Invoke(new DelegateTreeView(TreeAddList));

            return null;
            //throw new NotImplementedException();
        }

        // サーバーに参加・追加したときのイベント
        private Task DiscordJoinedGuild(SocketGuild arg)
        {
            Console.WriteLine("\nサーバーに参加・追加したときのイベント");

            // TreeViewをクリアして、起動時のようにまるごと取得しなおす

            // 「親スレッド」側で処理を行ってもらうコードを
            // デリゲートで記述
            Invoke(new Action(() => {
                treeView1.Nodes.Clear();        // TreeViewをクリアする
            }));

            // TreeViewに サーバー,カテゴリ,チャンネル を追加
            // await/async の非同期処理で異なるスレッドからフォームを操作するには
            // Invoke Delegate を使う
            Invoke(new DelegateTreeView(TreeAddList));

            return null;
            //throw new NotImplementedException();
        }


        // チャンネルが削除されたときのイベント
        private Task DiscordChannelDestroyed(SocketChannel arg)
        {
            Console.WriteLine("\nチャンネルが削除されたときのイベント");

            Console.WriteLine("削除されたチャンネルのID : {0}", arg.Id);

            // TreeView のノードをIDで検索
            TreeNode[] SerchNodes = treeView1.Nodes.Find(arg.Id.ToString(), true);

            // 「親スレッド」側で処理を行ってもらうコードを
            // デリゲートで記述
            Invoke(new Action(() => {
                SerchNodes[0].Remove();     // ノードを削除
            }));

            return null;
            //throw new NotImplementedException();
        }

        // チャンネルが追加されたときのイベント
        private Task DiscordChannelCreated(SocketChannel arg)
        {
            Console.WriteLine("\nチャンネルが追加されたときのイベント");

            // 追加されたチャンネルのIDしかわからないので、
            // どこに追加されたのか調べる方法がない

            // TreeViewをクリアして、起動時のようにまるごと取得しなおす

            // 「親スレッド」側で処理を行ってもらうコードを
            // デリゲートで記述
            Invoke(new Action(() => {
                treeView1.Nodes.Clear();        // TreeViewをクリアする
            }));

            // TreeViewに サーバー,カテゴリ,チャンネル を追加
            // await/async の非同期処理で異なるスレッドからフォームを操作するには
            // Invoke Delegate を使う
            Invoke(new DelegateTreeView(TreeAddList));

            return null;
            //throw new NotImplementedException();
        }



        // 接続が切れたときのイベント
        private async Task DiscordDisconnected(Exception arg)
        {
            Console.WriteLine("\n接続が切れたときのイベント");      // 


            // 接続中のままフォームを閉じようとして接続が切れたときの処理
            if (closeflag == true)
            {       // フラグが立ってるとき

                Console.WriteLine("接続中のままフォームを閉じようとして接続が切れたときの処理");

                // 「親スレッド」側で処理を行ってもらうコードを
                // デリゲートで記述
                Invoke(new Action(() => {
                    checkBox1.Checked = false;     // OFF
                    Console.WriteLine("開始/停止 OFF");
                    checkBox1.Text = "開始";     // 表示を開始にする
                }));
            }


            // 開始ボタンがONの状態のままで、通信障害などによる切断が起きたときの処理
            if (checkBox1.Checked == true)
            {       // 開始ボタンがONのままのとき

                Console.WriteLine("開始ボタンがONの状態のままで、通信障害などによる切断が起きたときの処理");

                // 「親スレッド」側で処理を行ってもらうコードを
                // デリゲートで記述
                Invoke(new Action(() => {
                    checkBox1.Checked = false;     // OFF
                    Console.WriteLine("開始/停止 OFF");
                    checkBox1.Text = "開始";     // 表示を開始にする
                }));
            }


            // TreeViewなどのクリアが必要

            // 「親スレッド」側で処理を行ってもらうコードを
            // デリゲートで記述
            Invoke(new Action(() => {
                // メールアドレスとパスワードの入力欄を有効化
                textBox1.Enabled = true;
                textBox2.Enabled = true;


                // ユーザーの登録は接続中でなければ情報を取得できないので、
                // 操作できないように登録ボタンを無効化する
                button1.Enabled = false;

                // 読み上げ機能のチェックボックスを無効化
                checkBox3.Checked = false;
                checkBox3.Enabled = false;


                // タスクトレイアイコンの右クリックメニュー
                // ミュートを無効化
                MuteToolStripMenuItem.Checked = false;
                MuteToolStripMenuItem.Enabled = false;


                // TreeView の内容をクリアする
                treeView1.Nodes.Clear();

                // DataGridView の内容をクリアする
                dataGridView1.Rows.Clear();

                //イメージリストの内容を削除する
                imageList1.Images.Clear();

                Console.WriteLine("クリア処理");
            }));


            // ログアウトしたことがわかるように、読み上げ終了のお知らせ
            var task = await Task.Run(() =>
            {
                using (BouyomiChanClient end = new BouyomiChanClient())
                {
                    Console.WriteLine("ディスコードの読み上げを終了しました。");
                    end.AddTalkTask("ディスコードの読み上げを終了しました。");

                    return true;
                }
            });

            return;
            //throw new NotImplementedException();
        }

        // 接続完了時のイベント
        private Task DiscordConnected()
        {
            Console.WriteLine("接続完了時のイベント");

            // 「親スレッド」側で処理を行ってもらうコードを
            // デリゲートで記述
            Invoke(new Action(() => {

                // メールアドレスとパスワードの入力欄を無効化
                textBox1.Enabled = false;
                textBox2.Enabled = false;

                // 開始/停止 ボタンをONにする
                checkBox1.Checked = true;

                // 起動時はチェックが入っておらず、表示が開始になっている
                // チェックが入った状態は接続中、表示を停止に変更
                checkBox1.Text = "停止";


                // ユーザーIDの登録ができるように Button を有効化
                //textBox3.Enabled = true;
                button1.Enabled = true;

                // 読み上げ機能のチェックボックスを有効化
                checkBox3.Enabled = true;       // 有効化
                checkBox3.Checked = true;       // 読み上げ機能 ON

                // タスクトレイのアイコンの右クリックメニュー
                // ミュートを有効化
                MuteToolStripMenuItem.Enabled = true;
            }));



            // ログインしたことがわかるように、読み上げ開始のお知らせ
            var task = Task.Run(() =>
            {
                using (var start = new BouyomiChanClient())
                {
                    Console.WriteLine("ディスコードの読み上げを開始しました。");
                    start.AddTalkTask("ディスコードの読み上げを開始しました。");
                }
            });



            // TreeViewに サーバー,カテゴリ,チャンネル を追加
            // await/async の非同期処理で異なるスレッドからフォームを操作するには
            // Invoke Delegate を使う
            Invoke(new DelegateTreeView(TreeAddList));


            return task;
            // 戻り値が「ない」とか「おかしい」ってエラーが出たので、直感でなんとなく task にしたら動いた
            // 理解してないので他への影響がないか不安

            //throw new NotImplementedException();

        }   // 接続完了時のイベント ここまで



        // await/async の非同期処理で異なるスレッドからフォームを操作するには
        // Invoke Delegate を使う必要があるので Delegate を用意
        delegate void DelegateTreeView();

        // チェックボックス付きツリーに サーバー・カテゴリ・チャンネル(テキスト ボイス) を追加する処理
// チェックボックスなどの前回の状態を復元したい
        internal void TreeAddList()
        {

            // サーバーの数だけ処理
            foreach (SocketGuild SG in client.Guilds)
            {
                Console.WriteLine("\n----------------------------------------------------------------------------------------------------");
                Console.WriteLine("サーバー名 : {0}", SG.Name);

                
                // 取得した情報の順番はバラバラなので、Discord上と同じ順番に並べ替える処理が必要
                // Positionプロパティ Discord上で、同じ種類のものだけを上から順に 0 から数えた数字 (※カテゴリは例外あり 処理部分参照)
                // サーバーは Positionプロパティ がないっぽいので並べ替えができない
                // Categoryプロパティ 所属しているカテゴリ名がわかる



                // 取得したカテゴリを並べ替える処理

                // カテゴリのオブジェクトをリストに入れて並べ替える
                var CCTabel = new List<SocketCategoryChannel>();    // カテゴリのインスタンス？を入れるListを用意

                // サーバー作成時に最初からある2個のカテゴリは、両方とも Position が 0 になってしまうみたいで、
                // 配列を使うと不都合がある
                // 配列は大きさ(要素数)を決めて作成する必要があるので CategoryChannel.Count で作成するが、
                // Addするとき Position = 0 が2個あるので [0] を上書きしちゃって、配列の末尾がnullになったりする
                // foreach でカテゴリの一覧を表示するなどの処理でnullエラーになるので、
                // Addで末尾に追加してソートできる List を使う

                Console.WriteLine("\n--------------------------------------------------");
                Console.WriteLine("カテゴリ ソート前");

                // カテゴリの数だけ処理
                foreach (SocketCategoryChannel SGCC in SG.CategoryChannels)
                {
                    Console.WriteLine("Position {0} : {1}", SGCC.Position, SGCC.Name);
                    CCTabel.Add(SGCC);      //カテゴリのインスタンス？を末尾に追加していく
                }

                // 並び替え処理 Position でソートして、同じ番号のものはカテゴリ名でソート
                var SortedCCArray = CCTabel.OrderBy(x => x.Position)
                    .ThenBy(x => x.Name);

                // 例
                // Position Name    ソート後
                // 1        てすと   0 Text
                // 0        Voice    0 Voice
                // 2        test     1 てすと
                // 0        Text     2 test


                // ソートできたか確認用
                Console.WriteLine("\nカテゴリ ソート後");
                foreach (SocketCategoryChannel SCCA in SortedCCArray)
                {
                    Console.WriteLine("Position {0} : {1}", SCCA.Position, SCCA.Name);
                }



                // 取得したテキストチャンネルを並べ替える処理

                // テキストチャンネルのオブジェクトをリストに入れて並べ替える
                var TCTable = new List<SocketTextChannel>();    // テキストチャンネルのインスタンス？を入れるListを用意

                Console.WriteLine("\n--------------------------------------------------");
                Console.WriteLine("テキストチャンネル ソート前");

                // テキストチャンネルの数だけ処理
                foreach (SocketTextChannel SGTC in SG.TextChannels)
                {
                    Console.WriteLine("Position {0} : {1}", SGTC.Position, SGTC.Name);
                    TCTable.Add(SGTC);      // テキストチャンネルのインスタンス？を末尾に追加していく
                }

                // 並び替え処理 Position でソート
                var SortedTCArray = TCTable.OrderBy(x => x.Position);

                // ソートできたか確認用
                Console.WriteLine("\nテキストチャンネル ソート後");
                foreach (SocketTextChannel STCA in SortedTCArray)
                {
                    Console.WriteLine("Position {0} : {1}", STCA.Position, STCA.Name);
                }



                // 取得したボイスチャンネルを並べ替える処理

                // ボイスチャンネルのオブジェクトをリストに入れて並べ替える
                var VCTable = new List<SocketVoiceChannel>();    // テキストチャンネルのインスタンス？を入れるListを用意

                Console.WriteLine("\n--------------------------------------------------");
                Console.WriteLine("ボイスチャンネル ソート前");

                // ボイスチャンネルの数だけ処理
                foreach (SocketVoiceChannel SGVC in SG.VoiceChannels)
                {
                    Console.WriteLine("Position {0} : {1}", SGVC.Position, SGVC.Name);
                    VCTable.Add(SGVC);      // ボイスチャンネルのインスタンス？を末尾に追加していく
                }

                // 並び替え処理 Position でソート
                var SortedVCArray = VCTable.OrderBy(x => x.Position);

                //ソートできたか確認用
                Console.WriteLine("\nボイスチャンネル ソート後");
                foreach (SocketVoiceChannel SVCA in SortedVCArray)
                {
                    Console.WriteLine("Position {0} : {1}", SVCA.Position, SVCA.Name);
                }

                //サーバーの カテゴリ・テキスト・ボイス の並び替え完了



                // ここから TreeView のノードを用意する処理

                //foreach サーバーの数だけ処理
                //
                //    親ノード(サーバー)を用意
                //
                //    カテゴリがないサーバーもあるので、nullチェックで分岐
                //    if カテゴリがある
                //
                //        カテゴリがあっても、入ってないチャンネルもあるので nullチェックで分岐が必要
                //
                //        foreach テキストの数だけ処理
                //            if 所属カテゴリがない null
                //                孫ノード(テキスト)の追加は親ノード(サーバー)へ
                //
                //        foreach ボイスの数だけ処理
                //            if 所属カテゴリがない null
                //                孫ノード(ボイス)の追加は親ノード(サーバー)へ
                //
                //        foreach カテゴリの数だけ処理
                //
                //            子ノード(カテゴリ)を用意
                //
                //            foreach テキストの数だけ処理
                //                if 所属カテゴリがあって処理中のカテゴリと同じ
                //                    孫ノード(テキスト)の追加は子ノード(カテゴリ)へ
                //
                //            foreach ボイスの数だけ処理
                //                if 所属カテゴリがあって処理中のカテゴリと同じ
                //                    孫ノード(ボイス)の追加は子ノード(カテゴリ)へ
                //
                //            子ノード(カテゴリ)を親ノード(サーバー)に追加
                //
                //    else カテゴリがない null
                //
                //        カテゴリがないサーバーなので孫ノード(テキスト・ボイスチャンネル)をすべて親ノード(サーバー)に追加すればよい
                //
                //        foreach テキストの数だけ処理
                //            孫ノード(テキスト・ボイス)の追加は親ノード(サーバー)へ
                //
                //        foreach ボイスの数だけ処理
                //            孫ノード(テキスト・ボイス)の追加は親ノード(サーバー)へ


                // イメージリストに画像を登録
                imageList1.Images.Add(Properties.Resources.sharp);      // 0 テキストチャンネル      数値はindex
                imageList1.Images.Add(Properties.Resources.speaker);    // 1 ボイスチャンネル
                imageList1.Images.Add(Properties.Resources.v);          // 2 カテゴリ 展開した状態
                imageList1.Images.Add(Properties.Resources.allow);      // 3 カテゴリ 折りたたんだ状態


                // 親ノード(サーバー名)を用意
                TreeNode GuildNode = new TreeNode(SG.Name);
                GuildNode.Name = SG.Id.ToString();      // Key検索できるようにIDを入れておく
                GuildNode.Tag = SG;        // ノードにインスタンスを入れておく
                GuildNode.Checked = true;       // チェック ON


                // WEB上のサーバーアイコンを取得する処理
                // アイコンを設定してないサーバーもあるのでNullチェックが必要
                if (SG.IconUrl != null)
                {       // サーバーアイコンがあるとき

                    string url = SG.IconUrl;            // URLを取得
                    WebClient wc = new WebClient();
                    Stream stream = wc.OpenRead(url);
                    Bitmap bitmap = new Bitmap(stream);
                    stream.Close();

                    // イメージリストにサーバーアイコンを追加
                    // keyで指定できるようにIDを入れておく
                    imageList1.Images.Add(SG.IconId, bitmap);

                    GuildNode.ImageIndex = imageList1.Images.IndexOfKey(SG.IconId);          // ノードが非選択時のアイコンを設定
                    GuildNode.SelectedImageIndex = imageList1.Images.IndexOfKey(SG.IconId);  // ノードが選択状態のときのアイコンを設定

                }
                else
                {       // サーバーアイコンがないとき

// NoImage のようなアイコンを表示しておこうかな？
                    //GuildNode.ImageIndex = ;          // 非選択時のアイコンを設定
                    //GuildNode.SelectedImageIndex = ;  // 選択状態のときのアイコンを設定

                }

                // 親ノード(サーバー名)を TreeView に追加
                treeView1.Nodes.Add(GuildNode);


                
                // カテゴリがないサーバーもあるので、nullチェックで分岐
                if (SG.CategoryChannels == null)
                {       // カテゴリがないサーバーのとき

                    // すべての孫ノード(テキスト・ボイス)を、親ノード(サーバー)に追加するだけでよい

                    // Discord は テキスト,ボイス の順に表示されるっぽいので、
                    // テキストチャンネルを先に処理する

                    // テキストチャンネルの処理
                    foreach (SocketTextChannel TC in SortedTCArray)       // テキストチャンネルの数だけ処理
                    {
                        // 孫ノード(テキストチャンネル名)を用意
                        TreeNode TCNode = new TreeNode(TC.Name);
                        TCNode.Name = TC.Id.ToString();     // Key検索できるようにIDを入れておく
                        TCNode.Tag = TC;        // ノードにインスタンスを入れておく
                        TCNode.Checked = true;       // チェック ON

                        TCNode.ImageIndex = 0;          // 非選択時のアイコンを設定         イメージリストのindexで指定
                        TCNode.SelectedImageIndex = 0;  // 選択状態のときのアイコンを設定

                        // 親ノード(サーバー名)に孫ノード(テキストチャンネル名)を追加
                        GuildNode.Nodes.Add(TCNode);
                    }


                    // ボイスチャンネルの処理
                    foreach (SocketVoiceChannel VC in SortedVCArray)       // ボイスチャンネルの数だけ処理
                    {
                        // 孫ノード(ボイスチャンネル名)を用意
                        TreeNode VCNode = new TreeNode(VC.Name);
                        VCNode.Name = VC.Id.ToString();         // Key検索できるようにIDを入れておく
                        VCNode.Tag = VC;        //ノードにインスタンスを入れておく
                        VCNode.Checked = true;       // チェック ON

                        VCNode.ImageIndex = 1;          // 非選択時のアイコンを設定         イメージリストのindexで指定
                        VCNode.SelectedImageIndex = 1;  // 選択状態のときのアイコンを設定

                        // 親ノード(サーバー名)に孫ノード(ボイスチャンネル名)を追加
                        GuildNode.Nodes.Add(VCNode);
                    }

                }
                else
                {       // カテゴリがあるサーバーのとき

                    // ただし、カテゴリ内に入ってないチャンネルもあるので注意


                    // カテゴリに入ってないチャンネルを先に処理する

                    // テキストチャンネルの処理
                    foreach (SocketTextChannel TC in SortedTCArray)       // テキストチャンネルの数だけ処理
                    {
                        // サーバー内のすべてのテキストチャンネルのうち、
                        // カテゴリに入ってないチャンネル(所属カテゴリを取得してみてnull)だけを親ノード(サーバー)に追加

                        if (TC.Category == null)
                        {
                            // 孫ノード(テキストチャンネル名)を用意
                            TreeNode TCNode = new TreeNode(TC.Name);
                            TCNode.Name = TC.Id.ToString();     // Key検索できるようにIDを入れておく
                            TCNode.Tag = TC;        //ノードにインスタンスを入れておく
                            TCNode.Checked = true;       // チェック ON

                            TCNode.ImageIndex = 0;          // 非選択時のアイコンを設定         イメージリストのindexで指定
                            TCNode.SelectedImageIndex = 0;  // 選択状態のときのアイコンを設定

                            // 親ノード(サーバー名)に孫ノード(テキストチャンネル名)を追加
                            GuildNode.Nodes.Add(TCNode);
                        }
                    }


                    // ボイスチャンネルの処理
                    foreach (SocketVoiceChannel VC in SortedVCArray)       // ボイスチャンネルの数だけ処理
                    {
                        // サーバー内のすべてのボイスチャンネルのうち、
                        // カテゴリに入ってないチャンネル(所属カテゴリを取得してみてnull)だけを親ノード(サーバー)に追加

                        if (VC.Category == null)
                        {
                            // 孫ノード(ボイスチャンネル名)を用意
                            TreeNode VCNode = new TreeNode(VC.Name);
                            VCNode.Name = VC.Id.ToString();         // Key検索できるようにIDを入れておく
                            VCNode.Tag = VC;        //ノードにインスタンスを入れておく
                            VCNode.Checked = true;       // チェック ON

                            VCNode.ImageIndex = 1;          // 非選択時のアイコンを設定         イメージリストのindexで指定
                            VCNode.SelectedImageIndex = 1;  // 選択状態のときのアイコンを設定

                            // 親ノード(サーバー名)に孫ノード(ボイスチャンネル名)を追加
                            GuildNode.Nodes.Add(VCNode);
                        }
                    }



                    // カテゴリに入ってるチャンネルの処理

                    // カテゴリの数だけ処理
                    foreach (SocketCategoryChannel CC in SortedCCArray)
                    {

                        // 子ノード(カテゴリ名)を用意
                        TreeNode CategoryNode = new TreeNode(CC.Name);
                        CategoryNode.Tag = CC;        // ノードにインスタンスを入れておく
                        CategoryNode.Checked = true;       // チェック ON

                        // 初期は閉じた状態なので 3
                        CategoryNode.ImageIndex = 3;          // 非選択時のアイコンを設定         イメージリストのindexで指定
                        CategoryNode.SelectedImageIndex = 3;  // 選択状態のときのアイコンを設定

                        // 親ノード(サーバー名)に子ノード(カテゴリ名)を追加
                        GuildNode.Nodes.Add(CategoryNode);



                        //テキストチャンネルの処理
                        foreach (SocketTextChannel TC in SortedTCArray)       // テキストチャンネルの数だけ処理
                        {
                            // 所属しているカテゴリがあって、CC.Nameと同じなら孫ノードに追加
                            if (TC.Category != null && CC.Name == TC.Category.Name)
                            {
                                // 孫ノード(テキストチャンネル名)を用意
                                TreeNode TCNode = new TreeNode(TC.Name);
                                TCNode.Name = TC.Id.ToString();     // Key検索できるようにIDを入れておく
                                TCNode.Tag = TC;        // ノードにインスタンスを入れておく
                                TCNode.Checked = true;       // チェック ON

                                TCNode.ImageIndex = 0;          // 非選択時のアイコンを設定         イメージリストのindexで指定
                                TCNode.SelectedImageIndex = 0;  // 選択状態のときのアイコンを設定

                                // 子ノード(カテゴリ名)に孫ノード(テキストチャンネル名)を追加
                                CategoryNode.Nodes.Add(TCNode);
                            }
                        }


                        //ボイスチャンネルの処理
                        foreach (SocketVoiceChannel VC in SortedVCArray)       // ボイスチャンネルの数だけ処理
                        {
                            // 所属しているカテゴリがあって、CC.Nameと同じなら孫ノードに追加
                            if (VC.Category != null && CC.Name == VC.Category.Name)
                            {
                                // 孫ノード(ボイスチャンネル名)を用意
                                TreeNode VCNode = new TreeNode(VC.Name);
                                VCNode.Name = VC.Id.ToString();         // Key検索できるようにIDを入れておく
                                VCNode.Tag = VC;        // ノードにインスタンスを入れておく
                                VCNode.Checked = true;       // チェック ON

                                VCNode.ImageIndex = 1;          // 非選択時のアイコンを設定         イメージリストのindexで指定
                                VCNode.SelectedImageIndex = 1;  // 選択状態のときのアイコンを設定

                                // 子ノード(カテゴリ名)に孫ノード(ボイスチャンネル名)を追加
                                CategoryNode.Nodes.Add(VCNode);
                            }
                        }

                    }   // カテゴリの数だけ処理

                }   // カテゴリがないサーバーかどうか

            }   // サーバーの数だけ処理

        }   // TreeView に サーバー,カテゴリ,テキスト・ボイスチャンネル を追加する処理 ここまで



        // ノードを展開した後に発生するイベント
        private void NodeAfterExpand(object sender, TreeViewEventArgs e)
        {
            Console.WriteLine("ノードを展開した後に発生するイベント");      // 

            // サーバーの開閉はサーバーアイコンのままにする
            // カテゴリノードを開閉したときだけアイコンを変更する
            if (typeof(SocketCategoryChannel) == e.Node.Tag.GetType())
            {
                e.Node.ImageIndex = 2;          // 非選択時のアイコンを設定         イメージリストのindexで指定
                e.Node.SelectedImageIndex = 2;  // 選択状態のときのアイコンを設定
            }

            //throw new NotImplementedException();
        }

        // ノードを折りたたんだ後に発生するイベント
        private void NodeAfterCollapse(object sender, TreeViewEventArgs e)
        {
            Console.WriteLine("ノードを折りたたんだ後に発生するイベント");      // 

            // サーバーの開閉はサーバーアイコンのままにする
            // カテゴリノードを開閉したときだけアイコンを変更する
            if (typeof(SocketCategoryChannel) == e.Node.Tag.GetType())
            {
                e.Node.ImageIndex = 3;          // 非選択時のアイコンを設定         イメージリストのindexで指定
                e.Node.SelectedImageIndex = 3;  // 選択状態のときのアイコンを設定
            }

            //throw new NotImplementedException();
        }



        /// <summary>
        /// メッセージを受け取った時の処理
        /// </summary>
        // メッセージを受信したときのイベント
        public async Task MessageReceivedAsync(SocketMessage arg)
        {
            Console.WriteLine("メッセージを受信したときのイベント");      // 


            /*/
            Console.WriteLine("arg : " + arg);      // 発言内容
            Console.WriteLine("arg.Id : " + arg.Id);    // どのメッセージを修正するのか指定できるように、毎回違うIDになるっぽい
            Console.WriteLine("arg.Attachments : " + arg.Attachments);      // Array
            Console.WriteLine("arg.Author : " + arg.Author);      // 発言者の名前になるっぽい？
            Console.WriteLine("arg.Author.Id : " + arg.Author.Id);      // 発言者のID
            Console.WriteLine("arg.Author.Username : " + arg.Author.Username);      // 
            Console.WriteLine("arg.Channel : " + arg.Channel);      // 発言したテキストチャンネル名
            Console.WriteLine("arg.Channel.Id : " + arg.Channel.Id);      // 発言したテキストチャンネルのID
            Console.WriteLine("arg.Channel.Name : " + arg.Channel.Name);      // 
            Console.WriteLine("arg.Content : " + arg.Content);      // 発言内容
            Console.WriteLine("arg.CreatedAt : " + arg.CreatedAt);      // 発言した時間
            Console.WriteLine("arg.EditedTimestamp : " + arg.EditedTimestamp);      // ????
            Console.WriteLine("arg.Source : " + arg.Source);      // User
            Console.WriteLine("arg.Tags : " + arg.Tags);      // Array
            //Console.WriteLine("arg : " + arg);      // 
            //*/



            // サーバーやカテゴリのチェックがついてるか確認

            // 見逃さないようにしたり、除外するためのユーザー登録はサーバーなどのチェックに関係なく最優先
            // チャンネルにチェックがついてなければ、カテゴリなどににチェックがあっても読み上げないので意味がない
            // カテゴリ内のチャンネルかどうかで取得できる親ノードが変わるので、
            // チャンネルの判定の後でなければサーバーとカテゴリの処理ができない

            // よって処理の順番は ユーザー > チャンネル > カテゴリ > サーバー


            // ユーザー登録してるか調べる
            // 未登録のとき
            //     チャンネルのIDを取得してノードを検索

            //     チャンネルにチェックが入ってる

            //         チャンネルの深度が 1 より大きい(カテゴリ内のチャンネル)
            //             親ノードを取得(カテゴリ)
            //             親ノードを取得(サーバー)

            //             カテゴリにチェックが入ってる

            //                 サーバーにチェックが入ってる
            //                     読み上げ

            //                 else    サーバーにチェックがない 読まない

            //             else     カテゴリにチェックがない 読まない

            //         else    深度 1 (カテゴリなし)
            //             親ノードを取得(サーバー)

            //             サーバーにチェックが入ってる
            //                 読み上げ

            //             else    サーバーにチェックがない 読まない

            //     else    チャンネルにチェックがない 読まない


            // 読み上げ機能が ON になってるか調べる
            if (checkBox3.Checked == true)
            {       // ONになってるとき

                SocketUser SU = client.GetUser(arg.Author.Id);        // メッセージ送信者のIDからインスタンスを取得


                // 次のような使い方をするので、ユーザー設定は最優先で処理する
                // サーバーで全体をOFFにしても、このユーザーだけ読み上げたい というときはユーザー登録をONにする
                // 逆に、サーバーなどの設定がONでも、このユーザーだけ読まない というときはユーザー登録をOFFにする

                // IDでユーザー登録のリストを検索
                UserInfo ui = userinfo.Find(x => x.Id == SU.Id);

                // ユーザー登録されてるかどうかで分岐
                if (null != ui)
                {           // リストに登録があるとき

                    // チェックボックスの状態で分岐
                    if (true == ui.Check)
                    {           // チェックが ON のとき

                        Console.WriteLine("ユーザー登録あり ON");
                        Console.WriteLine("{0} : {1}", SU.Username, SU.Id);
                        Console.WriteLine("メッセージ : {0}", arg.Content);

                        // 読み上げる
                        await Task.Run(() =>
                        {
                        // 棒読みちゃんに読み上げさせる
                        // using を使うとリソース解放してくれるので Dispose() しなくてすむ
                        using (var bc = new BouyomiChanClient())
                            {
                                bc.AddTalkTask(arg.Content);
                            }
                        });

                    }
                    else
                    {           // チェックが OFF のとき
                                // 何もしない
                        Console.WriteLine("ユーザー登録あり OFF");
                    }

                }
                else
                {           // リストに登録がないとき
                    Console.WriteLine("ユーザー登録なし");


                    // チャンネルのIDを取得する
                    string CId = arg.Channel.Id.ToString();

                    // TreeViewのすべてのノードをチャンネルのIDで検索
                    TreeNode[] ChannelNodes = treeView1.Nodes.Find(CId, true);

                    // ID はユニークなので一件しかヒットしないことがわかってる
                    // 何度もインデックスを指定するのは面倒なので変数に入れ直しておく
                    TreeNode ChannelNode = ChannelNodes[0];

                    // なんとなく null チェック
                    // DMの処理が必要っぽい
                    if (null != ChannelNode)
                    {           // 検索でヒットしたノードがあるとき


                        // サーバーのON/OFF以前に、チャンネルがOFFだと意味がない
                        // チャンネルの判定処理が優先

                        //チャンネルのチェックを調べる
                        if (true == ChannelNode.Checked)
                        {       // チャンネルのチェックが付いてるとき

                            // サーバーとカテゴリのチェックを調べる前に、次のことに注意
                            // チャンネルの深度は2パターンある
                            // 
                            // 0 サーバー
                            //     1 チャンネル
                            //     1 カテゴリ
                            //         2 チャンネル
                            // 
                            // 深度が 1 より大きいときだけ、カテゴリ内にあるチャンネルなので親ノード(カテゴリ)を取得できる

                            // チャンネルの深度を調べる
                            if (1 < ChannelNode.Level)
                            {       // 親ノード(カテゴリ)がある深度のとき
                                Console.WriteLine("カテゴリあり");

                                // チャンネルがONでも親のカテゴリがOFFだと読み上げないので
                                // 親ノード(カテゴリ)のチェックを調べる

                                // 親ノード(カテゴリ)を取得する
                                TreeNode CategoryNode = ChannelNode.Parent;

                                // カテゴリのチェックを調べる
                                if (true == CategoryNode.Checked)
                                {       // チェックが付いてるとき

                                    // カテゴリがONでも親のサーバーがOFFだと読み上げないので
                                    // 親ノード(サーバー)のチェックを調べる

                                    // 親ノード(サーバー)を取得する
                                    TreeNode ServerNode = CategoryNode.Parent;

                                    // サーバーのチェックを調べる
                                    if (true == ServerNode.Checked)
                                    {       // チェックが付いてるとき


                                        // 読み上げる
                                        await Task.Run(() =>
                                        {
                                        // 棒読みちゃんに読み上げさせる
                                        // using を使うとリソース解放してくれるので Dispose() しなくてすむ
                                        using (var bc = new BouyomiChanClient())
                                            {
                                                bc.AddTalkTask(arg.Content);
                                            }
                                        });

                                    }
                                    else
                                    {       // チェックがないとき

                                        //読み上げしない

                                    }

                                }
                                else
                                {       // チェックがないとき

                                    //読み上げしない

                                }

                            }
                            else
                            {       // 親ノードがない深度のとき(ルートチャンネル)
                                Console.WriteLine("カテゴリなし");

                                // チャンネルがONでも親のサーバーがOFFだと読み上げないので
                                // 親ノード(サーバー)のチェックを調べる

                                // 親ノード(サーバー)を取得する
                                TreeNode ServerNode = ChannelNode.Parent;

                                // サーバーのチェックを調べる
                                if (true == ServerNode.Checked)
                                {       // チェックが付いてるとき

                                    // 読み上げる
                                    await Task.Run(() =>
                                    {
                                    // 棒読みちゃんに読み上げさせる
                                    // using を使うとリソース解放してくれるので Dispose() しなくてすむ
                                    using (var bc = new BouyomiChanClient())
                                        {
                                            bc.AddTalkTask(arg.Content);
                                        }
                                    });

                                }
                                else
                                {       // チェックがないとき

                                    //読み上げしない

                                }
                            }

                        }
                        else
                        {       // チャンネルのチェックがないとき

                            //読み上げしない
                        }

                    }

                }

            }


        }   // メッセージを受信したときのイベント ここまで



        // 読み上げ 開始/停止 ボタンを押したときのイベント
        // チェックボックスのON/OFFをスイッチに見立ててトグル式のボタンにできる
        // AppearanceプロパティをButtonにする
        private async void checkBox1_ClickAsync(object sender, EventArgs e)
        {
            Console.WriteLine("\n読み上げ 開始/停止 ボタンを押したときのイベント");

            //CheckBox chkBox = (CheckBox)sender;     // ボタンのオブジェクトを取得

            if (checkBox1.Checked)
            {       // ON になったとき

                // テキストボックスに入力してあるメールアドレスを取得
                string email = textBox1.Text;
                Console.WriteLine("メールアドレス : {0}", email);

                // テキストボックスに入力してあるパスワードを取得
                string password = textBox2.Text;
                Console.WriteLine("パスワード : {0}", password);


                // メールアドレスの入力内容があるかチェック
                if (email == null || email.Length == 0)
                {        // 未入力 (null または 空文字"") のとき

                    Console.WriteLine("メールアドレスが未入力です。");

                    // 処理をキャンセルするのでボタンを戻す
                    checkBox1.Checked = false;

                    // メッセージボックスを表示する
                    MessageBox.Show("メールアドレスが未入力です。",
                        "警告",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);

                    return;
                }
                else
                {       // 入力内容があるとき

                    try
                    {
                        // 入力内容がメールアドレス形式になっているか調べる
                        MailAddress a = new MailAddress(textBox1.Text);
                    }
                    catch (FormatException)     // メールアドレス形式になってないとき
                    {
                        Console.WriteLine("メールアドレスを入力してください。");

                        // 処理をキャンセルするのでボタンを戻す
                        checkBox1.Checked = false;

                        // メッセージボックスを表示する
                        MessageBox.Show("メールアドレスを入力してください。",
                            "警告",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);

                        return;
                    }

                }


                // パスワードの入力内容があるかチェック
                if (password == null || password.Length == 0)
                {        // 未入力 (null または 空文字"") のとき

                    Console.WriteLine("パスワードが未入力です。");

                    // 処理をキャンセルするのでボタンを戻す
                    checkBox1.Checked = false;

                    // メッセージボックスを表示する
                    MessageBox.Show("パスワードが未入力です。",
                        "警告",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);

                    return;
                }
                else
                {       // 入力内容があるとき

                    // 正規表現で英数字のみか調べる
                    if (false == new Regex(@"^[0-9a-zA-Z]+$").IsMatch(password))
                    {       // 英数字以外が含まれていたとき

                        Console.WriteLine("パスワードは英数字のみ入力してください。");

                        // 処理をキャンセルするのでボタンを戻す
                        checkBox1.Checked = false;

                        // メッセージボックスを表示する
                        MessageBox.Show("パスワードは英数字のみ入力してください。",
                            "警告",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);

                        return;
                    }

                }

                // メールアドレスとパスワードの入力欄を無効化
                textBox1.Enabled = false;
                textBox2.Enabled = false;

                // 起動時はチェックが入っておらず、表示が開始になっている
                // チェックが入った状態は接続中、表示を停止に変更
                checkBox1.Text = "停止";


                // メールアドレスとパスワードでトークンを取得する処理
                // 入力ミスでトークンを取得できなかった時などの例外処理が必要かも？

                var token = await CheckTokenAsync(email, password);

                if (token == null)
                {       // 返り値が null だったとき

                    Console.WriteLine("トークンを取得できませんでした。");

                    // メールアドレスとパスワードの入力欄を有効化
                    textBox1.Enabled = true;
                    textBox2.Enabled = true;

                    // 表示を開始に戻す
                    checkBox1.Text = "開始";

                    // 処理をキャンセルするのでボタンを戻す
                    checkBox1.Checked = false;

                    // メッセージボックスを表示する
                    MessageBox.Show("トークンを取得できませんでした。",
                        "警告",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);

                    return;
                }

                // ログイン処理
                // 「TokenType.User は旧形式です」という警告がでてるけど、どうしたらいいかわからない
                Console.WriteLine("\nログイン処理中…");
                await client.LoginAsync(TokenType.User, token);
                await client.StartAsync();
                Console.WriteLine("ログイン完了");

                Console.WriteLine("TokenType : {0}", client.TokenType);

            }
            else
            {       // OFF になったとき

                // チェックを外した状態は停止中、表示を開始に変更
                checkBox1.Text = "開始";

                // ログアウト処理
                Console.WriteLine("\nログアウト処理中…");
                await client.StopAsync();
                await client.LogoutAsync();
                Console.WriteLine("ログアウト完了");
            }

            Console.WriteLine("読み上げ 開始/停止 ボタンを押したときのイベント ここまで");
        }   // 読み上げ 開始/停止 ボタンを押したときのイベント ここまで

        /// <summary>
        /// トークンを取得する処理
        /// </summary>
        static async Task<string> CheckTokenAsync(string email, string password)
        {
            Console.WriteLine("トークンを取得する処理");

            // メールアドレスとパスワードでトークンの取得を試みる
            string token = await DiscordApiHelper.LogInAsync(email, password);

            Console.WriteLine("取得したトークン : {0}", token);
            return token;
        }



        // ロード完了時のイベント
        private void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("\nロード完了時のイベント");

            // 前回の終了時、メールアドレスとパスワードを記憶する設定がONだったなら、保存しておいた情報を読み込む
            if (Properties.Settings.Default.AccountSave == true)
            {
                textBox1.Text = Properties.Settings.Default.MailAddress;
                textBox2.Text = Properties.Settings.Default.PassWord;
                checkBox2.Checked = Properties.Settings.Default.AccountSave;
            }

            checkBox4.Checked = Properties.Settings.Default.TaskTray;   // 最小化でタスクトレイに収納する設定
        }

        bool closeflag = false;

        // フォームが閉じる直前のイベント
        private async void Form1_FormClosingAsync(object sender, FormClosingEventArgs e)
        {
            Console.WriteLine("\nフォームが閉じる直前のイベント");

            // 接続中のままフォームを閉じようとしているか調べる
            if (checkBox1.Checked == true)
            {       // 接続中のままのとき

                Console.WriteLine("接続中のままのとき");

                e.Cancel = true;    // フォームが閉じるのをキャンセルする

                closeflag = true;   // Disconnectイベントで使うフラグを立てる

                // ログアウト処理
                Console.WriteLine("\nログアウト処理中…");
                await client.StopAsync();
                await client.LogoutAsync();
                Console.WriteLine("ログアウト完了");

                Console.WriteLine("closing 1");

                Console.WriteLine("アプリを終了する");
                Application.Exit();     // アプリを終了する

                Console.WriteLine("確認用");
                //return;
            }

            Console.WriteLine("closing 2");
            
            Console.WriteLine("e.Cancel = {0}", e.Cancel);
        }

        // フォームを閉じたときのイベント
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Console.WriteLine("\nフォームを閉じたときのイベント");


// チェックボックスなどの状態保存はここでする


            // メールアドレスとパスワードを記憶する設定の保存処理
            // 記憶するかどうかのチェックボックスで分岐
            if (checkBox2.Checked == true)
            {   // ONのとき

                Console.WriteLine("メールアドレスとパスワードを記憶する");
                Properties.Settings.Default.AccountSave = true;     // 記憶するかどうかの設定
                Properties.Settings.Default.MailAddress = textBox1.Text;     // メールアドレス
                Properties.Settings.Default.PassWord = textBox2.Text;        // パスワード
            }
            else
            {   // OFFのとき

                Console.WriteLine("メールアドレスとパスワードの記憶を消去する");
                Properties.Settings.Default.AccountSave = false;     // 記憶するかどうかの設定
                Properties.Settings.Default.MailAddress = "";     // メールアドレス
                Properties.Settings.Default.PassWord = "";        // パスワード
            }

            Properties.Settings.Default.TaskTray = checkBox4.Checked;   // 最小化でタスクトレイに収納する設定

            Properties.Settings.Default.Save();     // 設定を保存する



            // リソース開放
            Console.WriteLine("リソース開放 : client.Dispose();");
            client.Dispose();
        }



        // ユーザーIDを登録するボタンを押したときのイベント
        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("ユーザーを登録するボタンを押したときのイベント");


            string message;

            ulong uid = 0;      // ulong ユーザーIDを入れる変数を用意

            string tid = textBox3.Text;     // テキストボックスの入力内容を取得

            // ペーストで不正な値の入力が可能なのでチェック処理が必要

            // 入力内容があるかチェック
            if (tid == null || tid.Length == 0)
            {        // 未入力のとき

                Console.WriteLine("入力内容がありません。");
                message = "入力内容がありません。";        // メッセージボックスの表示で使う
            }
            else
            {       // 入力内容があるとき

                // 文字数をチェック
                if (tid.Length == 18)
                {       // IDと同じ18桁のとき
                    Console.WriteLine("IDと同じ18桁");


                    // 正規表現で半角数字のみか調べる
                    if (new Regex(@"^[0-9]+$").IsMatch(tid))
                    {       // 半角数字のみのとき


                        // テキストボックスに入力されたID(string)をulong型に変換
                        if (ulong.TryParse(tid, out uid))   // 変換できたら uid に格納される
                        {       // 変換できたとき
                            Console.WriteLine("入力内容 : {0}", uid);


                            // IDからユーザーのインスタンスを取得
                            SocketUser SU = client.GetUser(uid);

                            // サーバー用のIDなど種類があるので、ちゃんとユーザー用のIDで情報を取得できているかチェックが必要
                            // ユーザー用のIDでなければ情報を取得できず null になる
                            if (null != SU)
                            {     // ユーザー情報を取得できているとき (nullではない)
                                Console.WriteLine("ユーザー情報を取得できた");

                                // 登録済みかどうかチェック
                                if (false == userinfo.Exists(x => x.Id == SU.Id))     // IDでリストを検索
                                {           // 未登録のとき (検索して見つからなかった)

                                    Console.WriteLine("未登録なのでリストに追加");

                                    userinfo.Add(new UserInfo(true, SU.Username, SU.Id));       // リストに追加
                                    bs.ResetBindings(false);      // 表示を更新する

                                    // 他のIDを入力できるようにTextBoxをクリアしておく
                                    textBox3.Clear();

                                    return;
                                }
                                else
                                {            // 登録済みのとき

                                    Console.WriteLine("登録済みです。");

// 既に登録されていることがわかるように
// DataGridView の該当する行を選択状態にする機能とかつけてみる？

                                    message = "登録済みです。";        // メッセージボックスの表示で使う
                                }

                            }
                            else
                            {       // ユーザー情報を取得できなかったとき

                                Console.WriteLine("ユーザー情報を取得できませんでした。");
                                Console.WriteLine("サーバーIDなどを入力していないか確認してください。");

                                message = "ユーザー情報を取得できませんでした。\nサーバーIDなどを入力していないか確認してください。";        // メッセージボックスの表示で使う
                            }

                        }
                        else
                        {       // 変換できなかったとき

                            Console.WriteLine("入力された値が不正です。");
                            Console.WriteLine("IDは数値です。");
                            Console.WriteLine(tid);

                            message = "入力された値が不正です。\nIDは数値です。";        // メッセージボックスの表示で使う
                        }

                    }
                    else
                    {       // 半角数字以外が含まれていたとき
                        Console.WriteLine("IDは半角数字のみで入力してください。");

                        message = "IDは半角数字のみで入力してください。";        // メッセージボックスの表示で使う
                    }

                }
                else
                {       // IDの桁数と異なるとき
                    Console.WriteLine("IDの桁数と異なる");

                    Console.WriteLine("入力された値が不正です。");
                    Console.WriteLine("IDは18桁の数値です。");
                    Console.WriteLine(tid);

                    message = "入力された値が不正です。\nIDは18桁の数値です。";        // メッセージボックスの表示で使う
                }

            }


            // 入力内容が正しくないときはここにくる

            // メッセージボックスを表示する
            MessageBox.Show(message,
                "警告",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);

        }   // ユーザーIDを登録するボタンを押したときのイベント ここまで

        BindingSource bs;

        // 設定されたオブジェクトを入れておくリスト
        // これを DataGridView に DataSource でつなぐことで、リストの中身を一覧表示させる
        //BindingList<UserInfo> userinfo = new BindingList<UserInfo>();
        List<UserInfo> userinfo = new List<UserInfo>();

        // 読み上げ対象とするかどうかの設定を保存しておくオブジェクト
        class UserInfo
        {
            public bool Check { get; set; }
            public string Name { get; set; }
            public ulong Id { get; set; }

            public UserInfo(bool check, string name, ulong id)
            {
                Check = check;
                Name = name;
                Id = id;
            }
        }


        // セルがクリックされたときのイベント (スペースでもクリック判定になる)
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            Console.WriteLine("セルがクリックされたときのイベント");      // 

            // チェックボックスがあるセルをクリックしたら、ON/OFFを切り替える
            // どのセルをクリックしたかで分岐
            // チェックボックスがある列をクリックしたときなので ColumnIndex 0
            // 上にある列ヘッダーは RowIndex -1 になってるので、クリックすると「リストの範囲外」と怒られる
            if (e.ColumnIndex == 0 && e.RowIndex > -1)
            {
                Console.WriteLine("チェックボックスがあるセルがクリックされました。");

                if (true == (bool)dataGridView1[e.ColumnIndex, e.RowIndex].Value)
                {       // チェックがONのとき

                    // OFFにする
                    dataGridView1[e.ColumnIndex, e.RowIndex].Value = false;
                }
                else
                {       // チェックがOFFのとき

                    // ONにする
                    dataGridView1[e.ColumnIndex, e.RowIndex].Value = true;
                }


                // 確認用 出力
                Console.WriteLine(
                    string.Format("{0}行目のチェックボックスが {1} に変わりました。",
                    e.RowIndex,
                    dataGridView1[e.ColumnIndex, e.RowIndex].Value));
                foreach (var ui in userinfo)
                {
                    Console.WriteLine("{0} : {1}", ui.Check, ui.Name);
                }

            }
        }



        // 読み上げ機能ON/OFFのチェックボックスをクリックしたときのイベント
        private void checkBox3_Click(object sender, EventArgs e)
        {
            // タスクトレイのアイコンの右クリックメニュー
            // ミュートのチェックを反転する
            MuteToolStripMenuItem.Checked = !MuteToolStripMenuItem.Checked;
        }

        // タスクトレイアイコンの右クリックメニューで
        // ミュートを選択したときのイベント
        private void MuteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 読み上げ機能ON/OFFのチェック状態を反転する
            checkBox3.Checked = !checkBox3.Checked;
        }

        // タスクトレイアイコンの右クリックメニューで
        // 終了を選択したときのイベント
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();     // アプリケーションを終了する
        }

        // タスクトレイアイコンをクリックしたときのイベント
        private void notifyIcon1_MouseUp(object sender, MouseEventArgs e)
        {
            Console.WriteLine("タスクトレイアイコンをクリックしたときのイベント");

            // 左クリックしたときだけ処理
            if (e.Button == MouseButtons.Left)
            {
                if (WindowState == FormWindowState.Minimized)
                {       // ウィンドウが最小化の状態なら ウィンドウを表示する

                    Visible = true;             // ウィンドウの非表示を解除する
                    ShowInTaskbar = true;       // タスクバーの非表示を解除する
                    WindowState = FormWindowState.Normal;       // ウィンドウの最小化を解除する
                }

                // 最小化してなくても、タスクトレイのアイコンをクリックしたらウィンドウをアクティブ化する
                Activate();        // フォームをアクティブにする
            }
        }

        // ウィンドウのサイズが変更されたときのイベント
        private void Form1_Resize(object sender, EventArgs e)
        {
            Console.WriteLine("ウィンドウのサイズが変更されたときのイベント");

            // ウィンドウが最小化されたとき
            if (WindowState == FormWindowState.Minimized)
            {

                // 最小化でタスクトレイに収納する設定がONのとき
                if (checkBox4.Checked == true)
                {
                    //notifyIcon1.Visible = true;    // タスクトレイのアイコンを表示する

                    ShowInTaskbar = false;      // タスクバーに表示しない
                    Visible = false;            // ウィンドウを非表示にする
                    //WindowState = FormWindowState.Normal;   // 古いOSでの表示の対策
                }
            }
        }










        















        // デバッグ用 各種情報を出力
        public void GuildInfo(SocketGuild SG)
        {
            Console.WriteLine("\n----------------------------------------------------------------------------------------------------");
            Console.WriteLine("SG : " + SG + "    //サーバー名");
            Console.WriteLine("SG.Name : " + SG.Name + "    //サーバー名");
            Console.WriteLine("SG.Id : " + SG.Id + "    //サーバーID");
            Console.WriteLine("SG.GetType() : " + SG.GetType() + "    //種類 というか型");
            Console.WriteLine("SG.CreatedAt : " + SG.CreatedAt + "    //作成日時");
            Console.WriteLine("SG.IconUrl : " + SG.IconUrl + "    //サーバーアイコンのURL ファイル名が アイコンID.jpg となる");
            Console.WriteLine("SG.IconId : " + SG.IconId + "    //サーバーアイコンのID ファイル名と同じ");
            Console.WriteLine("SG.VoiceRegionId : " + SG.VoiceRegionId + "    //通信を中継してるサーバーが設置されてる国名");
            Console.WriteLine("SG.Owner : " + SG.Owner + "    //サーバー主名 のはずだが空白のときもある？？？");
            Console.WriteLine("SG.OwnerId : " + SG.OwnerId + "    //サーバー主のID サーバー主名が空白でも出る");
            Console.WriteLine("SG.MemberCount : " + SG.MemberCount + "    //サーバーに参加している人数");
            Console.WriteLine("");
            Console.WriteLine("SG.Users : " + SG.Users + "    //CollectionWrapper サーバーに参加してるユーザー？ 一部しか出てこなかったので要調査");
            foreach (SocketGuildUser SGU in SG.Users)
            {
                Console.WriteLine("SGU.Username : 名前 : " + SGU.Username);
            }


            Console.WriteLine("SG.CurrentUser : " + SG.CurrentUser + "    //現在のユーザー(つまりログインしてる自分自身の名前？)");
            Console.WriteLine("SG.AudioClient : " + SG.AudioClient + "    //????");
            Console.WriteLine("SG.DefaultMessageNotifications : " + SG.DefaultMessageNotifications + "    //???? AllMessages");
            Console.WriteLine("SG.Emotes : " + SG.Emotes + "    //Array エモート 絵文字のリスト？");
            Console.WriteLine("SG.Features : " + SG.Features + "    //Array String ????");
            Console.WriteLine("SG.HasAllMembers : " + SG.HasAllMembers + "    //???? False");
            Console.WriteLine("");

            Console.WriteLine("SG.SplashId : " + SG.SplashId + "    //????");
            Console.WriteLine("SG.SplashUrl : " + SG.SplashUrl + "    //????");
            Console.WriteLine("");

            Console.WriteLine("SG.DownloadedMemberCount : " + SG.DownloadedMemberCount + "    //???? 2");
            Console.WriteLine("SG.DownloaderPromise : " + SG.DownloaderPromise + "    //???? Task Boolean");
            Console.WriteLine("");

            Console.WriteLine("SG.IsEmbeddable : " + SG.IsEmbeddable + "    //???? 埋め込み可能かどうか？ False");
            Console.WriteLine("SG.EmbedChannel : " + SG.EmbedChannel + "    //???? Embed 埋め込み");
            Console.WriteLine("");

            Console.WriteLine("SG.EveryoneRole : " + SG.EveryoneRole + "    //???? Role 役割 @everyone");
            Console.WriteLine("SG.Roles : " + SG.Roles + "    //CollectionWrapper ???? Role 役割");
            Console.WriteLine("");

            Console.WriteLine("SG.IsConnected : " + SG.IsConnected + "    //???? 接続済みかどうか？ False");
            Console.WriteLine("SG.IsSynced : " + SG.IsSynced + "    //???? 同期済みかどうか？ False");
            Console.WriteLine("");

            Console.WriteLine("SG.MfaLevel : " + SG.MfaLevel + "    //???? Disabled");
            Console.WriteLine("SG.SyncPromise : " + SG.SyncPromise + "    //Task Boolean ???? Promise 約束");
            Console.WriteLine("SG.VerificationLevel : " + SG.VerificationLevel + "    //???? Verification 検証 None");
            Console.WriteLine("");

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("チャンネル情報");

            Console.WriteLine("SG.DefaultChannel : " + SG.DefaultChannel + "    //デフォルトチャンネル名 初参加時に最初に表示されるのかな？");
            Console.WriteLine("SG.SystemChannel : " + SG.SystemChannel + "    //システムチャンネル名 だとどうなるのかは不明");

            Console.WriteLine("SG.AFKChannel : " + SG.AFKChannel + "    //AFKチャンネル");
            Console.WriteLine("SG.AFKTimeout : " + SG.AFKTimeout + "    //AFKチャンネル送りになるまでの秒数");

            Console.WriteLine("\nSG.Channels : " + SG.Channels + "    //CollectionWrapper 各チャンネル(カテゴリ・テキスト・ボイスごちゃ混ぜ)のインスタンスが入ってる");
            foreach (SocketGuildChannel SGC in SG.Channels)
            {
                Console.WriteLine("\n------------------------------");
                Console.WriteLine(" SGC : " + SGC + "    //チャンネル名");
                Console.WriteLine(" SGC.Name : " + SGC.Name + "    //チャンネル名");
                Console.WriteLine(" SGC.Id : " + SGC.Id + "    //ID");
                Console.WriteLine(" SGC.Position : " + SGC.Position + "    //Discord上で同じ種類のチャンネルだけを上から数えて何番目かを表す 0から数える");
                Console.WriteLine(" SGC.CreatedAt : " + SGC.CreatedAt + "    //作成日時");
                Console.WriteLine(" SGC.Category : " + SGC.Category + "    //所属しているカテゴリ名");
                Console.WriteLine(" SGC.CategoryId : " + SGC.CategoryId + "    //所属しているカテゴリのID");
                Console.WriteLine(" SGC.Guild : " + SGC.Guild + "    //所属しているサーバー名");
                Console.WriteLine(" SGC.GetType() : " + SGC.GetType() + "    //種類");
                Console.WriteLine(" SGC.PermissionOverwrites : " + SGC.PermissionOverwrites + "    //Array ???? Overwrite");
                Console.WriteLine("");
                Console.WriteLine(" SGC.Users : " + SGC.Users + "    //Array どういうユーザーの情報なのか さっぱりわからない");
                foreach (SocketGuildUser SGCU in SGC.Users)
                {
                    Console.WriteLine("  SGCU : " + SGCU + "    //");
                    Console.WriteLine("  SGCU.Activity : " + SGCU.Activity + "    //");
                    Console.WriteLine("  SGCU.AudioStream : " + SGCU.AudioStream + "    //");
                    Console.WriteLine("  SGCU.AvatarId : " + SGCU.AvatarId + "    //");
                    Console.WriteLine("  SGCU.CreatedAt : " + SGCU.CreatedAt + "    //");
                    Console.WriteLine("  SGCU.Discriminator : " + SGCU.Discriminator + "    //");
                    Console.WriteLine("  SGCU.DiscriminatorValue : " + SGCU.DiscriminatorValue + "    //");
                    Console.WriteLine("  SGCU.GetType() : " + SGCU.GetType() + "    //");
                    Console.WriteLine("  SGCU.Guild : " + SGCU.Guild + "    //");
                    Console.WriteLine("  SGCU.GuildPermissions : " + SGCU.GuildPermissions + "    //");
                    Console.WriteLine("  SGCU.Hierarchy : " + SGCU.Hierarchy + "    //");
                    Console.WriteLine("  SGCU.Id : " + SGCU.Id + "    //");
                    Console.WriteLine("  SGCU.IsBot : " + SGCU.IsBot + "    //");
                    Console.WriteLine("  SGCU.IsDeafened : " + SGCU.IsDeafened + "    //");
                    Console.WriteLine("  SGCU.IsMuted : " + SGCU.IsMuted + "    //");
                    Console.WriteLine("  SGCU.IsSelfDeafened : " + SGCU.IsSelfDeafened + "    //スピーカーミュート True/False Deafened 聴覚障害者");
                    Console.WriteLine("  SGCU.IsSelfMuted : " + SGCU.IsSelfMuted + "    //マイクミュート True/False");
                    Console.WriteLine("  SGCU.IsSuppressed : " + SGCU.IsSuppressed + "    //");
                    Console.WriteLine("  SGCU.IsWebhook : " + SGCU.IsWebhook + "    //");
                    Console.WriteLine("  SGCU.JoinedAt : " + SGCU.JoinedAt + "    //");
                    Console.WriteLine("  SGCU.Mention : " + SGCU.Mention + "    //");
                    Console.WriteLine("  SGCU.Nickname : " + SGCU.Nickname + "    //");
                    Console.WriteLine("  SGCU.Roles : " + SGCU.Roles + "    //");
                    Console.WriteLine("  SGCU.Status : " + SGCU.Status + "    //???? 接続してるのにOffline 意味不明");
                    Console.WriteLine("  SGCU.Username : " + SGCU.Username + "    //");
                    Console.WriteLine("  SGCU.VoiceChannel : " + SGCU.VoiceChannel + "    //参加中のボイスチャンネル名");
                    Console.WriteLine("  SGCU.VoiceSessionId : " + SGCU.VoiceSessionId + "    //");
                    Console.WriteLine("  SGCU.VoiceState : " + SGCU.VoiceState + "    //参加中のボイスチャンネル名");
                    Console.WriteLine("");
                }
                //Console.WriteLine("");

            }

            Console.WriteLine("SG.CategoryChannels : " + SG.CategoryChannels + "    //Array 各カテゴリのインスタンスが入ってる");
            Console.WriteLine("SG.TextChannels : " + SG.TextChannels + "    //Array 各テキストチャンネルのインスタンスが入ってる");
            Console.WriteLine("SG.VoiceChannels : " + SG.VoiceChannels + "    //Array 各ボイスチャンネルのインスタンスが入ってる");


            //Console.WriteLine("" + SG + "    //");
            //Console.WriteLine("");

        }   // デバッグ用 各種情報を出力 ここまで

    }
}
