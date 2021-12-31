using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;

namespace Minadukitei.Products
{
    /// <summary>String系の拡張メソッドや正規表現処理用のクラス。</summary>
    public static class StringExtensions
    {
        /// <summary>Shift_JISのエンコーディングオブジェクト。</summary>
        public static Encoding shift_JisEncoding = Encoding.GetEncoding(932);
        /// <summary>漢字判定用。</summary>
        public static Regex regexKanji = new Regex(@"[\p{IsCJKUnifiedIdeographs}\p{IsCJKCompatibilityIdeographs}\p{IsCJKUnifiedIdeographsExtensionA}\uD840-\uD869\uDC00-\uDFFF\uD869\uDC00-\uDEDF]+", RegexOptions.Compiled);
        /// <summary>アルファベット判定用。</summary>
        public static Regex regexAlphabet = new Regex(@"[a-zA-Zａ-ｚＡ-Ｚ]+", RegexOptions.Compiled);
        /// <summary>数値（全半角）判定用。</summary>
        public static Regex regexNumber = new Regex(@"\d+", RegexOptions.Compiled);
        /// <summary>カタカナ判定用。</summary>
        public static Regex regexKatakana = new Regex(@"^[\p{IsKatakana}\u31F0-\u31FF\u3099-\u309C\uFF65-\uFF9F]+", RegexOptions.Compiled);
        /// <summary>ひらがな判定用。</summary>
        public static Regex regexHiragana = new Regex(@"\p{IsHiragana}+", RegexOptions.Compiled);
        /// <summary>傍点の開始注記の検出用。</summary>
        public static Regex regexBoutenStart = new Regex(@"［＃(?:左に)*?(?:白ゴマ|丸|白丸|黒三角|白三角|二重丸|蛇の目|ばつ)*?傍点］", RegexOptions.Compiled);
        /// <summary>傍点の終了注記の検出用。</summary>
        public static Regex regexBoutenEnd = new Regex(@"［＃(?:左に)*?(?:白ゴマ|丸|白丸|黒三角|白三角|二重丸|蛇の目|ばつ)*?傍点終わり］", RegexOptions.Compiled);
        /// <summary>回転させない文字の検出用。</summary>
        public static Regex regexLandscape = new Regex(@"[a-zA-Z0-9）,)\]｝〕〉》」』】〙〗〟’”｠»‐―￣－\-゠–!?‼⁇⁈⁉$\\_＿/＼／{}<>＜＞.〜:;/'^*+-=＝…‥：；［］（([｛〔〈《「『【〘〖〝‘“｟«""]+", RegexOptions.Compiled);
        /// <summary>回転させない文字の検出用。長母音。</summary>
        public static Regex regexLandscape2 = new Regex(@"[ー～]+", RegexOptions.Compiled);
        /// <summary>回転させる文字の検出用。</summary>
        public static Regex regexNonLandscape = new Regex(@"[^a-zA-Z0-9）,)\]｝〕〉》」』】〙〗〟’”｠»‐―￣ー－\-゠–!?‼⁇⁈⁉$\\_＿/＼／{}<>＜＞.〜～:;/'^*+-=＝…‥：；［］（([｛〔〈《「『【〘〖〝‘“｟«""]+", RegexOptions.Compiled);
       /// <summary>余分な回転タグの検出用。</summary>
        public static Regex regexRotateRemover = new Regex(@"<rotate=90>(?<Tag>(<[^<>]+>)*)<rotate=0>", RegexOptions.Compiled);
        /// <summary>位置調整する文字の検出用。</summary>
        public static Regex regex01em = new Regex(@"[ィゥェォャヵヶぁぅゕゖゃゅょゎㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇷ゚ㇺㇻㇼㇽㇾㇿ]+", RegexOptions.Compiled);
        /// <summary>位置調整する文字の検出用。</summary>
        public static Regex regex02em = new Regex(@"[ァッュョヮぃぇぉっ]+", RegexOptions.Compiled);
        /// <summary>位置調整する文字の検出用。</summary>
        public static Regex regex05em = new Regex(@"[、。]+", RegexOptions.Compiled);
        /// <summary>後置の場合の傍点検出用。</summary>
        public static Regex regexBoutenChecker = new Regex(@"［＃「(?<Target>[^「」]+?)」(?: の左)*?に(?:白ゴマ|丸|白丸|黒三角|白三角|二重丸|蛇の目|ばつ)*?傍点］", RegexOptions.Compiled);

