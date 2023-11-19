namespace HakuCommentViewer.WebClient
{
    /// <summary>
    /// 汎用機能クラス
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// UNIX時刻（ミリ秒）を日付文字列に変換
        /// </summary>
        /// <param name="unixTimeUsec"></param>
        /// <returns></returns>
        public static string UnixTimeToDateTimeString(string unixTimeUsec)
        {
            string returnVal = "";

            try
            {
                Int64 tmpInt = Int64.MinValue;

                if (Int64.TryParse(unixTimeUsec, out tmpInt))
                {
                    int tmpVal = unixTimeUsec.Length - 13;

                    if (tmpVal > 0)
                    {
                        tmpInt = tmpInt / (int)Math.Pow(10, tmpVal);
                    }
                    else if (tmpVal < 0)
                    {
                        tmpInt = tmpInt * (int)Math.Pow(10, tmpVal);
                    }

                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(tmpInt);
                    returnVal = dateTimeOffset.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
                }
                else
                {
                    returnVal = null;
                }
            }
            catch
            {
                returnVal = null;
            }

            return returnVal;
        }
    }
}
