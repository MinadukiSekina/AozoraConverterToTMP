using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

namespace Minadukitei.Products
{
    /// <summary>青空文庫形式をベースとしたtxtファイルをTMP向けの縦書きに変換します。外部ライブラリを使用。/summary>
    public static class AozoraConverterToTMP
    {
        /// <summary>txtファイルから読み込み、変換を行います。</summary>
        /// <param name="lineMaxLength">１行の最大文字数。</param>
        /// <param name="lineMaxCount">１ページの最大行数。</param>
        /// <param name="lineHight">フォントアセットのLinheHight。</param>
        /// <param name="pointSize">フォントアセットのPointSize。</param>
        /// <param name="filePath">対象ファイルのパス。</param>
        /// <param name="encoding">ファイルのエンコーディング。</param>
        /// <returns>ページ単位に構成した文字列の配列。</returns>
        public static string[] GetConvertText(int lineMaxLength, int lineMaxCount, float lineHight, int pointSize, string filePath, Encoding encoding, bool rotateOption)
        {
            List<AozoraToken> aozoraTokens;
            List<string> pageList = new List<string>();
            AozoraLexer  lexer    = new AozoraLexer();
            AozoraParser parser   = new AozoraParser(lineMaxLength, lineMaxCount, lineHight, pointSize, rotateOption);

            using(StreamReader streamReader = new StreamReader(filePath, encoding, true))
            {
                while (streamReader.EndOfStream == false)
                {
                    aozoraTokens = lexer.GetAozoraTokens(streamReader.ReadLine());
                    parser.Parse(aozoraTokens, ref pageList);
                }
            }
            return pageList.ToArray();
        }
    }
}
