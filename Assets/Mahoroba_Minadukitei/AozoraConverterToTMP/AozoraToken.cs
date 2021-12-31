using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minadukitei.Products
{
    /// <summary>変換するための情報を保持するトークンのクラス。</summary>
    public class AozoraToken
    {
        /// <summary>切り出した文字列の種類</summary>
        public enum TokenType
        {
            /// <summary>行の終端</summary>
            EndOfLine,
            /// <summary>ルビのテキスト</summary>
            RubyText,
            /// <summary>ルビを振る対象</summary>
            RubyTarget,
            /// <summary>注記</summary>
            Annotation,
            /// <summary>外字注記</summary>
            Gaiji,
            /// <summary>半角スペース</summary>
            HalfSpace,
            /// <summary>全角スペース</summary>
            FullSpace,
            /// <summary>句読点などの区切り</summary>
            Delimiter,
            /// <summary>空行</summary>
            EmptyLine,
            /// <summary>非表示行</summary>
            SkipLine,
            /// <summary>通常の文</summary>
            Normal
        }
        /// <summary>txtファイル内での位置づけを返します。</summary>
        public enum ElementType
        {
            /// <summary>ヘッダー部分。作品名や著者名など。</summary>
            Header,
            /// <summary>本文部分。</summary>
            Body,
            /// <summary>テキスト末尾。「底本：」以降など。</summary>
            Footer
        }

        /// <summary>トークンの種類。</summary>
        public TokenType Type { get; set; }
        /// <summary>トークンの文字列。</summary>
        public string Literal { get; set; }
        /// <summary>回転させるかどうか。</summary>
        public bool IsRotate { get; set; }
        /// <summary>トークンのtxtファイル内での位置づけ。</summary>
        public ElementType Element { get; set; }
        /// <summary>トークン文字列のStringInfoを返します。</summary>
        public System.Globalization.StringInfo StringInfo { get; private set; }
        /// <summary>文字列の大きさを返します。全角２バイト、半角１バイト換算です。</summary>
        public int Size
        {
            get
            {
                if (byteCount > StringInfo.LengthInTextElements) return byteCount;
                return StringInfo.LengthInTextElements;
            }
        }
        private int byteCount;

        public AozoraToken(string literal, TokenType tokenType, bool isRotate, ElementType element)
        {
            Literal    = literal;
            Type       = tokenType;
            IsRotate   = isRotate;
            Element    = element;
            StringInfo = new System.Globalization.StringInfo(Literal);
            byteCount  = StringExtensions.shift_JisEncoding.GetByteCount(Literal);
        }
    }
}