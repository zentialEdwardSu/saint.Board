using saint.Board.uwp.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using System.Threading.Tasks;
using System.Numerics;
using Windows.ApplicationModel.Activation;

namespace saint.Board.uwp
{
    public sealed partial class MainPage : Page
    {
        private Polyline lasso;
        private Rect boundingRect;
        private bool isBoundRect;

        Symbol LassoSelect = (Symbol)0xEF20;

        SaintBoardISF _currentBoard;
        bool _shouldSave = false;

        private bool ShouldSave
        {
            get { return _shouldSave; }
            set
            {
                _shouldSave = value;
                (App.Current as App).UpdateShouldSave(value);
                SetTitle(CurrentBoard.Name);
            }
        }

        private SaintBoardISF CurrentBoard { 
            get { return _currentBoard; } 
            set
            {
                _currentBoard = value;
                (App.Current as App).UpdateValidBoard(value.IsValid);
                ShouldSave = false;
            }
        }
        public MainPage()
        {
            this.InitializeComponent();

            (App.Current as App).NotifySave += MainPage_NotifySave;

            CurrentBoard = new SaintBoardISF();
            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen;

            inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            inkCanvas.InkPresenter.StrokesErased += InkPresenter_StrokesErased;
        }

        private void MainPage_NotifySave(object sender, string e)
        {
            SaveTask().Wait();
            (App.Current as App).OperationCompleted();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var args = e.Parameter as FileActivatedEventArgs;
            if (args != null)
            {
                if (args.Files.Count == 1)
                {
                    StorageFile file = (StorageFile)args.Files[0];
                    using (var stream = await file.OpenSequentialReadAsync())
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(stream);
                    }

                    CurrentBoard = new SaintBoardISF(file);
                    Notify(CurrentBoard.Name + " loaded!:D", NotifyType.StatusMessage);
                }
                else
                {
                    new Exception($"One file only but get {args.Files.Count}");
                }
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            HelperFuctions.UpdateCanvasSize(rootGrid, outputGrid, inkCanvas);
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            ShouldSave = true;
            //Notify(args.Strokes.Count + " stroke(s) collected!", NotifyType.StatusMessage);
        }

        private async void toolButtonSave_Click(object sender, RoutedEventArgs e)
        {
            await SaveTask();
        }

        private async Task SaveTask()
        {
            if (ShouldSave)
            {
                StorageFile file;
                if (CurrentBoard.IsValid)
                {
                    file = CurrentBoard.File;
                }
                else
                {
                    var savePicker = new FileSavePicker();
                    savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                    savePicker.FileTypeChoices.Add("saint.Board's Data format", new[] { ".sbisf" });

                    file = await savePicker.PickSaveFileAsync();
                }
                if (null != file)
                {
                    InternalSave(file);
                }
            }
            else
            {
                Notify("Already up to date", NotifyType.StatusMessage);
            }
        }

        private async void InternalSave(StorageFile file)
        {
            try
            {
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    // Truncate any existing stream in case the new file
                    // is smaller than the old file.
                    stream.Size = 0;

                    await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                }
                CurrentBoard = new SaintBoardISF(file);
                Notify(CurrentBoard.Name + " saved! :D", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                // Report I/O errors during save.
                Notify(ex.Message, NotifyType.ErrorMessage);
            }
        }

