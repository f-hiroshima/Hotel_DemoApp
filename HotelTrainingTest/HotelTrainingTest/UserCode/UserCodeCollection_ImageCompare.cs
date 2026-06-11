using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using System.Windows.Forms;
using Ranorex;
using Ranorex.Core;
using Ranorex.Core.Reporting;
using Ranorex.Core.Testing;
using Ranorex.Controls;
using Ranorex.Core.Repository;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;


namespace HotelTrainingTest.UserCode
{
    /// <summary>
    /// ユーザー コード ライブラリにユーザー コード メソッドを公開するためのファイルです。
    /// </summary>
    [UserCodeCollection]
    public class UserCodeCollection_ImageCompare
    {
        /// <summary>
        /// リポジトリ定義（テーブル利用時）
        /// </summary>
        public static global::HotelTrainingTest.Repository.Hotel_Planisphere repo = global::HotelTrainingTest.Repository.Hotel_Planisphere.Instance;

        /// <summary>
        /// 画像比較比較除外領域指定用
        /// </summary>
        private static List<System.Drawing.Rectangle> _ignoreRectangles;

        
        // ***************************
        // 画像撮影関連 START
        // ***************************        

        /// <summary>
        /// エビデンスフォルダパス取得（作成）
        /// 指定Path\プロジェクト名\現在時刻(yyyyMMddHHmmss)\テストスイート名\テストケース名
        /// 上記に取得した画像やファイルを格納するためにそのPath文字列を作り
        /// フォルダが存在していなければ作成するメソッド
        /// </summary>
        /// <param name="baseFolderPath">プロジェクト以下のパス（c:\Ranorex\Evidence\）</param>
        /// <returns>エビデンスフォルダのパス</returns>
        [UserCodeMethod]
        public static string GetEvidenceFolderPath(string baseFolderPath)
        {
        	
        	var projectName = Path.GetFileName(Directory.GetParent(Directory.GetParent(TestSuite.WorkingDirectory).ToString()).ToString());
        	Report.Info("evidence", string.Format("Get WorkingDirectory folder path {0}", Directory.GetParent(Directory.GetParent(TestSuite.WorkingDirectory).ToString())));

            var testSuiteName = TestSuite.Current.Name;
            var testCaseName = GetTestCaseName();
            var now = System.DateTime.Now.ToString("yyyyMMddHHmmss");

            var evidenceFolderPath = Path.Combine(baseFolderPath, projectName, now, testSuiteName, testCaseName);
            Directory.CreateDirectory(evidenceFolderPath);

            Report.Info("evidence", string.Format("Get evidence folder path {0}", evidenceFolderPath));

            return evidenceFolderPath;
        }

        /// <summary>
        /// 比較画像フォルダパス取得
        /// プロジェクト名＋テストスイート名＋テストケース名のフォルダをPathとして組み合わせてフォルダを作っているので
        /// そのPath文字列を作成して保持しておくためのメソッド
        /// </summary>
        /// <param name="baseFolderPath">プロジェクト以下のパス（c:\Ranorex\）</param>
        /// <returns>比較画像フォルダのパス</returns>
        [UserCodeMethod]
        public static string GetTargetFolderPath(string baseFolderPath)
        {
            var projectName = Path.GetFileName(Directory.GetParent(Directory.GetParent(TestSuite.WorkingDirectory).ToString()).ToString());
            var testSuiteName = TestSuite.Current.Name;
            var testCaseName = GetTestCaseName();

            var targetFolderPath = Path.Combine(baseFolderPath, testSuiteName, testCaseName);

            Report.Info("evidence", string.Format("Get evidence folder path {0}", targetFolderPath));

            return targetFolderPath;
        }

        /// <summary>
        /// [Private] テストケース名取得
        /// GetTestCaseを利用する為のメソッド
        /// 現行中のテストケース名を取得し保持する
        /// </summary>
        /// <returns>テストケース名</returns>
        private static string GetTestCaseName()
        {
            var testContainer = TestSuite.CurrentTestContainer;
            if (testContainer == null)
            {
                return "";
            }
            var testCase = GetTestCase(testContainer);
            return testCase.Name;
        }

        /// <summary>
        /// [Private] テストケース取得
        /// 現行中のテストケースを取得し保持する 親テストケースが存在しない場合は、再帰的に親テストケースを探索する
        /// </summary>
        /// <param name="testContainer">テストコンテナ</param>
        /// <returns>テストケース名</returns>
        private static TestCaseNode GetTestCase(ITestContainer testContainer)
        {
            if (testContainer.IsTestCase)
            {
                return (TestCaseNode)testContainer;
            }
            return GetTestCase(testContainer.ParentContainer);
        }

