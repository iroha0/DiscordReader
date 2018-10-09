// 検索すると出てくるのはBOTを導入する方法ばかりで、
// メールアドレスとパスワードで接続して読み上げさせる方法を説明しているサイトはほとんどなく、
// しかも、ソースコードを公開しているとなると、私が知る限りひとつしかありませんでした。
// ソースコードを GitHub で公開してくれている Yomisen から DiscordApiHelper.cs を使わせてもらってます。
// http://gootalife.hatenablog.com/entry/2017/12/18/145054
// https://github.com/Dy-gtlf/Yomisen

// 2018/09/21 Discordをアップデートしたら接続できなくなった
// Discord公式サイトの開発者用ページを調べたら、
// ヘッダーのコンテンツタイプが違うと認証できない、と黄色の枠で囲まれた記述があった
// 旧 application/json
// 新 application/x-www-form-urlencoded
// 一番下の57行目あたりを修正したら接続できるようになった

using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DiscordReader
{
    class DiscordApiHelper
    {
        /// <summary>
        /// EmailとPasswordでトークンを取得します
        /// </summary>
        public static async Task<string> LogInAsync(string mail, string password)
        {
            Console.WriteLine("メールアドレスとパスワードでトークンを取得します");

            var baseUrl = "https://discordapp.com/api/";
            var appName = "Discord Reader";               // 接続するアプリの名前をサーバーに通知するので必要
            var discordAccount = new { email = mail, password = password };     // key と 値 のペアになってるオブジェクトを用意
            var json = JsonConvert.SerializeObject(discordAccount);             // JSON形式に変換
            var content = new StringContent(json, Encoding.UTF8, "application/json");   // content-type などの設定をつける
            HttpResponseMessage res = null;     // 受け取った応答内容を入れておくために用意

            try
            {
                res = await GetClient(appName).PostAsync(new Uri($@"{baseUrl}/auth/login"), content);   // サーバーにPOST送信する

            }
            catch (TaskCanceledException e)
            {       // タイムアウトの場合、TaskCancelExceptionがスローされる
                Console.WriteLine(e.Message);

                return null;    // null を返す
            }

            // HTTPの応答を調べる
            if (res.IsSuccessStatusCode == true)
            {       // 応答あり
                Console.WriteLine("HTTP応答あり");
                
                var resJson = await res.Content.ReadAsStringAsync();    // サーバーから返ってきた内容を格納
                var deserializedJson = JsonConvert.DeserializeAnonymousType(resJson, new { token = "" });   // key(token)で検索してシリアライズ解除？

                return deserializedJson.token;      // トークンを返す

            }
            else
            {       // 応答なし
                Console.WriteLine("HTTP応答なし");

                return null;    // null を返す
            }
        }

        /// <summary>
        /// ヘッダーの追加
        /// </summary>
        public static HttpClient GetClient(string appName)
        {
            var client = new HttpClient();

            client.Timeout = TimeSpan.FromMilliseconds(5000);       // タイムアウト時間の設定(ミリ秒)
            client.DefaultRequestHeaders.Add("User-Agent", appName);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

    }
}