        /// <summary>行頭禁止文字。</summary>
        public static string NotLineHeadStrings = "）,)]｝、〕〉》」』】〙〗〟’”｠»ゝゞーァィゥェォッャュョヮヵヶぁぃぅぇぉっゃゅょゎゕゖㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇷ゚ㇺㇻㇼㇽㇾㇿ々〻‐゠–〜～?!‼⁇⁈⁉・:;/。.'";
        /// <summary>行末禁止文字。</summary>
        public static string NotLineEndString = "（([｛〔〈《「『【〘〖〝‘“｟«";

        private static Dictionary<char, char> toHalfDictionary = new Dictionary<char, char>() { { '０', '0' }, { '１', '1' }, { '２', '2' }, { '３', '3' }, { '４', '4' }, { '５', '5' }, { '６', '6' }, { '７', '7' }, { '８', '8' }, { '９', '9' } };

        /// <summary>
        /// バイト数とStringInfoの長さの内、長い方を返します。：全角＝２文字、半角１文字のつもりです。
        /// </summary>
        /// <param name="baseString">長さを取得する元の文字列。</param>
        /// <returns>文字列の長さ</returns>
        public static int Size(this string baseString)
        {
            StringInfo stringInfo = new StringInfo(baseString);
            int byteCount = shift_JisEncoding.GetByteCount(baseString);
            int elementCount = stringInfo.LengthInTextElements;

            return byteCount > elementCount ? byteCount : elementCount;
        }

        /// <summary>全角の数字を半角に変換します。</summary>
        /// <param name="baseString">変換元の文字列。</param>
        /// <returns>半角に変換した文字列。</returns>
        public static string ConvertHalf(this string baseString)
        {
            return regexNumber.Replace(baseString, ReplaceToHalf);

        }
        private static string ReplaceToHalf(Match match)
        {
            return new string(match.Value.Select(c => toHalfDictionary[c]).ToArray());
        }

        /// <summary>対象文字列のStringInfoを返します。</summary>
        /// <param name="baseString">対象の文字列。</param>
        /// <returns>対象文字列のStringInfo。</returns>
        public static StringInfo GetStringInfo(this string baseString)
        {
            return new StringInfo(baseString);
        }

        /// <summary>余分な回転タグを削除します。</summary>
        /// <param name="baseString">対象の文字列。</param>
        /// <returns>余分な回転タグを削除した後の文字列。</returns>
        public static string FixReplace(this string baseString)
        {
            return regexRotateRemover.Replace(baseString, @"${Tag}");
        }

        /// <summary>促音などの位置を調整するタグを追加します。ルビの処理が行われなくなるため、ルビ仮名には使用しないでください。</summary>
        /// <param name="baseString">対象の文字列。</param>
        /// <returns>促音などを位置調整のタグで挟んだ文字列。</returns>
        public static string AdjustReplace(this string baseString)
        {
            return regex05em.Replace(regex02em.Replace(regex01em.Replace(baseString, @"<voffset=0.1em>$0</voffset>"), @"<voffset=0.2em>$0</voffset>"), @"<voffset=0.5em>$0</voffset>");
        }

        /// <summary>横組みなどの回転させてはいけない部分だけ回転を無効にし、その後回転を戻すタグを追加します。</summary>
        /// <param name="baseString">対象の文字列。</param>
        /// <param name="isRotate">呼び出し時における回転のモード。通常はtrue、横組み指示中などで回転が無効の場合はfalse。</param>
        /// <returns>タグを追加した後の文字列。</returns>
        public static string ChangeRotate(this string baseString, bool isRotate)
        {
            if (isRotate == false) return baseString;

            return regexLandscape.Replace(baseString, @"<rotate=0>$0<rotate=90>");
        }

        /// <summary>長母音を回転させないようにタグを追加します。ルビ処理後に呼び出す想定です。</summary>
        /// <param name="baseString">対象の文字列。</param>
        /// <param name="isRotate">呼び出し時における回転のモード。通常はtrue、横組み指示中などで回転が無効の場合はfalse。</param>
        /// <returns>タグを追加した後の文字列。</returns>
        public static string ChangeRotate2(this string baseString, bool isRotate)
        {
            if (isRotate == false) return baseString;

            return regexLandscape2.Replace(baseString, @"<rotate=0>$0<rotate=90>");
        }
    }
}

