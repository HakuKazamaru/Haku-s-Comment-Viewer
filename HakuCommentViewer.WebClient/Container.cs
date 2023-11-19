namespace HakuCommentViewer.WebClient
{
    public class Container
    {
        /// <summary>
        /// WebSocket再接続回数
        /// </summary>
        public readonly int RetryCount = 5;

        /// <summary>
        /// 接続状態
        /// </summary>
        public Dictionary<int, bool> IsConnectedList { get; set; }

        /// <summary>
        /// フッターテキスト
        /// </summary>
        private string _footerText;

        /// <summary>
        /// フッターテキスト
        /// </summary>
        public string FooterText
        {
            get { return this._footerText; }
            set
            {
                this._footerText = value;
                OnChange?.Invoke();
            }
        }

        /// <summary>
        /// イベントハンドラ
        /// </summary>
        public event Action? OnChange;

        /// <summary>
        /// コンストラクター
        /// </summary>
        public Container()
        {
            this._footerText = "未初期化";
        }

    }
}
