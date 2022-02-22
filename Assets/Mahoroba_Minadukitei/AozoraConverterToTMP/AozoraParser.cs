using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;

namespace Minadukitei.Products
{
    /// <summary>トークンを受け取って変換するクラス。</summary>
    public class AozoraParser
    {
        /// <summary>指定された最大文字数・最大行数でページを生成・処理するパーサ。</summary>
        /// <param name="lineMaxLength">一行の最大文字数。</param>
        /// <param name="lineMaxCount">一ページの最大行数。</param>
        /// <param name="lineHight">フォントアセットの一行の高さ。</param>
        /// <param name="pointSize">フォントアセットのPointSize。</param>
        public AozoraParser(int lineMaxLength, int lineMaxCount, float lineHight, int pointSize, bool rotateOption)
        {
            CurrentLineLength = 0;
            CurrentLineCount  = 0;
            CurrentPageCount  = 0;
            IsPageFirstAdd    = true;
            IsCenterAlign     = false;
            IsRotate          = true;
            IsRotate2         = rotateOption;
            IsBouten          = false;
            LineMaxLength     = lineMaxLength * 2;
            LineMaxCount      = lineMaxCount;
            FixLineHight = (lineHight / pointSize) * 1.1f;
            TextBuilder.Clear();
        }

        /// <summary>文字種の一覧。</summary>
        private enum EStringType
        {
            AnyType,
            Hiragana,
            Katakana,
            Kanji,
            Number,
            Alphabet,
            NotKnown
        }
        /// <summary>現時点での文字数。バイト換算。</summary>
        private int CurrentLineLength { get; set; }
        /// <summary>現時点での行数。</summary>
        private int CurrentLineCount { get; set; }
        /// <summary>現時点でのページ数。</summary>
        private int CurrentPageCount { get; set; }
        /// <summary>コンストラクタで受け取った、一行の最大文字数。バイト換算。</summary>
        private int LineMaxLength { get; set; }
        /// <summary>コンストラクタで受け取った、一ページの最大行数。</summary>
        private int LineMaxCount { get; set; }
        /// <summary>字下げする文字数。</summary>
        private int JisageCount { get; set; }
        /// <summary>折り返し時の字数。</summary>
        private int OrikaesiCount { get; set; }
        /// <summary>地寄せの場合に上げる文字数。</summary>
        private int JiageCount { get; set; }
        /// <summary>改行タグを追加するかどうかの判断用。</summary>
        private bool IsMakeNewLine { get; set; }
        /// <summary>現在のページへの初めての追加かどうか。</summary>
        private bool IsPageFirstAdd { get; set; }
        /// <summary>読み込んだ行の初めての追加かどうか。</summary>
        private bool IsLineFirstAdd { get; set; }
        /// <summary>改行しているかどうか。</summary>
        private bool IsLineBreaked { get; set; }
        /// <summary>地寄せかどうか。TMProを90度回転させること前提。</summary>
        private bool IsRightAlign { get; set; }
        /// <summary>後の行も地寄せするかどうか。</summary>
        private bool IsRightAlignContinuity { get; set; }
        /// <summary>後の行も字下げするかどうか。</summary>
        private bool IsJisageContinuity { get; set; }
        /// <summary>後の行も字上げするかどうか。</summary>
        private bool IsJiageContinuity { get; set; }
        /// <summary>回転させるかどうか。縦書きにするためにはTrue。</summary>
        private bool IsRotate { get; set; }
        /// <summary>縦書きならTrue、横書きはFalse。</summary>
        public bool IsRotate2 { get; set; }
        /// <summary>傍点を振るかどうか。</summary>
        private bool IsBouten { get; set; }
        /// <summary>中央寄せにするかどうか。</summary>
        private bool IsCenterAlign { get; set; }
        /// <summary>現在処理している行の、txtファイル内での位置要素。</summary>
        private AozoraToken.ElementType CurrentElement { get; set; }

