/*
 * Created by Ranorex
 * User: k_hattori
 * Date: 2025/07/23
 * Time: 15:43
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
using Ranorex.Core.Testing;

namespace HotelTrainingTest.UserCode
{
    /// <summary>
    /// ユーザー コード ライブラリにユーザー コード メソッドを公開するためのファイルです。
    /// </summary>
    [UserCodeCollection]
    public class UserCodeCollection_textCompare
    {
    	
    	/// <summary>
    	/// [プライベート]ファイルが存在するかをチェックする
    	/// </summary>
    	/// <param name="filePaths">対象のファイルパス(複数指定可能)</param>
		/// <returns>存在チェックの結果、true/falseで返す</returns>
    	[UserCodeMethod]
    	private static bool FilesExist(params string[] filePaths)
    	{
    		bool filesExist = true;
    		foreach (string filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    Report.Error("The file '" + filePath + "' does not exist.");
                    filesExist = false;
                }
            }
    		return filesExist;
    	}
    	
    	/// <summary>
    	///テキストファイルを比較し、完全一致するかを確認する
    	/// </summary>
    	/// <param name="filePath1">比較ファイルひとつめ</param>
		/// <param name="filePath2">比較ファイルふたつめ</param>
		/// <param name="encodingType">CSVの文字コード(Shift_JIS,UTF-8など)</param>
    	[UserCodeMethod]
    	public static void ValidateFilesTextEqual(string filePath1, string filePath2, string encodingType)
    	{
    		if (!FilesExist(filePath1, filePath2))
            {
            	Report.Failure("Files are not exist.");
                return;
            }
    		
    		var newLineRegexPattern = "(\r\n)|(\n)|(\r)";
    		
    		//改行コードを統一する
    		var fileContent1 = File.ReadAllText(filePath1, System.Text.Encoding.GetEncoding(encodingType));
    		fileContent1 = Regex.Replace(fileContent1, newLineRegexPattern, "\r\n");
    		fileContent1 = Regex.Replace(fileContent1, "\"", "");
    		
    		var fileContent2 = File.ReadAllText(filePath2, System.Text.Encoding.GetEncoding(encodingType));
    		fileContent2 = Regex.Replace(fileContent2, newLineRegexPattern, "\r\n");
    		fileContent2 = Regex.Replace(fileContent2, "\"", "");
    		
    		if (fileContent1 != fileContent2)
            {
                Report.Failure("Files '" + filePath1 + "' and '" + filePath2 + "' are not equal.");
                return;
            }
    		Report.Success("Files '" + filePath1 + "' and '" + filePath2 + "' are equal.");
    	}
    }
}
