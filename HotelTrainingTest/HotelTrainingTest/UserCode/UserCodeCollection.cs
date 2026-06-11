/*
 * Created by Ranorex
 * User: k_hattori
 * Date: 2025/01/07
 * Time: 15:06
 * 
 * To change this template use Tools > Options > Coding > Edit standard headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.IO;
using System.Threading;
using WinForms = System.Windows.Forms;

using Ranorex;
using Ranorex.Core;
using Ranorex.Core.Reporting;
using Ranorex.Core.Testing;
using Ranorex.Core.Controls;
using Ranorex.Core.Repository;

namespace HotelTrainingTest.UserCode
{
    /// <summary>
    /// ユーザー コード ライブラリにユーザー コード メソッドを公開するためのファイルです。
    /// </summary>
    [UserCodeCollection]
    public class UserCodeCollection
    {
        // UserCode基礎のお試しメソッド
        [UserCodeMethod]
        public static int CalcNumAdd(int x, int y)
        {
        	return x+y;
        }
        
        /// 練習1-1:文字列結合
        /// 文字列型変数x(hoge)
        /// 文字列型変数y(fuga)
        /// 上記の2つの変数を結合して出力する
        [UserCodeMethod]
        public static string ConcatenationStr(string x, string y)
        {
        	return x + y;
        }
        /// <summary>
        /// This is a placeholder text. Please describe the purpose of the
        /// user code method here. The method is published to the user code library
        /// within a user code collection.
        /// </summary>
        [UserCodeMethod]
        public static string PathCombine(string x, string y)
        {
        	return Path.Combine(x, y);
        }
        
        /// 練習1-2：今日の日付を取得
        /// システム日付を文字列型で取得する
        /// モジュールにてログメッセージで表示する
        [UserCodeMethod]
        public static string GetToday()
        {
        	return System.DateTime.Now.ToString();
        }
        
        /// 練習1-3：日付のコントロール
        /// 1.練習1-2で取得した日付の表示形式を変更
        ///   returnの()内で表示形式を指定する
        /// 2.システム日付の翌日の値を1の形式で出力する
        /// 　AddDayを使用して翌日にする
        [UserCodeMethod]
        public static string DateControl()
        {
        	System.DateTime today = System.DateTime.Now;
        	System.DateTime tommrow = today.AddDays(1);
        	return tommrow.ToString("yyyy/MM/dd");
        }
        
        /// 練習1-4：日付実践
        /// 1-1.システム日付の翌日意向で月曜日になる日付を取得
        /// 1-2.日付の出力形式をユーザーが引数で指定できる(引数1)
        /// 1-3.1-1で指定した日付からN日後を指定できる(引数2) int addDay
        /// within a user code collection.
        [UserCodeMethod]
        public static string DateTraining(string format)
        {
        	System.DateTime today = System.DateTime.Now;
        	System.DateTime nextDay = today.AddDays(1);
        	
        	//次の月曜日になるまで1日ずつ足していく
        	while (nextDay.DayOfWeek != DayOfWeek.Monday) 
        	{
        		nextDay = nextDay.AddDays(1);
        	}
        	
        	//引数addDayの数だけ日数を増やす
        	//System.DateTime dayAfter = nextDay.AddDays(addDay);
        	//引数formatの形で日付を表示する
        	return nextDay.ToString(format);
        }
        
        /// 次の土曜日を引数のフォーマット通りに出力する
        [UserCodeMethod]
        public static string GetNextSaturday(string format)
        {
        	System.DateTime today = System.DateTime.Now;
        	System.DateTime nextDay = today.AddDays(1);
        	
        	while (nextDay.DayOfWeek != DayOfWeek.Saturday) 
        	{
        		nextDay = nextDay.AddDays(1);
        	}
        	return nextDay.ToString(format);
        }
        
        /// 日付を組み合わせるメソッド
        /// 引数D1、D2に日付を格納し"~"でつなげて1つの文字列にするメソッド
        [UserCodeMethod]
        public static string DateConcatenation(string D1, string D2)
        {
        	return D1 + "·〜·" + D2;
        }
        
        /// 練習2-5：チェックボックスクリック
        /// 初期入力値と引数によってクリックするしない決めるメソッド
        /// 要素からInputTagのCheckedを持ってきて、
        /// 引数との組み合わせで実行する操作を決める
        /// 初期入力値：True、引数：ON→クリックしない
        /// 初期入力値：True、引数：OFF→クリックする
        /// 初期入力値：False、引数：ON→クリックする
        /// 初期入力値：False、引数：OFF→クリックしない
        /// クリックする組み合わせをifで判定
        [UserCodeMethod]
        public static void InputCheckBox(RepoItemInfo repoItemInfo, string inputvalue)
        {
        	var targetItem = repoItemInfo.CreateAdapter<WebElement>(false);
        	var stateCheckBox = targetItem.Element.GetAttributeValueText("Checked");
        	
        	if ((inputvalue.Equals("ON") && stateCheckBox.Equals("False")) || (inputvalue.Equals("OFF") && stateCheckBox.Equals("True")))
        	{
        		targetItem.Click();
        		Report.Log(ReportLevel.Info, "Input", "Click CheckBox " + repoItemInfo + ".");
        	}
        }
        
        /// 練習2-6：プルダウンリストを選択
        /// 「性別」プルダウンリストを引数で指定した性別を選択するメソッド
        [UserCodeMethod]
        public static void SelectDispValueContains(RepoItemInfo selecttagInfo, string targetDispValue)
        {
    		string TagVal = "";
    		bool findTarget = false;
    		
    		//IEnumerable<Element>optionListでリスト型の変数optionListを宣言している（リストの中を構成する値はElement型（Ranorexのリポジトリアイテム））
    		//リストの宣言はいろいろ（ICollection、IList）あるが、IEnumerableで宣言するとで参照のみでforeachを使うのだなと読み手に推測させる（＝可読性の向上）につながる
    		IEnumerable<Element>optionList = selecttagInfo.FindAdapter<SelectTag>().Element.Find(".//Option", 30000);
    		
    		//optionListの中身を一つずつ確認、Ranorex.OptionTag型としてキャストすることで属性値を取得可能
    		foreach(Ranorex.OptionTag opt in optionList)
    		{
    			//<option>要素のLabel属性がある場合にその値を取得、ないときはnull
    			var labelValue = opt.GetAttributeValue<string>("Label");
    			//<option>要素の表示されるテキスト(InnerText)を取得
    			var innerTextValue = opt.GetAttributeValue<string>("InnerText");
    			//選択肢のvalue属性を取得して、後の選択処理で使用する
    			//ループで一致した<option>のTagValを使用する
    			TagVal = opt.GetAttributeValue<string>("Value");
    			
    			//Labelが存在し、Labelに入力値の値が含まれていて、入力値の値が空欄でないとき
    			if(labelValue != null && labelValue.Contains(targetDispValue) && targetDispValue.Trim() != "")
    			{
    				findTarget = true;
    				break;
    			}
    			
    			//InnerTextが存在し、InnerTextに入力値が含まれていて、入力値の値が空欄でないとき
    			else if(innerTextValue != null && innerTextValue.Contains(targetDispValue) && targetDispValue.Trim() != "")
    			{
    				findTarget = true;
    				break;
    			}
    			
    			//Labelが存在せず、ユーザーが空文字を指定しているとき（プルダウンリストのデフォルト値など、何も入っていない、選択していない状態）
    			else if(labelValue == null && targetDispValue.Trim() == "")
    			{
    				findTarget = true;
    				break;
    			}
    		}
    		
    		//前のループで一致したかどうかで処理を分岐
    		//一致するときにTagValで取得したValue値をセットすることで、プルダウンリストの値を選択する
    		if (findTarget == true)
    		{
    			Report.Log(ReportLevel.Info, "Set Value", "Setting attribute Value to " + targetDispValue + " on item" + selecttagInfo + ".");
    			selecttagInfo.FindAdapter<SelectTag>().Element.SetAttributeValue("TagValue", TagVal);
    		}
    		
    		//前のループで入力値がに該当するLabel、InnerTextが見つからない場合のエラーハンドリング
    		else if(targetDispValue.Trim() != "")
    		{
    			Validate.Fail("Set Value selecttingInfo Not found " + targetDispValue + ".");
    		}
        }
        
        /// 練習2-7：入力値に応じて処理をスキップする
        /// 各種テキストボックスについて入力値がある時はSetValueで入力するが、
        /// 値が＜ignore＞の時に入力処理をスキップするメソッド
        /// ifで条件分岐
        [UserCodeMethod]
        public static void InputValueIgnore(RepoItemInfo inputtagInfo, string inputValue)
        {
        	if (inputValue == "<ignore>") 
        	{
        		Report.Log(ReportLevel.Info, "Set value", "入力スキップ(<ignore>)");
        	}
        	else
        	{
        		Report.Log(ReportLevel.Info, "Set value", $"Setting attribute Value to " + inputValue + " on item " + inputtagInfo + ".");
    			inputtagInfo.FindAdapter<InputTag>().Element.SetAttributeValue("Value", inputValue);
        	}
        }
        
        /// 練習2-8：汎用入力まとめ
        /// 練習2で作成したメソッドを適切に呼び出すことで
        /// １つのメソッドにリポジトリアイテムと入力値を渡すことで以下の入力ができるようにする
        /// テキストボックス・プルダウンリスト・チェックボックス
        [UserCodeMethod]
        public static void InputIfIsNotIgnore(RepoItemInfo repoItemoInfo, string inputValue)
        {
        	if (inputValue == "<ignore>") 
        	{
        		Report.Log(ReportLevel.Info, "Set value", "「" + repoItemoInfo + "」の入力をスキップ");
        		return;
        	}
        	var targetItem = repoItemoInfo.CreateAdapter<Unknown>(false);
        	var adapterType = targetItem.GetAttributeValue<string>("type");
        	
        	//UI要素の属性値によって処理内容を分岐
			switch (adapterType) 
			{
				case "email":
				case "date":
				case "number":
				case "password":
				case "text":
				case "textarea":
				case "tel":
					targetItem.Element.SetAttributeValue("value",inputValue);
					Report.Log(ReportLevel.Info, "Input", " Set Value "+ inputValue + " to item " + repoItemoInfo + ".");
					break;
				case "select-one":
					SelectDispValueContains(repoItemoInfo, inputValue);
					break;
				case "checkbox":
					InputCheckBox(repoItemoInfo, inputValue);
					break;
				case "radio":
					targetItem.Click();
					Report.Log(ReportLevel.Info, "Input", "Turn on radio button " + repoItemoInfo + ".");
					break;
				default:
					Report.Log(ReportLevel.Info, "Input", "Target item " + repoItemoInfo + " is not input.");
					break;
			}        	
        }
        
        //Windowsアプリ用汎用入力
       [UserCodeMethod]
	    public static void InputIfIsNotIgnore_WindowsApp(RepoItemInfo repoItemInfo, string inputValue)
		{
		    if (inputValue == "<ignore>")
		    {
		        Report.Log(ReportLevel.Info, "Set value", $"「{repoItemInfo}」の入力をスキップ");
		        return;
		    }
		
		    // Text
		    try
		    {
		        var textElement = repoItemInfo.CreateAdapter<Text>(false);
		        textElement.TextValue = inputValue;
		        Report.Log(ReportLevel.Info, "Input", $"Set TextValue '{inputValue}' to {repoItemInfo}.");
		        return;
		    }
		    catch {}
		
		    // ComboBox
		    try
		    {
		        var comboBox = repoItemInfo.CreateAdapter<ComboBox>(false);
		        comboBox.SelectedItemText = inputValue;
		        Report.Log(ReportLevel.Info, "Input", $"Selected '{inputValue}' in ComboBox {repoItemInfo}.");
		        return;
		    }
		    catch {}
		
		    // CheckBox
		    try
		    {
		        var checkBox = repoItemInfo.CreateAdapter<CheckBox>(false);
		        bool shouldCheck = inputValue.ToLower() == "true" || inputValue == "1" || inputValue == "on";
		        checkBox.Checked = shouldCheck;
		        Report.Log(ReportLevel.Info, "Input", $"Set CheckBox {repoItemInfo} to {checkBox.Checked}.");
		        return;
		    }
		    catch {}
		
		    // RadioButton
		    try
		    {
		        var radioButton = repoItemInfo.CreateAdapter<RadioButton>(false);
		        radioButton.Select();
		        Report.Log(ReportLevel.Info, "Input", $"Selected RadioButton {repoItemInfo}.");
		        return;
		    }
		    catch {}
		
		    // Button
		    try
		    {
		        var button = repoItemInfo.CreateAdapter<Button>(false);
		        button.Click();
		        Report.Log(ReportLevel.Info, "Input", $"Clicked Button {repoItemInfo}.");
		        return;
		    }
		    catch {}
		
		    Report.Log(ReportLevel.Warn, "Input", $"Unsupported element type for {repoItemInfo}. No action taken.");
		}

        /// 練習4-10：カレント情報を取得する
        /// 練習4-11：テストケース名に応じたディレクトリ作成
        /// レコーディングモジュールを配置した場所を起点とし。
        /// プロジェクト名、テストスイート名、テストケース名をログに出力する
        [UserCodeMethod]
        public static void GetCurrentName()
        {
        	var projectName = Directory.GetParent(TestSuite.WorkingDirectory).Parent.Name;
        	//var projectName = Path.GetFileName(Directory.GetParent(Directory.GetParent(TestSuite.WorkingDirectory).ToString()).ToString());
        	var testSuiteName = TestSuite.Current.Name;
        	var testCaseName = TestSuite.CurrentTestContainer.Name;
        	
        	Report.Log(ReportLevel.Info, "User", projectName);
        	Report.Log(ReportLevel.Info, "User", testSuiteName);
        	Report.Log(ReportLevel.Info, "User", testCaseName);
        	
        	var now = System.DateTime.Now.ToString("yyyyMMddHHmmss");
        	var folderPath = Path.Combine("C:/evidence", projectName, now, testSuiteName, testCaseName);
            Directory.CreateDirectory(folderPath);
        	
        	Report.Log(ReportLevel.Info, "User", $"ディレクトリ作成完了{folderPath}");
        }
        
        /// 練習3-9：データソースフィルタ
        /// パターンIDでフィルターして参照するデータを絞り込む
        [UserCodeMethod]
        public static void DataFilter(string folderName, string dataNo, string targetColmunName)
        {
        	//対象のスマートフォルダ名を指定する​
            var CurrentTestCase = TestSuite.Current.GetTestContainer(folderName);
            
            //データセットの列を取得​
            Ranorex.Core.Data.ColumnCollection cols = CurrentTestCase.DataContext.Source.Columns;
            int colindex = cols.IndexOf(targetColmunName);
            
            //データセットの行を取得​
            Ranorex.Core.Data.RowCollection rows = CurrentTestCase.DataContext.Source.Rows;
            
            //データセットのレンジを格納
            //一致した行番号をカンマ区切りで格納するための文字列変数​
            string rangeString="";
            //行番号をカウントするための変数
            int i=0;
            //対象のdataNoが含まれる行を1行目から検索し、範囲を格納する
            foreach (Ranorex.Core.Data.Row element in rows)
            {
            	i++;
            	if (element.Values[colindex]==dataNo)
            	{
            		//一致する行数とカンマを追加
            		rangeString =rangeString + i.ToString()+",";
            	}

            }
            
            //１件もヒットしない場合はエラーとしておく（データ範囲が０）​
            if (rangeString.Length == 0)
            {
            	throw new Ranorex.ValidationException("対象データなし");
            }
            
            //データ範囲を格納したときに生じる末尾のカンマ(,)を削除​
            rangeString = rangeString.Remove(rangeString.Length-1);
            
            //抽出した範囲をセットする​
            DataRangeSet new_data_range=DataRangeSet.Parse(rangeString);
            CurrentTestCase.DataContext.SetRange(new_data_range);
        }
        
        /// 指定の要素がデータソースの値と一致するかの検証
        [UserCodeMethod]
        public static void ValidateTextboxAttributeEquals(RepoItemInfo textboxItem,string attributeName, string inputValue)
        {
			// 入力欄のアダプターを取得
        	var element = textboxItem.CreateAdapter<Unknown>(false);
        		
        	// 'value' 属性の取得
        	var attributeValue = element.Element.GetAttributeValueText(attributeName);
				    
			// ログに出力
    		Report.Log(ReportLevel.Info,$"テキストボックス '{textboxItem.Name}' の属性 '{attributeName}' の値: '{attributeValue}'");

		    // 空欄かどうかの検証
		    Validate.AreEqual(inputValue, attributeValue,
		        $"入力欄 '{textboxItem.Name}'の値が期待値 '{inputValue}' と一致することを確認");
        }
        
        /// 指定の要素がnullかどうかの検証
        [UserCodeMethod]
        public static void ValidateTextboxesAreEmpty(RepoItemInfo textboxItem,string attributeName)
        {
			// 入力欄のアダプターを取得
        	var element = textboxItem.CreateAdapter<Unknown>(false);
        		
        	// 'value' 属性の取得
        	var attributeValue = element.Element.GetAttributeValueText(attributeName);
				    
			// ログに出力
    		Report.Log(ReportLevel.Info,$"テキストボックス '{textboxItem.Name}' の属性 '{attributeName}' の値: '{attributeValue}'");
    		
    		// 空欄かどうかの検証
		    Validate.IsTrue(string.IsNullOrEmpty(attributeValue),
		        $"入力欄 '{textboxItem.Name}' が空欄であることを確認");  
        }
        /// Dataフォルダまでのパスを取得
        [UserCodeMethod]
        public static string GetDataFolderPath()
        {

			// 実行ファイルのディレクトリを取得（例：bin\Debug）
    		string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

    		// プロジェクトルートに戻る（2階層上）
    		string projectRoot = Directory.GetParent(exeDirectory).Parent.FullName;

        	// Dataフォルダのパスを結合
        	string dataFolderPath = Path.Combine(projectRoot, "Data");
        	
        	Report.Log(ReportLevel.Info, "Data Folder Path", dataFolderPath);
        	return dataFolderPath;
        }
        /// 条件に沿って、合計金額を更新する
        [UserCodeMethod]
        public static void UpdateTotalBill()
        {
        	
        }
    }
}