        /// <summary>
        /// ファイルパスを結合する
        /// </summary>
        /// <param name="baseFilePath">基底ファイルパス</param>
        /// <param name="combinePath">結合するパス</param>
        /// <returns>結合されたパス</returns>
        [UserCodeMethod]
        public static string GetCombinePath(string baseFilePath, string combinePath)
        {
            Report.Log(ReportLevel.Info, "evidence", "baseFilePath=" + baseFilePath + " combinePath=" + combinePath);
            return Path.Combine(baseFilePath, combinePath);
        }

        /// <summary>
        /// ファイル名と拡張子を結合した文字列を作成する
        /// </summary>
        /// <param name="filePath">ファイル保存先の絶対パス</param>
        /// <param name="fileName">ファイル名</param>
        /// <param name="fileNo">ファイル番号</param>
        /// <param name="fileExtension">ファイル拡張子</param>
        /// <returns>ファイル名を含むパス</returns>
        [UserCodeMethod]
        public static string GetCombinePathExtension(string filePath, string fileName, string fileNo, string fileExtension)
        {
            var fileFullName = fileName;
            if (!string.IsNullOrEmpty(fileNo))
            {
                fileFullName = fileFullName + "_" + fileNo;
            }
            fileFullName = fileFullName + fileExtension;

            Report.Log(ReportLevel.Info, "evidence", "baseFilePath=" + filePath + " fileName=" + fileFullName);

            return Path.Combine(filePath, fileFullName);
        }
        
        /// <summary>
        /// 内部クラス（画像ファイル情報）
        /// </summary>
        private class ImageInfo{
        	public bool Success {get; set;}
        	public ImageFormat Format{get; set;}
        }
        
        /// <summary>
        /// フルパスから拡張子を確認してImageFormatを返却する
        /// </summary>
        /// <param name="fileFullPath">画像ファイルのフルPath</param>
        /// <returns>成否、フォーマット</returns>
        private static ImageInfo GetImageFormat(string fileFullPath){
        	try{
        		if(string.IsNullOrEmpty(fileFullPath)){
        			
        			return new ImageInfo { Success = false,Format = ImageFormat.MemoryBmp};
        		}
        		var extensionWord = Path.GetExtension(fileFullPath).ToLower();
				ImageFormat returnFormat;
				switch(extensionWord)
				{
					case ".jpg":
					case ".jpeg":
						returnFormat = ImageFormat.Jpeg;
						break;
					case ".png":
						returnFormat = ImageFormat.Png;
						break;
					case ".gif":
						returnFormat = ImageFormat.Gif;
						break;
					case ".tiff":
					case ".tif":
						returnFormat = ImageFormat.Tiff;
						break;
					default:
						returnFormat = ImageFormat.MemoryBmp;
						break;
				}
       			return new ImageInfo { Success = true,Format = returnFormat};
        	}
        	catch(ArgumentException)
        	{
        		return new ImageInfo { Success = false,Format = ImageFormat.MemoryBmp};
        	}
        	catch(Exception)
        	{
        		return new ImageInfo { Success = false,Format = ImageFormat.MemoryBmp};
        	}
        	
        }
        
        /// <summary>
        /// スクリーンショット取得（オブジェクト）
        /// </summary>
        /// <param name="repoInfo">レポジトリ情報</param>
        /// <param name="evidenceFilePath">エビデンスのファイルパス</param>
        [UserCodeMethod]
        public static void GetScreenshot(RepoItemInfo repoInfo, string evidenceFilePath)
        {
            ProgressForm.Hide();
            try
            {
            	ImageInfo imgInfo = GetImageFormat(evidenceFilePath);
            	if(!imgInfo.Success){
                    Report.Error("evidence", string.Format("画像ファイルパスが存在しません '{0}' is not a web document.", evidenceFilePath));
                    return;
            	}
            	
                var webDocument = repoInfo.CreateAdapter<Unknown>(false);
                if (webDocument == null)
                {
                    Report.Error("evidence", string.Format("Repository item '{0}' is not a web document.", repoInfo.FullName));
                    return;
                }
                Bitmap screenshot = repoInfo.FindAdapter<Unknown>().CaptureCompressedImage();
                // 画像ファイル出力
                screenshot.Save(evidenceFilePath, imgInfo.Format );
                Report.LogData(ReportLevel.Info, "evidence", screenshot);
                DeleteTempFile();
            }
            catch (Exception exception)
            {
                OutputReportException(exception);
            }
            ProgressForm.Show();
        }