        private StringBuilder textBuilder;
        /// <summary>ページを構成する文字列を作成するためのビルダー。</summary>
        private StringBuilder TextBuilder
        {
            get
            {
                if (textBuilder == null) textBuilder = new StringBuilder(1024);
                return textBuilder;
            }
        }
        /// <summary>行間固定用。TMPTextを介さないため。</summary>
        private float FixLineHight { get; set; }

        /// <summary>トークンを受け取り、対象の文章を変換します。</summary>
        /// <param name="aozoraTokens">元の文章一行に相当するトークン列。</param>
        /// <param name="resultList">変換後の文章をページ単位で格納します。</param>
        public void Parse(List<AozoraToken> aozoraTokens, ref List<string> resultList)
        {
            if (aozoraTokens == null || aozoraTokens.Count == 0) return;

            int currentIndex = 0;

            if (resultList == null || resultList.Count == 0) resultList.Add("");

            // 作品名などの抜き出し
            if (aozoraTokens[currentIndex].Element == AozoraToken.ElementType.Header)
            {
                if (aozoraTokens[currentIndex].Type == AozoraToken.TokenType.SkipLine) return;
                resultList[currentIndex] += $"{aozoraTokens[currentIndex].Literal}<br>";
                CurrentElement = AozoraToken.ElementType.Header;
                return;
            }
            else if (CurrentElement == AozoraToken.ElementType.Header)
            {
                resultList.Add("");
                CurrentPageCount++;
                IsPageFirstAdd = true;
                CurrentElement = aozoraTokens[currentIndex].Element;
            }

            TextBuilder.Clear();
            TextBuilder.Append(resultList[CurrentPageCount]);

            IsMakeNewLine = true;
            IsLineFirstAdd = true;
            IsLineBreaked = false;

            while (currentIndex < aozoraTokens.Count)
            {

                if (aozoraTokens[currentIndex].Type == AozoraToken.TokenType.SkipLine) return;
                if (aozoraTokens[currentIndex].Type == AozoraToken.TokenType.EmptyLine)
                {
                    currentIndex++;
                    continue;
                }

                // 縦書きにする時のみ
                if (IsRotate2)
                {
                    if (IsRotate != aozoraTokens[currentIndex].IsRotate)
                    {
                        if (IsRotate)
                        {
                            TextBuilder.Append("<rotate=0>");
                        }
                        else
                        {
                            TextBuilder.Append("<rotate=90>");
                        }
                    }
                    IsRotate = aozoraTokens[currentIndex].IsRotate;
                }

                switch (aozoraTokens[currentIndex].Type)
                {
                    case AozoraToken.TokenType.RubyTarget:
                        // ルビ対象を含めると１行の文字数を超える時
                        if (CurrentLineLength + aozoraTokens[currentIndex].Literal.Size() > LineMaxLength) MakeNewLineOrPage(ref resultList);

                        FirstAdd();

                        if (IsRotate2)
                        {
                            TextBuilder.Append($"<r={aozoraTokens[currentIndex + 1].Literal}>{aozoraTokens[currentIndex].Literal.ChangeRotate(IsRotate).AdjustReplace()}</r>");
                        }
                        else
                        {
                            TextBuilder.Append($"<r={aozoraTokens[currentIndex + 1].Literal}>{aozoraTokens[currentIndex].Literal}</r>");
                        }
                        CurrentLineLength += aozoraTokens[currentIndex].Literal.Size();

                        if (CurrentLineLength >= LineMaxLength) MakeNewLineOrPage(ref resultList);

                        currentIndex++;
                        break;

                    case AozoraToken.TokenType.Gaiji:
                        PartialAdd(aozoraTokens[currentIndex].Literal, ref resultList);
                        break;

                    case AozoraToken.TokenType.Annotation:
                        CheckAnnotation(aozoraTokens[currentIndex].Literal, ref resultList);
                        if (aozoraTokens.Count == 1) IsMakeNewLine = false;
                        break;

                    default:
                        if (CurrentElement != aozoraTokens[currentIndex].Element && aozoraTokens[currentIndex].Element == AozoraToken.ElementType.Footer)
                        {
                            CurrentElement = aozoraTokens[currentIndex].Element;
                            if (IsRotate2)
                            {
                                resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString()).ChangeRotate2(true).FixReplace();
                            }
                            else
                            {
                                resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString());
                            }
                            resultList.Add("");
                            TextBuilder.Clear();
                            CurrentLineCount = 0;
                            CurrentLineLength = 0;
                            CurrentPageCount++;
                            IsLineBreaked = false;
                            IsPageFirstAdd = true;

                            FirstAdd();
                            if (IsRotate2)
                            {
                                TextBuilder.Append(aozoraTokens[currentIndex].Literal.ChangeRotate(IsRotate).AdjustReplace());
                            }
                            else
                            {
                                TextBuilder.Append(aozoraTokens[currentIndex].Literal);
                            }
                            CurrentLineLength = aozoraTokens[currentIndex].Size;
                            break;
                        }
                        if (currentIndex >= aozoraTokens.Count - 1)
                        {
                            PartialAdd(aozoraTokens[currentIndex].Literal, ref resultList);
                            break;
                        }
                        // トークンの先読み
                        switch (aozoraTokens[currentIndex + 1].Type)
                        {
                            // ルビのテキストの場合
                            case AozoraToken.TokenType.RubyText:

                                // ルビ対象の特定
                                int rubyStartPosition = GetRubyStartPosition(aozoraTokens[currentIndex].Literal, aozoraTokens[currentIndex].StringInfo.LengthInTextElements - 1);

                                if (rubyStartPosition == 0)
                                {
                                    // ルビ対象を含めると１行の文字数を超える時
                                    if (CurrentLineLength + aozoraTokens[currentIndex].Literal.Size() > LineMaxLength) MakeNewLineOrPage(ref resultList);

                                    FirstAdd();

                                    if (IsRotate2)
                                    {
                                        TextBuilder.Append($"<r={aozoraTokens[currentIndex + 1].Literal}>{aozoraTokens[currentIndex].Literal.ChangeRotate(IsRotate).AdjustReplace()}</r>");
                                    }
                                    else
                                    {
                                        TextBuilder.Append($"<r={aozoraTokens[currentIndex + 1].Literal}>{aozoraTokens[currentIndex].Literal}</r>");
                                    }
                                    CurrentLineLength += aozoraTokens[currentIndex].Literal.Size();

                                    if (CurrentLineLength >= LineMaxLength) MakeNewLineOrPage(ref resultList);
                                }
                                else
                                {
                                    string nonRubyTarget = aozoraTokens[currentIndex].StringInfo.SubstringByTextElements(0, rubyStartPosition);
                                    string rubyTarget = aozoraTokens[currentIndex].StringInfo.SubstringByTextElements(rubyStartPosition);

                                    PartialAdd(nonRubyTarget, ref resultList);
                                    if (CurrentLineLength + rubyTarget.Size() > LineMaxLength) MakeNewLineOrPage(ref resultList);

                                    FirstAdd();

                                    if (IsRotate2)
                                    {
                                        TextBuilder.Append($"<r={aozoraTokens[currentIndex + 1].Literal}>{rubyTarget.ChangeRotate(IsRotate).AdjustReplace()}</r>");
                                    }
                                    else
                                    {
                                        TextBuilder.Append($"<r={aozoraTokens[currentIndex + 1].Literal}>{rubyTarget}</r>");
                                    }
                                    CurrentLineLength += rubyTarget.Size();

                                    if (CurrentLineLength >= LineMaxLength) MakeNewLineOrPage(ref resultList);
                                }
                                currentIndex++;
                                break;

                            case AozoraToken.TokenType.Annotation:

                                Match match = StringExtensions.regexBoutenChecker.Match(aozoraTokens[currentIndex + 1].Literal);

                                if (match.Success)
                                {
                                    match = Regex.Match(aozoraTokens[currentIndex].Literal, match.Groups["Target"].Value);
                                    if (match.Success)
                                    {
                                        if (match.Index > 0) PartialAdd(aozoraTokens[currentIndex].Literal.Substring(0, match.Index), ref resultList);
                                        IsBouten = true;
                                        PartialAdd(aozoraTokens[currentIndex].Literal.Substring(match.Index), ref resultList);
                                        IsBouten = false;
                                    }
                                    else
                                    {
                                        PartialAdd(aozoraTokens[currentIndex].Literal, ref resultList);
                                    }
                                    currentIndex++;
                                    break;
                                }

                                PartialAdd(aozoraTokens[currentIndex].Literal, ref resultList);
                                break;

                            default:
                                PartialAdd(aozoraTokens[currentIndex].Literal, ref resultList);
                                break;
                        }
                        break;
                }
                currentIndex++;
            }
            // 行終わりの処理
            MakeNewLineOrPage(ref resultList, true);
        }
        /// <summary>後置されたルビの対象を特定し、その開始位置を返します。</summary>
        /// <param name="baseString">元の文字列。</param>
        /// <param name="currentIndex">ルビ対象を検索する開始位置。</param>
        /// <returns></returns>
        private static int GetRubyStartPosition(string baseString, int currentIndex)
        {
            EStringType tempStringType = EStringType.AnyType;
            string tempString;

            for (int j = currentIndex; j >= 0; j--)
            {
                tempString = StringInfo.GetNextTextElement(baseString, j);

                // 空白ならすぐに返す
                if (string.IsNullOrWhiteSpace(tempString)) return j + 1;

                // 漢字
                if (@"[仝々〆〇ヶ]".Contains(tempString))
                {
                    switch (tempStringType)
                    {
                        case EStringType.AnyType:
                            tempStringType = EStringType.Kanji;
                            continue;

                        case EStringType.Kanji:
                            continue;

                        default:
                            return j + 1;
                    }
                }
                // 力技で除外。
                if (@"[-―ー～‐－￣_＿]".Contains(tempString) || StringExtensions.NotLineHeadStrings.Contains(tempString) || StringExtensions.NotLineEndString.Contains(tempString))
                {
                    switch (tempStringType)
                    {
                        case EStringType.AnyType:
                            tempStringType = EStringType.Alphabet;
                            continue;

                        case EStringType.Alphabet:
                            continue;

                        default:
                            return j + 1;
                    }
                }
                if (StringExtensions.regexKanji.IsMatch(tempString))
                {
                    switch (tempStringType)
                    {
                        case EStringType.AnyType:
                            tempStringType = EStringType.Kanji;
                            continue;

                        case EStringType.Kanji:
                            continue;

                        default:
                            return j + 1;
                    }
                }
                // アルファベット
                if (StringExtensions.regexAlphabet.IsMatch(tempString))
                {
                    switch (tempStringType)
                    {
                        case EStringType.AnyType:
                            tempStringType = EStringType.Alphabet;
                            continue;

                        case EStringType.Alphabet:
                            continue;

                        default:
                            return j + 1;
                    }
                }
                // 数字
                if (StringExtensions.regexNumber.IsMatch(tempString))
                {
                    switch (tempStringType)
                    {
                        case EStringType.AnyType:
                            tempStringType = EStringType.Number;
                            continue;

                        case EStringType.Number:
                            continue;

                        default:
                            return j + 1;
                    }
                }
                // カタカナ（すべて）
                if (StringExtensions.regexKatakana.IsMatch(tempString))
                {
                    switch (tempStringType)
                    {
                        case EStringType.AnyType:
                            tempStringType = EStringType.Katakana;
                            continue;

                        case EStringType.Katakana:
                            continue;

                        default:
                            return j + 1;
                    }
                }
                // ひらがな
                if (StringExtensions.regexHiragana.IsMatch(tempString))
                {
                    switch (tempStringType)
                    {
                        case EStringType.AnyType:
                            tempStringType = EStringType.Hiragana;
                            continue;

                        case EStringType.Hiragana:
                            continue;

                        default:
                            return j + 1;
                    }
                }
            }
            return 0;
        }

        /// <summary>改行を追加し、最大行数を超えていれば改ページします。</summary>
        /// <param name="resultList">変換後の文章をページ単位で格納します。</param>
        /// <param name="lineEndFlag">読み取った１行の終わりである場合はtrue。</param>
        private void MakeNewLineOrPage(ref List<string> resultList, bool lineEndFlag = false)
        {
            if (IsMakeNewLine)
            {
                CurrentLineCount++;
                if (CurrentLineCount < LineMaxCount) TextBuilder.Append("<br>");
                IsLineBreaked = true;
            }

            if (lineEndFlag)
            {
                if (JisageCount != 0 && IsJisageContinuity == false)
                {
                    LineMaxLength += JisageCount * 2;
                    JisageCount = 0;
                    OrikaesiCount = 0;
                    TextBuilder.Append("<margin-left=0>");
                }
                if (JiageCount != 0 && IsJiageContinuity == false)
                {
                    LineMaxLength += JiageCount * 2;
                    JiageCount = 0;
                    TextBuilder.Append(@"<align=""left""><margin-right=0>");
                }
                if (IsRightAlign && IsRightAlignContinuity == false)
                {
                    IsRightAlign = false;
                    TextBuilder.Append(@"<align=""left"">");
                }
                IsLineBreaked = false;
            }

            if (CurrentLineCount >= LineMaxCount)
            {
                if (IsRotate2)
                {
                    resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString()).ChangeRotate2(true).FixReplace();
                }
                else
                {
                    resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString());
                }
                resultList.Add("");
                TextBuilder.Clear();
                CurrentLineCount = 0;
                CurrentPageCount++;
                IsPageFirstAdd = true;
                IsCenterAlign = false;
            }
            else if (lineEndFlag)
            {
                if (IsRotate2)
                {
                    resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString()).ChangeRotate2(true).FixReplace();
                }
                else
                {
                    resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString());
                }
                TextBuilder.Clear();
            }
            CurrentLineLength = 0;
            IsMakeNewLine     = true;
            IsLineFirstAdd    = true;
        }

        /// <summary>注記された組版の処理を行います。</summary>
        /// <param name="annotationString">注記として組版の指示が書かれた文字列。</param>
        /// <param name="resultList">変換後の文章をページ単位で格納します。</param>
        private void CheckAnnotation(string annotationString, ref List<string> resultList)
        {
            string[] annotation = annotationString.Split('、');

            foreach (string annotate in annotation)
            {
                switch (annotate)
                {
                    case string str when str.Contains("字下げ"):
                        if (annotate.Contains("終わり"))
                        {
                            // 初期化して返す
                            LineMaxLength += JisageCount * 2;
                            JisageCount = 0;
                            OrikaesiCount = 0;
                            TextBuilder.Append("<margin-left=0em>");
                            IsJisageContinuity = false;
                            break;
                        }
                        Match matchJisage = StringExtensions.regexNumber.Match(str);
                        if (matchJisage.Success == false) continue;

                        // 一旦戻す
                        LineMaxLength += JisageCount * 2;
                        JisageCount = int.Parse(matchJisage.Value.ConvertHalf());
                        LineMaxLength -= JisageCount * 2;
                        if (IsPageFirstAdd == false) TextBuilder.Append($"<margin-left={JisageCount}em>");

                        switch (annotate)
                        {
                            case string jisage when jisage.Contains("ここから"):
                                OrikaesiCount = 0;
                                IsJisageContinuity = true;
                                break;

                            case string jisage when jisage.Contains("折り返し"):
                                OrikaesiCount = int.Parse(matchJisage.Value.ConvertHalf()) - JisageCount;
                                if (OrikaesiCount < 0) OrikaesiCount = 0;
                                break;

                            default:
                                OrikaesiCount = 0;
                                IsJisageContinuity = false;
                                break;
                        }
                        break;

                    case string str when str.Contains("字上げ"):
                        if (annotate.Contains("終わり"))
                        {
                            // 初期化して返す。
                            LineMaxLength += JiageCount * 2;
                            JiageCount = 0;
                            TextBuilder.Append(@"<align=""left""><margin-right=0em>");
                            break;
                        }
                        // 単独で無いなら処理しない。
                        if (CurrentLineLength != 0) continue;
                        Match matchJiage = StringExtensions.regexNumber.Match(str);
                        if (matchJiage.Success == false) continue;

                        // 一旦戻す
                        LineMaxLength += JiageCount * 2;
                        JiageCount = int.Parse(matchJiage.Value.ConvertHalf());
                        LineMaxLength -= JiageCount * 2;

                        if (IsPageFirstAdd == false) TextBuilder.Append($@"<align=""right""><margin-right={JiageCount}em>");
                        IsJiageContinuity = annotate.Contains("ここから");
                        break;

                    // 「改行天付き」
                    case string str when str.Contains("改行天付き"):
                        TextBuilder.Append($@"<align=""left""><margin-left=0em>");
                        JiageCount = 0;
                        JisageCount = 0;
                        OrikaesiCount = 0;
                        break;

                    case string str when str.Contains("地付き"):
                        if (annotate.Contains("終わり"))
                        {
                            TextBuilder.Append(@"<align=""left"">");
                            IsRightAlign = false;
                            IsRightAlignContinuity = false;
                            break;
                        }
                        // 単独で無いなら処理しない。
                        if (CurrentLineLength != 0) continue;
                        if (IsPageFirstAdd == false) TextBuilder.Append(@"<align=""right"">");
                        IsRightAlign = true;
                        IsRightAlignContinuity = annotate.Contains("ここから");
                        break;

                    case string str when str.Contains("左右中央"):
                        IsCenterAlign = true;
                        break;

                    // 次のページから始める。
                    case string str when str.Contains("改ページ"):
                        MakeCenterAlign();
                        if (CurrentLineCount == 0) break;
                        if (IsRotate2)
                        {
                            resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString()).ChangeRotate2(true).FixReplace();
                        }
                        else
                        {
                            resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString());
                        }
                        resultList.Add("");
                        TextBuilder.Clear();
                        CurrentLineLength = 0;
                        CurrentLineCount = 0;
                        CurrentPageCount++;
                        IsLineBreaked = false;
                        IsPageFirstAdd = true;
                        break;

                    // 次の左ページから始める。
                    case string str when str.Contains("改丁"):
                        MakeCenterAlign();
                        if (CurrentLineCount == 0) break;
                        if (IsRotate2)
                        {
                            resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString()).ChangeRotate2(true).FixReplace();
                        }
                        else
                        {
                            resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString());
                        }
                        resultList.Add("");
                        TextBuilder.Clear();
                        CurrentLineLength = 0;
                        CurrentLineCount = 0;
                        CurrentPageCount++;
                        if (CurrentPageCount % 2 == 0)
                        {
                            resultList.Add("");
                            CurrentPageCount++;
                        }
                        IsLineBreaked = false;
                        IsPageFirstAdd = true;
                        break;

                    // 次の右ページから始める。
                    case string str when str.Contains("改見開き"):
                        MakeCenterAlign();
                        if (CurrentLineCount == 0) break;
                        if (IsRotate2)
                        {
                            resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString()).ChangeRotate2(true).FixReplace();
                        }
                        else
                        {
                            resultList[CurrentPageCount] = TMProRubyUtil.GetExpandText(TextBuilder.ToString());
                        }
                        resultList.Add("");
                        TextBuilder.Clear();
                        CurrentLineLength = 0;
                        CurrentLineCount = 0;
                        CurrentPageCount++;
                        if (CurrentPageCount % 2 != 0)
                        {
                            resultList.Add("");
                            CurrentPageCount++;
                        }
                        IsLineBreaked = false;
                        IsPageFirstAdd = true;
                        break;

                    case string str when str.Contains("横組み"):
                        if (IsRotate2 == false) break;
                        if (annotate.Contains("終わり"))
                        {
                            TextBuilder.Append("<rotate=90>");
                            IsRotate = true;

                        }
                        else
                        {
                            TextBuilder.Append("<rotate=0>");
                            IsRotate = false;
                        }
                        break;

                    case string str when StringExtensions.regexBoutenStart.IsMatch(str):
                        IsBouten = true;
                        break;

                    case string str when StringExtensions.regexBoutenEnd.IsMatch(str):
                        IsBouten = false;
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>ページ追加時など、一度だけ書き込めば良いタグなどを処理します。</summary>
        private void FirstAdd()
        {
            if (IsPageFirstAdd)
            {
                textBuilder.Append($"<line-height={FixLineHight:F3}em>");
                if (IsRotate2 && IsRotate) TextBuilder.Append("<rotate=90>");
                if (JisageCount != 0) TextBuilder.Append($"<margin-left={JisageCount}em>");
                if (JiageCount != 0) TextBuilder.Append($@"<align=""right""><margin-right={JiageCount}em>");
                if (IsRightAlign) TextBuilder.Append(@"<align=""right"">");
                IsPageFirstAdd = false;
            }
            if (IsLineBreaked)
            {
                if (OrikaesiCount != 0)
                {
                    TextBuilder.Append($"<space={OrikaesiCount}em>");
                    CurrentLineLength += OrikaesiCount;
                }
                IsLineBreaked = false;
            }
        }

        /// <summary>一行に収まるように、適当な改行・改ページと共に文字列を追加します。</summary>
        /// <param name="baseString">ページに追加する文字列。</param>
        /// <param name="resultList">変換後の文章をページ単位で格納します。</param>
        private void PartialAdd(string baseString, ref List<string> resultList)
        {
            if (IsLineBreaked && CurrentLineLength == 0)
            {
                if (string.IsNullOrWhiteSpace(baseString)) return;
            }

            int AdditionalSize = 0;
            if (IsLineBreaked) AdditionalSize += OrikaesiCount * 2;

            if (LineMaxLength - CurrentLineLength >= baseString.Size() + AdditionalSize)
            {
                FirstAdd();

                if (IsBouten) AddBouten(baseString);

                if (IsRotate2)
                {
                    TextBuilder.Append(baseString.ChangeRotate(IsRotate).AdjustReplace());
                }
                else
                {
                    TextBuilder.Append(baseString);
                }

                if (IsBouten) TextBuilder.Append("</r>");

                CurrentLineLength += baseString.Size();
                if (CurrentLineLength >= LineMaxLength) MakeNewLineOrPage(ref resultList);

                return;
            }

            StringInfo stringInfo = baseString.GetStringInfo();
            // バイト換算⇒文字数換算へ
            int leftLength = (LineMaxLength - CurrentLineLength - AdditionalSize) / 2 <= 0 ? 1 : (LineMaxLength - CurrentLineLength - AdditionalSize) / 2;
            int separateLength = stringInfo.LengthInTextElements < leftLength ? stringInfo.LengthInTextElements : leftLength;

            separateLength = CheckKinsoku(baseString, separateLength);
            if (separateLength <= 0)
            {
                MakeNewLineOrPage(ref resultList);
                separateLength = stringInfo.LengthInTextElements;
            }

            string addString = stringInfo.SubstringByTextElements(0, separateLength);
            string leftString = separateLength < stringInfo.LengthInTextElements ? stringInfo.SubstringByTextElements(separateLength) : "";

            FirstAdd();

            if (IsBouten) AddBouten(addString);

            if (IsRotate2)
            {
                TextBuilder.Append(addString.ChangeRotate(IsRotate).AdjustReplace());
            }
            else
            {
                TextBuilder.Append(addString);
            }

            if (IsBouten) TextBuilder.Append("</r>");

            CurrentLineLength += addString.Size();
            if (CurrentLineLength >= LineMaxLength) MakeNewLineOrPage(ref resultList);

            if (string.IsNullOrEmpty(leftString)) return;

            PartialAdd(leftString, ref resultList);

        }

        /// <summary>文字列を分割する場合に、禁則処理を行います。</summary>
        /// <param name="baseString">ページに追加する文字列。</param>
        /// <param name="separateLength">文字列を分割する位置。</param>
        /// <returns></returns>
        private int CheckKinsoku(string baseString, int separateLength)
        {
            StringInfo stringInfo = new StringInfo(baseString);
            if (stringInfo.LengthInTextElements <= separateLength) return separateLength;

            int currentTextElement = separateLength;

            // 基本的に禁則処理は次の行に送る方針

            // 行頭禁止をチェック。
            if (StringExtensions.NotLineHeadStrings.Contains(stringInfo.SubstringByTextElements(currentTextElement, 1)))
            {

                // １文字だけの場合は例外的に前の行に含める。
                if (currentTextElement + 1 >= stringInfo.LengthInTextElements) return stringInfo.LengthInTextElements;
                if (StringExtensions.NotLineHeadStrings.Contains(stringInfo.SubstringByTextElements(currentTextElement + 1, 1)) == false) return currentTextElement + 1;

                // 次の文字も行頭禁止の時は、前の行から１文字一緒に送る。
                currentTextElement--;

                while (currentTextElement >= 0)
                {
                    if (StringExtensions.NotLineHeadStrings.Contains(stringInfo.SubstringByTextElements(currentTextElement, 1)) == false) return currentTextElement;

                    // 前の文字も行頭禁止の時は、さらに前の文字から１文字一緒に送る。
                    currentTextElement--;
                }
            }

            if (currentTextElement <= 0)
            {
                currentTextElement = 0;
                return currentTextElement;
            }

            // 行末禁止をチェック。
            if (StringExtensions.NotLineEndString.Contains(stringInfo.SubstringByTextElements(currentTextElement - 1, 1)))
            {
                if (currentTextElement - 1 <= 0) return 0;
                // 末尾を次の行に送る。
                currentTextElement--;

                while (currentTextElement >= 1)
                {
                    if (StringExtensions.NotLineEndString.Contains(stringInfo.SubstringByTextElements(currentTextElement - 1, 1)) == false) return currentTextElement;

                    // 行末禁止の文字であれば、次の行に送る。
                    currentTextElement--;
                }
            }

            if (currentTextElement <= 0)
            {
                currentTextElement = 0;
                return currentTextElement;
            }

            // 分離禁止文字
            switch (stringInfo.SubstringByTextElements(currentTextElement, 1))
            {
                case "―":
                    if (currentTextElement > 0 && stringInfo.SubstringByTextElements(currentTextElement - 1, 1) == "―") return currentTextElement -= 1;
                    break;

                case "—":
                    if (currentTextElement > 0 && stringInfo.SubstringByTextElements(currentTextElement - 1, 1) == "—") return currentTextElement -= 1;
                    break;

                case "…":
                    if (currentTextElement > 0 && stringInfo.SubstringByTextElements(currentTextElement - 1, 1) == "…") return currentTextElement -= 1;
                    break;

                case "‥":
                    if (currentTextElement > 0 && stringInfo.SubstringByTextElements(currentTextElement - 1, 1) == "‥") return currentTextElement -= 1;
                    break;

                case "〵":
                    if (currentTextElement > 0 && stringInfo.SubstringByTextElements(currentTextElement - 1, 1) == "〳" || stringInfo.SubstringByTextElements(currentTextElement - 1, 1) == "〴")
                        return currentTextElement -= 1;
                    break;

                case "＼":
                    if (currentTextElement > 0 && stringInfo.SubstringByTextElements(currentTextElement - 1, 1) == "／") return currentTextElement -= 1;
                    if (currentTextElement - 1 > 0 && stringInfo.SubstringByTextElements(currentTextElement - 1, 1) == "″" && stringInfo.SubstringByTextElements(currentTextElement - 2, 1) == "／")
                        return currentTextElement - 2;
                    break;
            }
            return currentTextElement;
        }

        /// <summary>傍点をルビの形でTextBuilderに付加します。</summary>
        /// <param name="baseString">傍点を振る対象の文字列。</param>
        private void AddBouten(string baseString)
        {
            TextBuilder.Append("<r=・");
            for (int i = 0; i < baseString.GetStringInfo().LengthInTextElements - 1; i++)
            {
                TextBuilder.Append("　・");
            }
            TextBuilder.Append(">");
        }

        /// <summary>ページ左右中央を指定されている場合の処理をします。</summary>
        private void MakeCenterAlign()
        {
            if (IsCenterAlign == false || CurrentLineCount >= LineMaxCount) return;

            while (CurrentLineCount < LineMaxCount)
            {
                TextBuilder.Insert(0, "<br>").Insert(TextBuilder.Length, "<br>");
                CurrentLineCount += 2;
            }
            IsCenterAlign = false;
        }
    }
}