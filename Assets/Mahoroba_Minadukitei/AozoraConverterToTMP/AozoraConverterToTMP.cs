using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

namespace Minadukitei.Products
{
    public static class AozoraConverterToTMP
    {
        public static string[] GetConvertText(int lineMaxLength, int lineMaxCount, string filePath, Encoding encoding)
        {
            List<AozoraToken> aozoraTokens;
            List<string> pageList = new List<string>();
            AozoraLexer  lexer    = new AozoraLexer();
            AozoraParser parser   = new AozoraParser(lineMaxLength, lineMaxCount);

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