        /// <summary>
        /// フルページのスクリーンショット取得
        /// 指定した情報を元にしてフルページのスクリーンショットを取得し画像形式で保存する
        /// ※環境によっては動かない場合がある
        /// </summary>
        /// <param name="repoInfo">レポジトリ情報</param>
        /// <param name="evidenceFilePath">エビデンスファイルのパス</param>
        [UserCodeMethod]
        public static void GetReportFullPageScreenshot(RepoItemInfo repoInfo, string evidenceFilePath)
        {
            ProgressForm.Hide();
            try
            {
            	ImageInfo imgInfo = GetImageFormat(evidenceFilePath);
            	if(!imgInfo.Success){
                    Report.Error("evidence", string.Format("画像ファイルパスが存在しません '{0}' is not a web document.", evidenceFilePath));
                    return;
            	}

            	var webDocument = repoInfo.CreateAdapter<WebDocument>(false);
                if (webDocument == null)
                {
                    Report.Error("evidence", string.Format("Repository item '{0}' is not a web document.", repoInfo.FullName));
                    return;
                }

                Report.Info("evidence", string.Format("Get evidence file path {0}", evidenceFilePath));
                var screenshot = webDocument.CaptureFullPageScreenshot();
                screenshot.Save(evidenceFilePath, imgInfo.Format);
                Report.LogData(ReportLevel.Info, "evidence", screenshot);

                DeleteTempFile();
            }
            catch (Exception exception)
            {
                OutputReportException(exception);
            }
            ProgressForm.Show();
        }

		/// <summary>
        /// 対象リポジトリのスクリーンショットを撮影する
        /// 画像比較で利用する事が多い
        /// PNG型式で保存する（画像比較しやすい）
        /// </summary>
        /// <param name="repoInfo">リポジトリ情報</param>
        /// <param name="saveFileFullPath">保存先ファイルパス</param>
        [UserCodeMethod]
        public static void GetScreenShotRepoItem(RepoItemInfo repoInfo, string saveFileFullPath)
        {
            ProgressForm.Hide();
            try
            {
	        	Bitmap screenshot = repoInfo.FindAdapter<Unknown>().CaptureCompressedImage();
	            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
	            ImageCodecInfo pngCodecInfo = GetEncoderInfo(ImageFormat.Png );
	            EncoderParameters parameters = new EncoderParameters(1);
	            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionNone);
	            screenshot.Save(saveFileFullPath, pngCodecInfo, parameters);
	            Report.LogData(ReportLevel.Info, "スクリーンショット", screenshot);
            }
            catch (Exception exception)
            {
                OutputReportException(exception);
            }
            ProgressForm.Show();	         
        }

		/// <summary>
		/// スクリーンショット取得（Windowsアプリケーション）
		/// スクリーンショット取得（Windowsアプリケーション）
		/// </summary>
		/// <param name="repoItemInfo">撮影対象のアプリケーション（Base:/form から始まる要素）</param>
		/// <param name="evidenceFilePath">保存するエビデンスファイルのFullPath</param>
		[UserCodeMethod]
		public static void GetWinAppScreenshot(RepoItemInfo repoItemInfo, string evidenceFilePath)
		{
			ProgressForm.Hide();
			
			var targetElement = repoItemInfo.CreateAdapter<Unknown>(false);
			if(targetElement == null)
			{
				Report.Error("evidence",string.Format("Repository item '{0}' is not exist.",repoItemInfo.FullName));
				return;
			}
			var screenshot = targetElement.CaptureCompressedImage().Image;
			
			Rectangle rect = new Rectangle(10,1,screenshot.Width -20, screenshot.Height -11);
			screenshot = screenshot.Clone(rect,screenshot.PixelFormat);
						
			screenshot.Save(evidenceFilePath);
				
			DeleteTempFile();
			
			ProgressForm.Show();
		}    	        
       	