        private async void toolButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentBoard.IsValid)
            {
                var dialog = new ContentDialog()
                {
                    Title = "你似乎想要从文件加载另一个白板",
                    Content = $"但是当前页面似乎已经有一个正在工作的白板，\n加载后当前文件: {CurrentBoard.Name} 会被关闭",
                    PrimaryButtonText = "开！",
                    SecondaryButtonText = "保存当前的之后再开",
                    CloseButtonText = "算啦",
                    FullSizeDesired = false,
                };

                dialog.PrimaryButtonClick += (_s, _e) => { };
                var res = await dialog.ShowAsync();

                switch (res)
                {
                    case ContentDialogResult.None:
                        return;
                    case ContentDialogResult.Primary:
                        break;
                    case ContentDialogResult.Secondary:
                        SaveTask().Wait();
                        break;
                    default:
                        return;
                }
            }
            var openPicker = new FileOpenPicker();
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add(".sbisf");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (null != file)
            {
                try
                {
                    using (var stream = await file.OpenSequentialReadAsync())
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(stream);
                    }

                    CurrentBoard = new SaintBoardISF(file);
                    Notify(CurrentBoard.Name + " loaded!:D", NotifyType.StatusMessage);
                }
                catch (Exception ex)
                {
                    // Report I/O errors during load.
                    Notify(ex.Message, NotifyType.ErrorMessage);
                }
            }
        }

        private void StrokeInput_StrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            ClearSelection();
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;
        }

        private void InkPresenter_StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            ShouldSave = true;
            ClearSelection();
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;
        }

        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            lasso = new Polyline()
            {
                Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 5, 2 },
            };

            lasso.Points.Add(args.CurrentPoint.RawPosition);
            selectionCanvas.Children.Add(lasso);
            isBoundRect = true;
        }

        private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
        {
            if (isBoundRect)
            {
                lasso.Points.Add(args.CurrentPoint.RawPosition);
            }
        }

        private void UnprocessedInput_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
        {
            lasso.Points.Add(args.CurrentPoint.RawPosition);

            boundingRect = inkCanvas.InkPresenter.StrokeContainer.SelectWithPolyLine(lasso.Points);
            isBoundRect = false;
            DrawBoundingRect();
        }

        private void DrawBoundingRect()
        {
            selectionCanvas.Children.Clear();

            if (boundingRect.Width <= 0 || boundingRect.Height <= 0)
            {
                return;
            }

            var rectangle = new Rectangle()
            {
                Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 5, 2 },
                Width = boundingRect.Width,
                Height = boundingRect.Height
            };

            Canvas.SetLeft(rectangle, boundingRect.X);
            Canvas.SetTop(rectangle, boundingRect.Y);

            selectionCanvas.Children.Add(rectangle);
        }

        private void ToolButton_Lasso(object sender, RoutedEventArgs e)
        {
            // By default, pen barrel button or right mouse button is processed for inking
            // Set the configuration to instead allow processing these input on the UI thread
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;
        }

        private void ClearDrawnBoundingRect()
        {
            if (selectionCanvas.Children.Count > 0)
            {
                selectionCanvas.Children.Clear();
                boundingRect = Rect.Empty;
            }
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.StrokeContainer.CopySelectedToClipboard();
        }

        private void OnCut(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.StrokeContainer.CopySelectedToClipboard();
            inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            ClearDrawnBoundingRect();
            ShouldSave = true;
        }

        private void OnPaste(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.InkPresenter.StrokeContainer.CanPasteFromClipboard())
            {
                inkCanvas.InkPresenter.StrokeContainer.PasteFromClipboard(HelperFuctions.GetWindowCenter());
                ShouldSave = true;
            }
            else
            {
                Notify("Cannot paste from clipboard.", NotifyType.ErrorMessage);
            }
        }

        private void ClearSelection()
        {
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            ClearDrawnBoundingRect();
        }

        private void touchGrid_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            e.Container = inkCanvas;
            ClearSelection();
        }

        private void touchGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var sc = e.Delta.Scale;
            var CenterPoint = e.Position;

            //foreach(InkStroke stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
            //{
            //    Matrix3x2 m = stroke.PointTransform;
            //    if (sc == 1)
            //    {
            //        m.M31 += (float)e.Delta.Translation.X;
            //        m.M32 += (float)e.Delta.Translation.Y;
            //    } else
            //    {
            //        m.M11 *= sc;
            //        m.M22 *= sc;
            //        m.M31 = (float)CenterPoint.X + ((float)CenterPoint.X - m.M31) * sc;
            //        m.M32 = (float)CenterPoint.Y + ((float)CenterPoint.Y - m.M32) * sc;
            //    }

            //    stroke.PointTransform = m;
            //}
            if (sc == 1)
            {
                foreach (InkStroke stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
                {
                    Matrix3x2 m = stroke.PointTransform;
                    m.M31 += (float)e.Delta.Translation.X;
                    m.M32 += (float)e.Delta.Translation.Y;
                    stroke.PointTransform = m;
                }
            }
            else
            {
                foreach (InkStroke stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
                {
                    Matrix3x2 m = stroke.PointTransform;
                    m.M11 *= sc;
                    m.M22 *= sc;
                    m.M31 = (float)CenterPoint.X + (m.M31 - (float)CenterPoint.X) * sc;
                    m.M32 = (float)CenterPoint.Y + (m.M32 - (float)CenterPoint.Y) * sc;
                    stroke.PointTransform = m;
                }
            }
            ShouldSave = true;
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        private void SetTitle(string title)
        {
            if (ShouldSave)
            {
                title = $"{title}(unsaved)";
            }
            ApplicationView.GetForCurrentView().Title = title;
        }

        private void Notify(string strMessage, NotifyType type)
        {
            UpdateStatus(strMessage, type);
            //Task.Delay(3000).Wait();
            //UpdateStatus(String.Empty, type);
        }

        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }

            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }

            // Raise an event if necessary to enable a screen reader to announce the status update.
            var peer = FrameworkElementAutomationPeer.FromElement(StatusBlock);
            if (peer != null)
            {
                peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
        }
    }
}
