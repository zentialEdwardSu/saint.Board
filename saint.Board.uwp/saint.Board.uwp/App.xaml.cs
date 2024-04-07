using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace saint.Board.uwp
{

    sealed partial class App : Application
    {
        bool shouldsave = false;
        bool boardvalid = false;
        public event EventHandler<string> NotifySave;
        private ManualResetEventSlim operationCompletedEvent = new ManualResetEventSlim(false);

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override void OnFileActivated(FileActivatedEventArgs e)
        {
            Frame rootFrame = CreateRootFrame();

            if (!rootFrame.Navigate(typeof(MainPage),e))
            {
                throw new Exception("Failed to create initial page");
            }

            // Ensure the current window is active
            Window.Current.Activate();
            Windows.UI.Core.Preview.SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += App_CloseRequested;
        }

        private async void App_CloseRequested(object sender, Windows.UI.Core.Preview.SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            if (shouldsave)
            {
                var deferral = e.GetDeferral();

                var messageDialog = new ContentDialog
                {
                    Title = "saint.Board 正在退出",
                    Content = "你似乎有未被保存的更改",
                    PrimaryButtonText = "保存",
                    SecondaryButtonText = "不保存",
                    CloseButtonText = "不退出了"
                };

                messageDialog.DefaultButton = ContentDialogButton.Primary;

                var result = await messageDialog.ShowAsync();
                switch (result)
                {
                    case ContentDialogResult.None:
                        e.Handled = true;
                        break;
                    case ContentDialogResult.Primary:
                        if (boardvalid)
                        {
                            RaiseNotifySave("save pls");
                            operationCompletedEvent.Wait();
                        } else
                        {
                            var noticeDialog = new ContentDialog
                            {
                                Title = "saint.Board 不能保存未指定路径的白板",
                                Content = "请先使用保存指定路径",
                                PrimaryButtonText = "好的",
                            };
                            
                            noticeDialog.DefaultButton = ContentDialogButton.Primary;
                            await noticeDialog.ShowAsync();
                            e.Handled = true;
                        }
                        break;
                    case ContentDialogResult.Secondary:
                        break;
                    default:
                        break;
                }

                deferral.Complete();
            }
        }

        private Frame CreateRootFrame()
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // 当导航堆栈尚未还原时，导航到第一页，
                    // 并通过将所需信息作为导航参数传入来配置
                    // 参数
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // 确保当前窗口处于活动状态
                Window.Current.Activate();
                Windows.UI.Core.Preview.SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += App_CloseRequested;
            }
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }

        public void RaiseNotifySave(string message)
        {
            NotifySave?.Invoke(this, message);
        }

        public void UpdateShouldSave(bool b)
        {
            shouldsave = b;
        }

        public void UpdateValidBoard(bool b)
        {
            boardvalid = b;
        }

        public void OperationCompleted()
        {
            operationCompletedEvent.Set();
        }
    }
}
