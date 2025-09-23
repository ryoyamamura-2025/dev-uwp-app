using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Devices.Scanners;
using Windows.Storage; // ファイルやフォルダ操作のために追加
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging; // BitmapImageのために追加

namespace ScannerApp
{
    public sealed partial class MainPage : Page
    {
        public class Fruit
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //ダミーの挙動：ログに"画面がロードされました！"と表示
            System.Diagnostics.Debug.WriteLine("画面がロードされました！");

            //// データリストを作成
            //var fruits = new List<Fruit>
            //{
            //    new Fruit { Id = 1, Name = "りんご" },
            //    new Fruit { Id = 2, Name = "ばなな" },
            //    new Fruit { Id = 3, Name = "さくらんぼ" },
            //};

            //// ComboBoxの項目を一旦クリア
            //choiceCombo.Items.Clear();

            //// foreachループで1件ずつ手動で追加
            //foreach (var fruit in fruits)
            //{
            //    // ComboBoxに追加するための新しい項目(ComboBoxItem)を作成
            //    var item = new ComboBoxItem();

            //    // 1. 表示されるテキストを設定
            //    item.Content = fruit.Name;

            //    // 2. 内部で使うための値(Id)を「Tag」プロパティに保存
            //    item.Tag = fruit.Id;

            //    // 3. ComboBoxに作成した項目を追加
            //    choiceCombo.Items.Add(item);
            //}

            //// 初期選択を設定 (Tagが2の項目を探して選択する)
            //foreach (ComboBoxItem item in choiceCombo.Items)
            //{
            //    if ((int)item.Tag == 2)
            //    {
            //        choiceCombo.SelectedItem = item;
            //        break;
            //    }
            //}
        }

        //private void RunButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // 現在選択されている項目をComboBoxItemとして取得
        //    var selectedItem = choiceCombo.SelectedItem as ComboBoxItem;

        //    if (selectedItem != null)
        //    {
        //        // Content(表示名)とTag(Id)をそれぞれ取り出して表示
        //        string name = selectedItem.Content.ToString();
        //        int id = (int)selectedItem.Tag;
        //        OutputTextBlock.Text = $"選択: {name} (ID: {id})";
        //    }
        //    else
        //    {
        //        OutputTextBlock.Text = "何も選択されていません。";
        //    }
        //}


        /// <summary>
        /// 「スキャナーを検索」ボタンがクリックされたときに実行されるメソッド
        /// </summary>
        private async void ScanDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            ScannerStatusTextBlock.Text = "スキャナーを検索中...";
            scannerCombo.Items.Clear();

            // ImageScannerクラスのデバイスをすべて検索する
            string selector = ImageScanner.GetDeviceSelector();
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(selector);

            if (devices.Count > 0)
            {
                // 見つかったデバイスをComboBoxに追加していく
                foreach (var device in devices)
                {
                    var item = new ComboBoxItem
                    {
                        Content = device.Name, // スキャナーの表示名
                        Tag = device.Id        // 接続に使うための一意なID
                    };
                    scannerCombo.Items.Add(item);
                }

                // 最初のスキャナーを自動的に選択状態にする
                scannerCombo.SelectedIndex = 0;
                ScannerStatusTextBlock.Text = $"{devices.Count}台のスキャナーが見つかりました。";
            }
            else
            {
                // スキャナーが見つからなかった場合
                ScannerStatusTextBlock.Text = "利用可能なスキャナーが見つかりませんでした。";
            }
        }
        /// <summary>
        /// 「スキャン実行」ボタンがクリックされたときの処理
        /// </summary>
        private async void ExecuteScanButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. 選択されているスキャナーを取得
            var selectedItem = scannerCombo.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                ScannerStatusTextBlock.Text = "スキャナーが選択されていません。";
                return;
            }

            // Tagに保存したスキャナーのIDを取得
            string scannerDeviceId = selectedItem.Tag.ToString();

            try
            {
                ScannerStatusTextBlock.Text = "スキャナーに接続しています...";

                // 2. スキャナーに接続する
                ImageScanner scanner = await ImageScanner.FromIdAsync(scannerDeviceId);

                // スキャナーが設定（プレビュー）に対応しているかなどを確認
                if (scanner == null)
                {
                    ScannerStatusTextBlock.Text = "スキャナーに接続できませんでした。";
                    return;
                }

                // 3. スキャンしたファイルを保存する一時フォルダを準備
                StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;

                ScannerStatusTextBlock.Text = "スキャンを実行中... スキャナードライバの画面が表示される場合があります。";

                // 4. スキャンを実行し、ファイルをフォルダに保存
                var result = await scanner.ScanFilesToFolderAsync(ImageScannerScanSource.Default, tempFolder);

                // 5. スキャン結果を確認し、画像を表示
                if (result.ScannedFiles.Count > 0)
                {
                    StorageFile scannedFile = result.ScannedFiles[0]; // 最初のファイルを取得

                    // ファイルを読み込んでBitmapImageに変換
                    var bitmapImage = new BitmapImage();
                    using (var stream = await scannedFile.OpenAsync(FileAccessMode.Read))
                    {
                        await bitmapImage.SetSourceAsync(stream);
                    }

                    // Imageコントロールに画像を表示
                    ScannedImage.Source = bitmapImage;
                    ScannerStatusTextBlock.Text = $"スキャンが完了しました: {scannedFile.Name}";

                    // WebView2に画像を送信
                    await SendImageToWebViewAsync(scannedFile);
                }
                else
                {
                    ScannerStatusTextBlock.Text = "スキャンは実行されましたが、画像ファイルは作成されませんでした。";
                }
            }
            catch (Exception ex)
            {
                // 何かエラーが起きた場合
                ScannerStatusTextBlock.Text = "エラーが発生しました: " + ex.Message;
            }
        }

        /// <summary>
        /// 指定された画像ファイルをBase64に変換し、WebView2内のJavaScript関数を呼び出す
        /// </summary>
        /// <param name="imageFile">スキャンされた画像ファイル</param>
        private async System.Threading.Tasks.Task SendImageToWebViewAsync(StorageFile imageFile)
        {
            if (imageFile == null || MyWebView.CoreWebView2 == null)
            {
                return;
            }

            try
            {
                // 1. ファイルをバイト配列に読み込む
                IBuffer buffer = await FileIO.ReadBufferAsync(imageFile);
                byte[] bytes = buffer.ToArray();

                // 2. バイト配列をBase64文字列に変換
                string base64String = Convert.ToBase64String(bytes);

                // 3. JavaScriptで使えるようにデータURI形式の文字列を作成
                //    'data:image/jpeg;base64,' の部分はファイルの形式に合わせて変更可能
                string dataUriString = $"data:image/jpeg;base64,{base64String}";

                // 4. 呼び出すJavaScriptコードを生成
                //    文字列はシングルクォートで囲む
                string script = $"displayScannedImage('{dataUriString}')";

                // 5. WebView2上でJavaScriptを実行
                await MyWebView.CoreWebView2.ExecuteScriptAsync(script);

                System.Diagnostics.Debug.WriteLine("WebView2に画像を送信しました。");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WebView2への画像送信中にエラー: " + ex.Message);
            }
        }

    }
}