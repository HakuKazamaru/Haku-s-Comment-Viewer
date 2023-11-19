using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HakuCommentViewer.WinForm
{
    internal static class Program
    {
        /// <summary>
        /// NLog���K�[
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// �ݒ�Ǘ��I�u�W�F�N�g
        /// </summary>
        public static Common.Setting setting;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string path = "";

            // �A�b�Z���u����񂩂�t�@�C���p�X�擾
            try
            {
                Assembly myAssembly = Assembly.GetEntryAssembly();
                if (myAssembly is not null)
                {
                    if (!string.IsNullOrWhiteSpace(myAssembly.Location))
                    {
                        path = Path.Combine(Path.GetDirectoryName(myAssembly.Location), "appsettings.json");
                    }
                    else
                    {
                        myAssembly = Assembly.GetExecutingAssembly();
                        if (!string.IsNullOrWhiteSpace(myAssembly.Location))
                        {
                            path = Path.Combine(Path.GetDirectoryName(myAssembly.Location), "appsettings.json");
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(Application.ExecutablePath))
                            {
                                path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "appsettings.json");
                            }
                            else
                            {
                                MessageBox.Show(
                                    "�A�b�Z���u���t�@�C���p�X�̎擾�Ɏ��s���܂����B",
                                    "�G���[",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error,
                                    MessageBoxDefaultButton.Button1);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(
                        "�A�b�Z���u���G���g���[���̎擾�Ɏ��s���܂����B",
                        "�G���[",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("�A�b�Z���u�����̎擾�Ɏ��s���܂����B\r\n�G���[:{0}", ex.Message),
                    "�G���[",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);
                return;
            }

            // �A�v���P�[�V�����ݒ�̓ǂݍ���
            try
            {
                setting = new Common.Setting(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("�A�v���P�[�V�����ݒ�̓ǂݍ��݂Ɏ��s���܂����B\r\n�G���[:{0}\r\n�ݒ�t�@�C��:\r\n{1}", ex.Message, path),
                    "�G���[",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);
                return;
            }

            // NLog�̐ݒ蔽�f
            NLog.LogManager.Configuration = setting.GetNLogSetting();

            logger.Info("==============================  Start   ==============================");

            try
            {
                using (var tokenSource = new CancellationTokenSource())
                {
                    CancellationToken token = tokenSource.Token;

                    using (var server = Task.Run(() => Common.WebServer.StartServerAsync(token), tokenSource.Token))
                    {
                        bool testmode = false;
                        // To customize application configuration such as set high DPI settings or default font,
                        // see https://aka.ms/applicationconfiguration.

                        ApplicationConfiguration.Initialize();

                        if (args is not null)
                        {
                            foreach (var arg in args)
                            {
                                if (arg.IndexOf("test") > -1)
                                {
                                    logger.Info("�e�X�g���[�h�ŋN�����܂��B");
                                    testmode = true;
                                    break;
                                }
                            }
                        }

                        if (testmode) Application.Run(new TestForm());
                        else Application.Run(new MainForm());

                        tokenSource.Cancel();

                        try
                        {
                            server.Wait();
                        }
                        catch (Exception ex)
                        {
                            logger.Info(ex, "WEB�T�[�o�[�̏I���ŃG���[���������܂����B���b�Z�[�W:{0}", ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "�\�����ʃG���[���������܂����B�G���[:{0}", ex.Message);
            }

            logger.Info("==============================   End    ==============================");
        }
    }
}