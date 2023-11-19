using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HakuCommentViewer.Plugin.Interface;
using HakuCommentViewer.Plugin.Models;

namespace HakuCommentViewer.Plugin
{
    /// <summary>
    /// プラグイン関連処理クラス
    /// </summary>
    public class PluginFunc
    {
        /// <summary>
        /// コメント取得/送信プラグインファイル名
        /// </summary>
        private const string CommentPluginName = "*.Plugin.Comment.*.dll";

        /// <summary>
        /// コメント読上げプラグインファイル名
        /// </summary>
        private const string NarrationPluginName = "*.Plugin.Narration.*.dll";

        /// <summary>
        /// コメントお遊びプラグインファイル名
        /// </summary>
        private const string HangOutPluginName = "*.Plugin.Hangout.*.dll";

        /// <summary>
        /// NLogロガー
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// プラグインインストール先
        /// </summary>
        private static string pluginBaseDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\plugin";

        /// <summary>
        /// プラグイン読込
        /// </summary>
        /// <param name="loadMode"></param>
        /// <returns></returns>
        public static PluginInstance LoadPlugins(int loadMode = 0)
        {
            PluginInstance returnVal = new PluginInstance();
            logger.Debug("========== Func Start! ==================================================");

            // コメント取得/送信プラグイン読込
            if (loadMode == 0 || loadMode == 1)
            {
                returnVal.CommentPlugin = LoadCommentPlugins();
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal;
        }

        /// <summary>
        /// コメントプラグイン読込
        /// </summary>
        /// <returns></returns>
        public static IComment[] LoadCommentPlugins()
        {
            List<IComment> returnVal = new List<IComment>();
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                List<IPlugin> pluginList = LoadDlls(CommentPluginName).ToList();
                logger.Info("コメントプラグインを{0}件検出しました。", pluginList.Count);
                foreach (var plugin in pluginList.Select((Value, Index) => (Value, Index)))
                {
                    returnVal.Add((IComment)plugin.Value);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "チャットプラグインの読み込みでエラーが発生しました。", ex.Message);
                returnVal = null;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal.ToArray();
        }

        /// <summary>
        /// DLL読込
        /// </summary>
        /// <param name="dllName"></param>
        /// <returns></returns>
        public static IPlugin[] LoadDlls(string dllName)
        {
            List<IPlugin> returnVal = new List<IPlugin>();
            logger.Debug("========== Func Start! ==================================================");

            try
            {
                string[] dllFilePathList = Directory.GetFiles(pluginBaseDirPath, dllName);
                foreach (var dllFilePath in dllFilePathList.Select((Value, Index) => (Value, Index)))
                {
                    logger.Debug("[{0}]DLLファイルパス:{1}", dllFilePath.Index, dllFilePath.Value);
                    logger.Debug("[{0}]-------------------------------------------------------------------------", dllFilePath.Index);
                    logger.Debug("[{0}]DLL情報", dllFilePath.Index);
                    logger.Debug("[{0}]-------------------------------------------------------------------------", dllFilePath.Index);

                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(dllFilePath.Value);

                        logger.Debug("[{0}]フルネーム　　　　　　:{1}", dllFilePath.Index, assembly.FullName);
                        logger.Debug("[{0}]アッセンブリ名　　　　:{1}", dllFilePath.Index, assembly.GetName().Name);
                        logger.Debug("[{0}]アッセンブリフルネーム:{1}", dllFilePath.Index, assembly.GetName().FullName);
                        logger.Debug("[{0}]ロケールカルチャー　　:{1}", dllFilePath.Index, assembly.GetName().CultureName);
                        logger.Debug("[{0}]-------------------------------------------------------------------------", dllFilePath.Index);

                        Type[] foundTypes = Array.FindAll(assembly.GetTypes(), (t) =>
                        {
                            return (t.IsClass && t.IsPublic && !t.IsAbstract && t.GetInterface(typeof(IComment).FullName) != null);
                        });

                        foreach (var pluginType in foundTypes.Select((Value, Index) => (Value, Index)))
                        {
                            logger.Debug("[{0}-{1}]フルネーム:{1}", dllFilePath.Index, pluginType.Index, pluginType.Value.FullName);
                            logger.Debug("[{0}-{1}]名前空間　:{1}", dllFilePath.Index, pluginType.Index, pluginType.Value.Namespace);
                            logger.Debug("[{0}-{1}]クラス名　:{1}", dllFilePath.Index, pluginType.Index, pluginType.Value.Name);
                            returnVal.Add((IPlugin)assembly.CreateInstance(pluginType.Value.FullName));
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "DLL'{1}'の読み込みでエラーが発生しました。:{0}", ex.Message, dllFilePath.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "プラグインの読み込みでエラーが発生しました。", ex.Message);
                returnVal = null;
            }

            logger.Debug("========== Func End!　 ==================================================");
            return returnVal.ToArray();
        }
    }
}
