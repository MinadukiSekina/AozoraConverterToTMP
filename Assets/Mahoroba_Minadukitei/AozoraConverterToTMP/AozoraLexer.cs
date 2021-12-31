using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Minadukitei.Products
{
    /// <summary>一行読み込んだ結果を元にトークンを返すクラス。</summary>
    public class AozoraLexer
    {
        public AozoraLexer()
        {
            SkipFlag = false;
            ElementType = AozoraToken.ElementType.Header;
        }
        /// <summary>読み込み時点でのtxtファイル内の位置要素</summary>
        private AozoraToken.ElementType ElementType { get; set; }
        /// <summary>読み飛ばすかどうか。</summary>
        private bool SkipFlag { get; set; }

        /// <summary>入力文字列をトークンに分けて返します。</summary>
        /// <param name="baseString">変換する文章の一行の文字列。</param>
        /// <returns>切り分けた結果のトークンのリスト。</returns>
        public List<AozoraToken> GetAozoraTokens(string baseString)
        {
            // 冒頭の説明は飛ばす
            if (Regex.IsMatch(baseString, "-{3}"))
            {
                if (SkipFlag) ElementType = AozoraToken.ElementType.Body;
                SkipFlag = !SkipFlag;
                return new List<AozoraToken> { new AozoraToken("", AozoraToken.TokenType.SkipLine, true, ElementType) };
            }
            if (SkipFlag) return new List<AozoraToken> { new AozoraToken("", AozoraToken.TokenType.SkipLine, true, ElementType) };

            if (ElementType == AozoraToken.ElementType.Header)
            {
                return new List<AozoraToken> { new AozoraToken(baseString, AozoraToken.TokenType.Normal, false, ElementType) };
            }

            // 空行の処理
            if (string.IsNullOrWhiteSpace(baseString)) return new List<AozoraToken> { new AozoraToken(baseString, AozoraToken.TokenType.EmptyLine, true, ElementType) };

            if (baseString.Contains("底本：") || baseString.Contains("［＃本文終わり］")) ElementType = AozoraToken.ElementType.Footer;

            StringBuilder stringBuilder = new StringBuilder(1024);
            int currentIndex = 0;
            List<AozoraToken> aozoraTokens = new List<AozoraToken>();

            // １字ずつチェック
            while (currentIndex < baseString.Length)
            {
                switch (baseString[currentIndex])
                {
                    case '※':
                        if (currentIndex == baseString.Length - 1 || baseString[currentIndex + 1] != '［')
                        {
                            stringBuilder.Append(baseString[currentIndex]);
                            break;
                        }
                        // トークンを追加してから、外字注記の読み込み
                        if (currentIndex != 0 && stringBuilder.Length > 0)
                        {
                            aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                            stringBuilder.Clear();
                        }

                        stringBuilder.Append(baseString[currentIndex]);
                        currentIndex++;
                        stringBuilder.Append(baseString[currentIndex]);
                        int gaijiCount = 1;

                        // 一応入れ子状になっていることも想定
                        while (gaijiCount > 0 && currentIndex < baseString.Length - 1)
                        {
                            currentIndex++;
                            if (baseString[currentIndex] == '［') gaijiCount++;
                            if (baseString[currentIndex] == '］') gaijiCount--;
                            stringBuilder.Append(baseString[currentIndex]);
                        }

                        aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Gaiji, true, ElementType));
                        stringBuilder.Clear();
                        break;

                    case '［':
                        if (currentIndex == baseString.Length - 1 || baseString[currentIndex + 1] != '＃')
                        {
                            stringBuilder.Append(baseString[currentIndex]);
                            break;
                        }
                        // トークンを追加してから、注記の読み込み
                        if (currentIndex != 0 && stringBuilder.Length > 0)
                        {
                            aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                            stringBuilder.Clear();
                        }

                        stringBuilder.Append(baseString[currentIndex]);
                        int AnnotationCount = 1;

                        // 一応入れ子状になっていることも想定
                        while (AnnotationCount > 0 && currentIndex < baseString.Length - 1)
                        {
                            currentIndex++;
                            if (baseString[currentIndex] == '［') AnnotationCount++;
                            if (baseString[currentIndex] == '］') AnnotationCount--;
                            stringBuilder.Append(baseString[currentIndex]);
                        }

                        aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Annotation, true, ElementType));
                        stringBuilder.Clear();
                        break;

                    case '|':
                        // トークンを追加してから、ルビ対象の読み込み
                        if (currentIndex != 0 && stringBuilder.Length > 0)
                        {
                            aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                            stringBuilder.Clear();
                        }

                        while (currentIndex < baseString.Length - 1)
                        {
                            currentIndex++;
                            stringBuilder.Append(baseString[currentIndex]);

                            if (currentIndex == baseString.Length - 1 || baseString[currentIndex + 1] == '《')
                            {
                                aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.RubyTarget, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                                stringBuilder.Clear();
                                break;
                            }
                        }
                        break;

                    case '｜':
                        // トークンを追加してから、ルビ対象の読み込み
                        if (currentIndex != 0 && stringBuilder.Length > 0)
                        {
                            aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                            stringBuilder.Clear();
                        }

                        while (currentIndex < baseString.Length - 1)
                        {
                            currentIndex++;
                            stringBuilder.Append(baseString[currentIndex]);

                            if (currentIndex == baseString.Length - 1 || baseString[currentIndex + 1] == '《')
                            {
                                aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.RubyTarget, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                                stringBuilder.Clear();
                                break;
                            }
                        }
                        break;

                    case '《':
                        // トークンを追加してから、ルビの読み込み
                        if (currentIndex != 0 && stringBuilder.Length > 0)
                        {
                            aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                            stringBuilder.Clear();
                        }

                        bool isBouten = currentIndex < baseString.Length - 1 && baseString[currentIndex + 1] == '《';
                        if (isBouten)
                        {
                            aozoraTokens.Add(new AozoraToken("［＃傍点］", AozoraToken.TokenType.Annotation, true, ElementType));
                            currentIndex++;
                        }

                        while (currentIndex < baseString.Length - 1)
                        {
                            currentIndex++;
                            stringBuilder.Append(baseString[currentIndex]);

                            if (currentIndex == baseString.Length - 1 || baseString[currentIndex + 1] == '》')
                            {
                                if (isBouten)
                                {
                                    aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, true, ElementType));
                                    aozoraTokens.Add(new AozoraToken("［＃傍点終わり］", AozoraToken.TokenType.Annotation, true, ElementType));
                                    currentIndex++;
                                }
                                else
                                {
                                    aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.RubyText, true, ElementType));
                                }
                                stringBuilder.Clear();
                                break;
                            }
                        }
                        break;

                    case '》':
                        break;

                    case ' ':
                        if (stringBuilder.Length > 0)
                        {
                            aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                            stringBuilder.Clear();
                        }
                        aozoraTokens.Add(new AozoraToken(baseString[currentIndex].ToString(), AozoraToken.TokenType.HalfSpace, false, ElementType));
                        break;

                    case '　':
                        if (stringBuilder.Length > 0)
                        {
                            aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                            stringBuilder.Clear();
                        }
                        aozoraTokens.Add(new AozoraToken(baseString[currentIndex].ToString(), AozoraToken.TokenType.FullSpace, true, ElementType));
                        break;

                    case ',':
                        stringBuilder.Append(baseString[currentIndex]);
                        aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));
                        stringBuilder.Clear();
                        break;

                    case '、':
                        stringBuilder.Append(baseString[currentIndex]);
                        aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, true, ElementType));
                        stringBuilder.Clear();
                        break;

                    default:
                        stringBuilder.Append(baseString[currentIndex]);
                        break;
                }
                currentIndex++;
            }

            if (stringBuilder.Length > 0) aozoraTokens.Add(new AozoraToken(stringBuilder.ToString(), AozoraToken.TokenType.Normal, StringExtensions.regexNonLandscape.IsMatch(stringBuilder.ToString()), ElementType));

            return aozoraTokens;
        }
    }
}

