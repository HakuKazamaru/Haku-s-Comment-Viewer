namespace HakuCommentViewer
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// アプリ終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void mfiExit_Clicked(object sender, EventArgs e)
        {
            bool result = await DisplayAlert("確認", "アプリを終了します。よろしいですか？", "Yes", "No");

            if (result)
            {
                Application.Current.CloseWindow(GetParentWindow());
            }
        }
    }
}