        /// <summary>
        /// 内部メソッド
        /// イメージコーデックを取得する
        /// </summary>
        /// <param name="format">イメージフォーマット</param>
        /// <returns>イメージコーデックの情報</returns>
        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        /// <summary>
        /// 内部メソッド
        /// テンプファイル削除
        /// スクリーンショット取得時に作成されたTempファイルを削除する
        /// </summary>
        private static void DeleteTempFile()
        {
            var tempFilePath = Path.GetTempFileName();
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    Report.Error(string.Format("Delete temp file failed '{0}'", tempFilePath));
                }
            }
        }

        /// <summary>
        /// 内部メソッド
        /// 引数がnullかどうか確認しnullの場合ArgumentNullExceptionを投げる
        /// </summary>
        /// <param name="argument">引数</param>
        /// <param name="argumentName">引数の名前</param>
        private static void CheckArgumentNotNull(object argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }        

        /// <summary>
        /// 内部メソッド
        /// 発生した例外をRanorexの標準機能を使って報告
        /// </summary>
        /// <param name="exception">例外</param>
        private static void OutputReportException(Exception exception)
        {
            CheckArgumentNotNull(exception, nameof(exception));
            Report.Log(ReportLevel.Error, "evidence", "Exception occurred: in usercode", new SimpleReportMetadata("stacktrace",exception.ToString()));
        }

        // ***************************
        // 画像撮影 END
        // ***************************        

        // ***************************
        // Validate関連 START
        // ***************************        

        /// <summary>
        /// [Validate]
        /// 指定されたファイルパスにファイルが存在するかを確認する
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        [UserCodeMethod]
        public static void ValidateFileExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                Validate.IsTrue(true, "ファイルの存在を確認しました。[" + filePath + "]");
            }
            else
            {
                Validate.Fail("ファイルが存在しません。[" + filePath + "]");
            }
        }		

 		/// <summary>
        /// [Validate]
        /// 指定された2つのビットマップを比較し、結果を判定する
        /// 結果が同一の場合は何もせず、差分がある場合には差分であることを示すビットマップを出力する
        /// </summary>
        /// <param name="bmp1Path">ビットマップ1のパス</param>
        /// <param name="bmp2Path">ビットマップ2のパス</param>
        /// <param name="resultPath">結果を出力するビットマップのパス</param>
        [UserCodeMethod]
        public static void ValidateCompareImageBitmap(string bmp1Path, string bmp2Path, string resultPath)
        {
            try
            {
                string compareResult = "no different";
                // 2つの画像を読み込み、幅と高さを取得
                Bitmap bmp1 = new Bitmap(bmp1Path);
                Bitmap bmp2 = new Bitmap(bmp2Path);
                int width = Math.Max(bmp1.Width, bmp2.Width);
                int height = Math.Max(bmp1.Height, bmp2.Height);
                Bitmap diffBmp = new Bitmap(width, height);
                Color diffColor = Color.Red;

                // ピクセルごとに比較し、異なる場合は差分画像にそのピクセルを設定
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        try
                        {
                            Color color1 = bmp1.GetPixel(i, j);
                            Color color2 = bmp2.GetPixel(i, j);
                            //許容誤差
                            int tolerance = 2;
                            if (Math.Abs(color1.R - color2.R) <= tolerance &&
                                Math.Abs(color1.G - color2.G) <= tolerance &&
                                Math.Abs(color1.B - color2.B) <= tolerance)
                            {
                                diffBmp.SetPixel(i, j, color1);
                            }
                            else
                            {
                                diffBmp.SetPixel(i, j, diffColor);
                                compareResult = "different";
                            }
                        }
                        catch
                        {
                            // 異なるサイズの場合は例外が発生するため、その場合は差分として扱う
                            diffBmp.SetPixel(i, j, diffColor);
                            compareResult = "different";
                        }
                    }
                }

                diffBmp.Save(resultPath, ImageFormat.Png);
                if (compareResult == "no different")
                {
                    Report.LogData(ReportLevel.Info, "bmp1", bmp1);
                    Report.LogData(ReportLevel.Info, "bmp2", bmp2);
                	Report.LogData(ReportLevel.Success, "evidence", diffBmp);
                }
                else
                {
                    Report.Log(ReportLevel.Failure, "Validation", "画像比較に失敗しました。");
                    Report.LogData(ReportLevel.Info, "bmp1", bmp1);
                    Report.LogData(ReportLevel.Info, "bmp2", bmp2);
                    Report.LogData(ReportLevel.Error, "evidence", diffBmp);
                }
            }catch(System.ArgumentException e){
			    Report.Log(ReportLevel.Failure,"Failure","引数が不正です。画像Pathが間違っていないか確認してください。");
				Report.Log(ReportLevel.Failure,"LogData" , e.Message + Environment.NewLine +  e.StackTrace);
				
			}
			catch (FileNotFoundException e)
			{
			    // FileNotFoundException が発生した場合の処理
			    Report.Log(ReportLevel.Failure,"Failure","ファイルが存在しません。");
				Report.Log(ReportLevel.Failure,"LogData" , e.Message + Environment.NewLine +  e.StackTrace);
			}
            catch (Exception e)
            {
                Report.Log(ReportLevel.Failure, "LogData", e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        /// <summary>
        /// [Validate]
        /// 指定された2つのビットマップを比較し、結果を判定する
        /// 無視する領域を設定することができる
        /// </summary>
        /// <param name="bmp1Path">ビットマップ1のパス</param>
        /// <param name="bmp2Path">ビットマップ2のパス</param>
        /// <param name="resultPath">結果を出力するビットマップのパス</param>
        [UserCodeMethod]
        public static void ValidateCompareImageBitmapIgnore(string bmp1Path, string bmp2Path, string resultPath)
        {
            try
            {
                string compareResult = "no different";
                Bitmap bmp1 = new Bitmap(bmp1Path);
                Bitmap bmp2 = new Bitmap(bmp2Path);
                int width = Math.Max(bmp1.Width, bmp2.Width);
                int height = Math.Max(bmp1.Height, bmp2.Height);
                Bitmap diffBmp = new Bitmap(width, height);
                Color diffColor = Color.Red;
                Color ignoreColor = Color.Green;

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        if (CheckIgnorePoint(i, j))
                        {
                            diffBmp.SetPixel(i, j, ignoreColor);
                            continue;
                        }
                        try
                        {
                            Color color1 = bmp1.GetPixel(i, j);
                            Color color2 = bmp2.GetPixel(i, j);
                            int tolerance = 2;
                            if (Math.Abs(color1.R - color2.R) <= tolerance &&
                                Math.Abs(color1.G - color2.G) <= tolerance &&
                                Math.Abs(color1.B - color2.B) <= tolerance)
                            {
                                diffBmp.SetPixel(i, j, color1);
                            }
                            else
                            {
                                diffBmp.SetPixel(i, j, diffColor);
                                compareResult = "different";
                            }
                        }
                        catch
                        {
                            diffBmp.SetPixel(i, j, diffColor);
                            compareResult = "different";
                        }
                    }
                }

                diffBmp.Save(resultPath, ImageFormat.Png);
                if (compareResult == "no different")
                {
                    Report.LogData(ReportLevel.Success, "evidence", diffBmp);
                }
                else
                {
                    Report.Log(ReportLevel.Failure, "Validation", "画像比較に失敗しました。");
                    Report.LogData(ReportLevel.Error, "evidence", diffBmp);
                }
                //除外範囲のクラス変数を初期化する
                InitializationIgnoreRectangles();
            }catch(System.ArgumentException e){
			    Report.Log(ReportLevel.Failure,"Failure","引数が不正です。画像Pathが間違っていないか確認してください。");
				Report.Log(ReportLevel.Failure,"LogData" , e.Message + Environment.NewLine +  e.StackTrace);
				
			}
			catch (FileNotFoundException e)
			{
			    // FileNotFoundException が発生した場合の処理
			    Report.Log(ReportLevel.Failure,"Failure","ファイルが存在しません。");
				Report.Log(ReportLevel.Failure,"LogData" , e.Message + Environment.NewLine +  e.StackTrace);
			}
            catch (Exception e)
            {
                Report.Log(ReportLevel.Failure, "LogData", e.Message + Environment.NewLine + e.StackTrace);
            }
        }
      
        /// <summary>
        /// [Validate]
        /// 指定された2つのビットマップを比較し、結果を判定する
        /// 結果が同一の場合は何もせず、差分がある場合には差分であることを示すビットマップを出力する
        /// エラーではなく、指定した誤差範囲内であれば警告を出力することができる
        /// </summary>
        /// <param name="bmp1Path">ビットマップ1のパス</param>
        /// <param name="bmp2Path">ビットマップ2のパス</param>
        /// <param name="resultPath">結果を出力するビットマップのパス</param>
        /// <param name="warningThresholdPercent">警告を出す割合の文字列(5%という文字列で定義)</param>
        [UserCodeMethod]
        public static void ValidateCompareImageBitmapWarning(string bmp1Path, string bmp2Path, string resultPath,string warningThresholdPercent)
        {
            try
            {
            	double warningPercent;
            	if(!GetTryParsePercentage(warningThresholdPercent,out warningPercent)){
            		Report.Log(ReportLevel.Failure,"Invalid Input","入力値が異なります。");
            		return;
            	}
                // 2つの画像を読み込み、幅と高さを取得
                Bitmap bmp1 = new Bitmap(bmp1Path);
                Bitmap bmp2 = new Bitmap(bmp2Path);
                int width = Math.Max(bmp1.Width, bmp2.Width);
                int height = Math.Max(bmp1.Height, bmp2.Height);
                Bitmap diffBmp = new Bitmap(width, height);
                Color diffColor = Color.Red;
                
                int totalPixcels = width * height;
                int differentPixcels = 0;

                // ピクセルごとに比較し、異なる場合は差分画像にそのピクセルを設定
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        try
                        {
                            Color color1 = bmp1.GetPixel(i, j);
                            Color color2 = bmp2.GetPixel(i, j);
                            //許容誤差
                            int tolerance = 2;
                            if (Math.Abs(color1.R - color2.R) <= tolerance &&
                                Math.Abs(color1.G - color2.G) <= tolerance &&
                                Math.Abs(color1.B - color2.B) <= tolerance)
                            {
                                diffBmp.SetPixel(i, j, color1);
                                differentPixcels++;
                            }
                        }
                        catch
                        {
                            // 異なるサイズの場合は例外が発生するため、その場合は差分として扱う
                            diffBmp.SetPixel(i, j, diffColor);
                            differentPixcels++;
                        }
                    }
                }

                diffBmp.Save(resultPath, ImageFormat.Png);

                double differentPercentage = (double)differentPixcels / totalPixcels * 100;
                if(differentPercentage == 0){
                    Report.LogData(ReportLevel.Success, "evidence", diffBmp);
                    Report.Log(ReportLevel.Success,"Compare Result","Images are idential");
                }
                else if(differentPercentage <= warningPercent){
                    Report.LogData(ReportLevel.Success, "evidence", diffBmp);
                    Report.Log(ReportLevel.Warn,"Validation","画像に微細なずれ（数字部分など）がありますので画像を確認してください。");
                }
                else
                {
                    Report.Log(ReportLevel.Failure, "Validation", "画像比較に失敗しました。");
                    Report.LogData(ReportLevel.Error, "evidence", diffBmp);
                }
            }catch(System.ArgumentException e){
			    Report.Log(ReportLevel.Failure,"Failure","引数が不正です。画像Pathが間違っていないか確認してください。");
				Report.Log(ReportLevel.Failure,"LogData" , e.Message + Environment.NewLine +  e.StackTrace);
				
			}
			catch (FileNotFoundException e)
			{
			    // FileNotFoundException が発生した場合の処理
			    Report.Log(ReportLevel.Failure,"Failure","ファイルが存在しません。");
				Report.Log(ReportLevel.Failure,"LogData" , e.Message + Environment.NewLine +  e.StackTrace);
			}
            catch (Exception e)
            {
                Report.Log(ReportLevel.Failure, "LogData", e.Message + Environment.NewLine + e.StackTrace);
            }
        }

		/// <summary>
		/// 内部メソッド
		/// 引数で受け取った文字列がパーセント型式かを確認する
		/// </summary>
		/// <param name="input">対象文字列</param>
		/// <param name="result">結果格納</param>
		/// <returns>True：成功　False：失敗</returns>
        private static bool GetTryParsePercentage(string input,out double result){
        	input = input.Trim().TrimEnd('%');
        	if(double.TryParse(input,NumberStyles.Any,CultureInfo.InvariantCulture,out result)){
        		return result >= 0 && result <= 100;
        	}
        	return false;
        }

        /// <summary>
        /// ignoreRectanglesの初期化
        /// </summary>
		[UserCodeMethod]
        public static void InitializationIgnoreRectangles()
        {
            _ignoreRectangles = new List<System.Drawing.Rectangle>();
        }
		
        /// <summary>
        /// [内部メソッド]
        /// ignorePointであるかを確認する
        /// 除外範囲のクラス変数はこのクラスの上部で定義
        /// その除外範囲内に対象の座標が存在したい無いかを確認して結果を返す
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <returns>True:x,y座標が除外範囲内にいる False:x,y座標が除外範囲内にいない</returns>
        private static bool CheckIgnorePoint(int x, int y)
        {
            if (_ignoreRectangles == null || _ignoreRectangles.Count == 0)
                return false;

            bool returnVal = false;
            foreach (var rect in _ignoreRectangles)
            {
                returnVal = rect.Contains(x, y);
                if (returnVal)
                    return returnVal;
            }
            return false;
        }

        /// <summary>
        /// Windowsアプリ用
		/// テーブル内において、画像比較を除外する矩形領域を定義するメソッド
		/// ignoreRectangles引数に追加された矩形領域をValidateCompareImage方法の比較対象から除外することで
		/// 実際の対象物の特定部位のみの比較を実現する
		/// ignoreRectanglesを初期化して格納する。
		/// </summary>
		/// <param name="startRowIndex">開始する行番号</param>
		/// <param name="startColIndex">開始する列番号</param>
		/// <param name="endRowIndex">終了する行番号</param>
		/// <param name="endColIndex">終了する列番号</param>
		/// <param name="colRepo">列のリポジトリ</param>
		/// <param name="bodyRepo">比較対象のイメージの親リポジトリのオブジェクト</param>
		[UserCodeMethod]
		public static void AddIgnoreRectTableInitWin(int startRowIndex,
		                                          	 int startColIndex,
		                                             int endRowIndex,
		                                          	 int endColIndex,
		                                          	 RepoItemInfo colRepo,
		                                          	 RepoItemInfo bodyRepo)
		{
			if (bodyRepo == null)
				throw new ArgumentNullException("bodyRepo");
			Report.Info("debug","path=" + colRepo.Path.ToString());

			RepoItemInfo alphaPath = new RepoItemInfo(colRepo.ParentFolder, "alpha",
		        new RxPath(colRepo.Path.ToString().Replace("$colIndex", startColIndex.ToString()).Replace("$rowIndex", startRowIndex.ToString())),
		        new Duration(3000), null);
		        
		    //Bodyの親リポジトリの要素から開始行の矩形領域を取得
		    var body = bodyRepo.FindAdapter<Ranorex.Unknown>().Element.ScreenRectangle;
		    
		    repo.rowIndex = startRowIndex.ToString();
		    repo.colIndex = startColIndex.ToString();
		    var startRect = alphaPath.FindAdapter<Ranorex.Unknown>().Element.ScreenRectangle;

		    RepoItemInfo betaPath = new RepoItemInfo(colRepo.ParentFolder, "beta",
		        new RxPath(colRepo.Path.ToString().Replace("$colIndex", endColIndex.ToString()).Replace("$rowIndex", endRowIndex.ToString())),
		        new Duration(3000), null);
		        
		    //終了行の矩形領域を取得
		    repo.rowIndex = endRowIndex.ToString();
		    repo.colIndex = endColIndex.ToString();
		    var endRect = betaPath.FindAdapter<Ranorex.Unknown>().Element.ScreenRectangle;
		    
		    //クラス変数に初期化したうえで格納する
		    _ignoreRectangles = new List<System.Drawing.Rectangle>();
		    
		    //IgnoreRectangle領域を定義し、リストに追加
		    _ignoreRectangles.Add(new System.Drawing.Rectangle(startRect.X - body.X,
		                                                       startRect.Y - body.Y,
		                                                      endRect.X + endRect.Width - startRect.X,
		                                                      endRect.Y + endRect.Height - startRect.Y));
		}
		
		/// <summary>
		/// Webベース用
		/// テーブル内において、画像比較を除外する矩形領域を定義するメソッド
		/// ignoreRectangles引数に追加された矩形領域をValidateCompareImage方法の比較対象から除外することで
		/// 実際の対象物の特定部位のみの比較を実現する
		/// ignoreRectanglesを初期化して格納する。
		/// </summary>
		/// <param name="startRowIndex">開始する行番号</param>
		/// <param name="startColIndex">開始する列番号</param>
		/// <param name="endRowIndex">終了する行番号</param>
		/// <param name="endColIndex">終了する列番号</param>
		/// <param name="colRepo">列のリポジトリ</param>
		/// <param name="bodyRepo">比較対象のイメージの親リポジトリのオブジェクト</param>
		[UserCodeMethod]
		public static void AddIgnoreRectTableInitWeb(int startRowIndex,
		                                          	 int startColIndex,
		                                          	 int endRowIndex,
		                                          	 int endColIndex,
		                                          	 RepoItemInfo colRepo,
		                                          	 RepoItemInfo bodyRepo)
		{
			if (bodyRepo == null)
				throw new ArgumentNullException("bodyRepo");
			Report.Info("debug","path=" + colRepo.Path.ToString());

			RepoItemInfo alphaPath = new RepoItemInfo(colRepo.ParentFolder, "alpha",
		        new RxPath(colRepo.Path.ToString().Replace("$colIndex", startColIndex.ToString()).Replace("$rowIndex", startRowIndex.ToString())),
		        new Duration(3000), null);
		        
		    //Bodyの親リポジトリの要素から開始行の矩形領域を取得
		    var body = bodyRepo.FindAdapter<WebElement>().Element.ScreenRectangle;
		    
		    repo.rowIndex = startRowIndex.ToString();
		    repo.colIndex = startColIndex.ToString();
		    var startRect = alphaPath.FindAdapter<WebElement>().Element.ScreenRectangle;

		    RepoItemInfo betaPath = new RepoItemInfo(colRepo.ParentFolder, "beta",
		        new RxPath(colRepo.Path.ToString().Replace("$colIndex", endColIndex.ToString()).Replace("$rowIndex", endRowIndex.ToString())),
		        new Duration(3000), null);
		        
		    //終了行の矩形領域を取得
		    repo.rowIndex = endRowIndex.ToString();
		    repo.colIndex = endColIndex.ToString();
		    var endRect = betaPath.FindAdapter<WebElement>().Element.ScreenRectangle;
		    
		    //クラス変数に初期化したうえで格納する
		    _ignoreRectangles = new List<System.Drawing.Rectangle>();
		    
		    //IgnoreRectangle領域を定義し、リストに追加
		    _ignoreRectangles.Add(new System.Drawing.Rectangle(startRect.X - body.X,
		                                                       startRect.Y - body.Y,
		                                                      endRect.X + endRect.Width - startRect.X,
		                                                      endRect.Y + endRect.Height - startRect.Y));
		}
		
		/// <summary>
		/// 画像比較の除外範囲を設定するメソッド(WebElementのリポジトリアイテム1個指定)
		/// ignoreRectanglesリストに除外する矩形領域を追加することで
		/// 実際の対象物の特定部位のみの比較を実現する
		/// ignoreRectanglesを初期化しない。
		/// </summary>
		/// <param name="targetRepo">対象のリポジトリアイテム</param>
		/// <param name="bodyRepo">BODY部分のリポジトリアイテム(スクリーンRectangleを取得するにはBODYのリポジトリが必要となる)</param>
		[UserCodeMethod]
		public static void AddIgnoreRect(RepoItemInfo targetRepo, RepoItemInfo bodyRepo){
		    //Bodyの表示領域を取得する(この部分は静的取得とする)
		    var body = bodyRepo.FindAdapter<WebElement>().Element.ScreenRectangle;
		    //指定したリポジトリの表示領域を取得する
		    var repoRectangle = targetRepo.FindAdapter<WebElement>().Element.ScreenRectangle;
		 
		    // ignoreRectanglesが初期化状態かどうか確認(初期化していない場合は除外範囲が広がる)
		    _ignoreRectangles.Add(new System.Drawing.Rectangle(repoRectangle.X - body.X,
		                                                       repoRectangle.Y - body.Y,
		                                                       repoRectangle.Width,
		                                                       repoRectangle.Height));
		}
        /// <summary>
        /// 画像比較の除外範囲を指定するメソッド（リポジトリアイテム1個指定）
        /// ignoreRectanglesを初期化しないで格納するので、初めて使う場合は初期化必須
        /// 指定した後に、ValidateCompareImageBitmapIgnoreで比較することで除外範囲を利用した画像比較が可能
        /// </summary>
        /// <param name="targetRepo">対象のリポジトリアイテム(WebElementではない物)</param>
        /// <param name="baseRepo">スクリーンショットで指定しているリポジトリ</param>
        [UserCodeMethod]
        public static void AddIgnoreRectUnknown(RepoItemInfo targetRepo,RepoItemInfo baseRepo){
        	var baseRect = baseRepo.FindAdapter<Unknown>().Element.ScreenRectangle;
        	var repoRectangles = targetRepo.FindAdapter<Unknown>().Element.ScreenRectangle;
        	_ignoreRectangles.Add(new System.Drawing.Rectangle(repoRectangles.X - baseRect.X ,
        	                                                  repoRectangles.Y - baseRect.Y ,
        	                                                  repoRectangles.Width,
        	                                                  repoRectangles.Height));
        }    
		
		/// <summary>
		/// 座標をリポジトリの範囲で除外領域を指定する
		/// ignoreRectanglesを初期化せず格納する。
		/// </summary>
		/// <param name="startPointX">除外領域の開始点のX座標</param>
		/// <param name="startPointY">除外領域の開始点のY座標</param>
		/// <param name="endPointX">除外領域の終了点のX座標</param>
		/// <param name="endPointY">除外領域の終了点のY座標</param>
		/// <param name="baseRepo">基準となるリポジトリ項目</param>
		[UserCodeMethod]
		public static void AddIgnoreRectSetPos(int startPointX, int startPointY, int endPointX, int endPointY, RepoItemInfo baseRepo)
		{
		    Report.Info("Info","座標タイプの除外範囲を定義する");
		 
		    //Baseの表示領域を取得
		    var baseRect = baseRepo.FindAdapter<Unknown>().Element.ScreenRectangle;
		 
		    var startX = baseRect.X + startPointX;
		    var startY = baseRect.Y + startPointY;
		    var endX = startX + endPointX;
		    var endY = startY + endPointY;
		 
		    Report.Info("Info", "除外領域の座標範囲: " + baseRect.X.ToString() + ", " + baseRect.Y.ToString());
		 
		    //除外範囲を追加
		    _ignoreRectangles.Add(new System.Drawing.Rectangle(startX, startY, endX - startX, endY - startY));
		}

    }
}
